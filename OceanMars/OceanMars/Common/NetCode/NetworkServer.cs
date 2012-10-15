using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

/*What do I want to achieve here...
    I need to specify the transport layer
 * I require a constant interface which is exported to the game logic
 * We want to be able to send game information
 * Need a way to specify who to connnect to... probably via direct connection at first
 * Need to be able to receive information in a new thread
 * Probably have some sort of obeservable object, which notifies interested parties of new events.
 * I'll probably supply interfaces for player centric activity and game world.
 * We'll likely send updates in a single packet -> Extract them and notify the required sub components.
 * -> the subcomponents will be inhereted by game logic (so a component for players [subtype of character, which could be a monster or player])
 * 
 * Latency and connectivity measurements
 */

namespace OceanMars.Common.NetCode
{

    public class NetworkServer : NetworkBase
    {

        #region Member Variables

        private Timer TimeoutTimer; // The timer used to cause sync timeouts.
        private Dictionary<IPEndPoint, ConnectionID> connections; // Mapping of IPEndPoints to ConnectionIDs

        // Timeout related constants
        private const int TIMEOUT_INITIAL_DELAY = 2000;
        private const int TIMEOUT_PERIOD = 1000;
        private const int MAX_MISSED_SYNCS = 10;

        /// <summary>
        /// Number of packets received by the server.
        /// </summary>
        public long PacketsReceived
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of packets sent by the server.
        /// </summary>
        public long PacketsSent
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of packets processed by the server.
        /// </summary>
        public long PacketsProcessed
        {
            get;
            private set;
        }
        
        #endregion

        #region Internal Code

        /// <summary>
        /// Create a new network server.
        /// </summary>
        /// <param name="gameDataUpdater">The function to use to update the game when receiving game packets.</param>
        /// <param name="port">The port to create the network server on.</param>
        public NetworkServer(int port) : base(NetworkStateMachine.NetworkState.SERVERSTART)
        {
            connections = new Dictionary<IPEndPoint, ConnectionID>();

            Debug.WriteLine("Starting Server");

            networkWorker = new NetworkWorker(port);
            networkThread.Start();
            return;
        }

        /// <summary>
        /// Servers main event loop
        /// </summary>
        protected override void NetworkMain()
        {
            TimeoutTimer = new Timer(TimeoutTimerTicked, new AutoResetEvent(false), TIMEOUT_INITIAL_DELAY, TIMEOUT_PERIOD);

            while (continueRunning)
            {
                NetworkPacket packet = networkWorker.ReceivePacket();
                NetworkStateMachine.TransitionEvent transitionEvent = NetworkStateMachine.TransitionEvent.SERVERSTARTED;

                if (packet == null) // Server has timed out, should query clients to verify client health
                {
                    continue;
                }

                PacketsReceived++;

                //Switch on the packet type to create the correct state transition
                switch (packet.Type)
                {
                    case NetworkPacket.PacketType.HANDSHAKE:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERHANDSHAKE;
                        break;
                    case NetworkPacket.PacketType.SYNC:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERSYNC;
                        break;
                    case NetworkPacket.PacketType.PING:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERPING;
                        break;
                    case NetworkPacket.PacketType.GAMEDATA:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERGAMEDATA;
                        break;
                    default:
                        continue;
                }
                networkStateMachine.DoTransition(transitionEvent, packet);
                PacketsProcessed++;
            }
            return;
        }

        /// <summary>
        /// Sets up the state machine for the main server
        /// </summary>
        protected override void RegisterStateMachineTransitions()
        {
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERSTART, NetworkStateMachine.TransitionEvent.SERVERSTARTED, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, delegate { });
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERHANDSHAKE, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, OnHandshake);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERGAMEDATA, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, OnGameData);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERPING, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, OnPing);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, OnSync);
            networkStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.SERVERSTARTED, null);
            return;
        }

        /// <summary>
        /// Shutdown this particular network server.
        /// </summary>
        protected override void Shutdown()
        {
            TimeoutTimer.Dispose();
            base.Shutdown();
        }

        /// <summary>
        /// The sync timer timed out, we need to send out
        /// Syncs to all players
        /// </summary>
        /// <param name="eventArgs">The event arguments for this timer tick.</param>
        private void TimeoutTimerTicked(Object eventArgs)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(SendSyncOnTick));
            return;
        }

        /// <summary>
        /// Send Sync packets based on timer ticks.
        /// </summary>
        /// <param name="eventArgs">The event arguments passed into this function.</param>
        private void SendSyncOnTick(Object eventArgs)
        {
            List<ConnectionID> rmList = new List<ConnectionID>();
            foreach (IPEndPoint ep in connections.Keys)
            {
                SyncPacket syncPacket = new SyncPacket(ep);
                networkWorker.SendPacket(syncPacket);
                PacketsSent++;
                connections[ep].ChangeState(NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING);
                if (connections[ep].MissedSyncs >= MAX_MISSED_SYNCS)
                {
                    rmList.Add(connections[ep]);
                }
            }
            for (int i = 0; i < rmList.Count; i++)
            {
                connections.Remove(rmList[i].IPEndPoint);
            }
            return;
        }

        /// <summary>
        /// After receiving a handshake, respond to a client with another handshake.
        /// </summary>
        /// <param name="packet"></param>
        private void OnHandshake(NetworkPacket packet)
        {
            if (!connections.ContainsKey(packet.Destination))
            {
                Debug.WriteLine("Server - New connection from: " + packet.Destination);
                connections[packet.Destination] = new ConnectionID(packet.Destination);
                Debug.WriteLine("Server - Added Connection: " + connections[packet.Destination].ID);
                HandshakePacket hs = new HandshakePacket(packet.Destination);
                networkWorker.SendPacket(hs);
                PacketsSent++;
            }
            return;
        }

        /// <summary>
        /// Handles reception of game data updates from the client.
        /// </summary>
        /// <param name="packet">A packet that contains game data information.</param>
        protected override void OnGameData(NetworkPacket packet)
        {
            GameData gameData = new GameData(packet.DataArray);
            gameData.ConnectionInfo = connections[packet.Destination];
            gameDataUpdater(gameData);
            return;
        }

        /// <summary>
        /// Callback function on receipt of a ping.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        protected override void OnPing(NetworkPacket packet)
        {
            PingPacket ps = new PingPacket(packet.Destination);
            networkWorker.SendPacket(ps); //ACK the ping
            PacketsSent++;
            return;
        }

        /// <summary>
        /// Callback function on receipt of a SYNC packet.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        protected override void OnSync(NetworkPacket packet)
        {
            connections[packet.Destination].ChangeState(NetworkStateMachine.TransitionEvent.SERVERSYNC);
            return;
        }

        #endregion

        #region Server Public interfaces

        /// <summary>
        /// Broadcast a list of game data to all clients.
        /// </summary>
        /// <param name="gameDataList">The list of game data to broadcast.</param>
        public void BroadCastGameData(List<GameData> gameDataList)
        {
            for (int i = 0; i < gameDataList.Count; i += 1)
            {
                BroadCastGameData(gameDataList[i]);
            }
            return;
        }

        /// <summary>
        /// Broadcast a unit of game data to all users.
        /// </summary>
        /// <param name="gameData">The unit of game data to broadcast.</param>
        public void BroadCastGameData(GameData gameData)
        {
            foreach (ConnectionID connection in connections.Values)
            {
                SendGameData(gameData, connection);
            }
            return;
        }

        /// <summary>
        /// Send a list of game data to a particular client.
        /// </summary>
        /// <param name="gameDataList">The list of game data to send.</param>
        /// <param name="connectionID">The ID of the client to send the game data to.</param>
        public void SendGameData(List<GameData> gameDataList, ConnectionID connectionID)
        {
            for (int i = 0; i < gameDataList.Count; i += 1)
            {
                SendGameData(gameDataList[i], connectionID);
            }
            return;
        }

        /// <summary>
        /// Send a unit of game data to a particular client.
        /// </summary>
        /// <param name="gameData">The unit of game data to send.</param>
        /// <param name="connectionID">The ID of the client to send the game data to.</param>
        public void SendGameData(GameData gameData, ConnectionID connectionID)
        {
            networkWorker.SendPacket(new GameDataPacket(connectionID.IPEndPoint, gameData));
            return;
        }

        #endregion

    }

}
