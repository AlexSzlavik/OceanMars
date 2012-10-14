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

    #region Server Internals

    public class NetworkServer : NetworkBase
    {

        #region Member Variables

        private Thread serverThread; // Thread used to host the server
        private Timer TimeoutTimer;
        private ServerStats serverStats;
        private Dictionary<IPEndPoint, ConnectionID> connections;

        // Timeout related constants
        private const int TIMEOUT_INITIAL_DELAY = 2000;
        private const int TIMEOUT_PERIOD = 1000;
        private const int MAX_MISSED_SYNCS = 10;
        
        #endregion

        /// <summary>
        /// Create a new network server.
        /// </summary>
        /// <param name="gameDataUpdater">The function to use to update the game when receiving game packets.</param>
        /// <param name="port">The port to create the network server on.</param>
        public NetworkServer(int port) : base(NetworkStateMachine.NetworkState.SERVERSTART)
        {
            serverStats = new ServerStats(); // Instantiate some basic variables
            connections = new Dictionary<IPEndPoint, ConnectionID>();

            Debug.WriteLine("Starting Server");

            serverThread = new Thread(ServerMainLoop); // Create the new thread and associated network worker
            serverThread.IsBackground = true;
            networkWorker = new NetworkWorker(port);
            serverThread.Start();
            return;
        }

        /// <summary>
        /// The sync timer timed out, we need to send out
        /// Syncs to all players
        /// </summary>
        /// <param name="eventArgs">The event arguments for this timer tick.</param>
        private void TimeoutTimerTicked(Object eventArgs)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(SendTimerSync));
            return;
        }

        /// <summary>
        /// Send Sync packets based on timer ticks.
        /// </summary>
        /// <param name="eventArgs">The event arguments passed into this function.</param>
        private void SendTimerSync(Object eventArgs)
        {
            List<ConnectionID> rmList = new List<ConnectionID>();
            foreach (IPEndPoint ep in connections.Keys)
            {
                SyncPacket ps = new SyncPacket(ep);
                this.networkWorker.SendPacket(ps);
                this.serverStats.sentPkts++;
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
                this.serverStats.sentPkts++;
            }
            return;
        }

        /// <summary>
        /// Handles reception of game data updates from the client.
        /// </summary>
        /// <param name="packet">A packet that contains game data information.</param>
        private void OnGameData(NetworkPacket packet)
        {
            GameData gameData = new GameData(packet.DataArray);
            gameData.ConnectionInfo = connections[packet.Destination];
            gameDataUpdater(gameData);
            return;
        }

        /// <summary>
        /// Handles Sync acks from clients
        /// </summary>
        /// <param name="packet"></param>
        private void OnSync(NetworkPacket packet)
        {
            connections[packet.Destination].ChangeState(NetworkStateMachine.TransitionEvent.SERVERSYNC);
            return;
        }

        /// <summary>
        /// Handles the acking of a ping from clients
        /// </summary>
        /// <param name="packet"></param>
        private void OnPing(NetworkPacket packet)
        {
            PingPacket ps = new PingPacket(packet.Destination);
            networkWorker.SendPacket(ps); //ACK the ping
            this.serverStats.sentPkts++;
            return;
        }

        /// <summary>
        /// Gracefully shut down the network server.
        /// </summary>
        public void Exit()
        {
            this.continueRunning = false;
            return;
        }

        /// <summary>
        /// Servers main event loop
        /// </summary>
        private void ServerMainLoop()
        {
            NetworkPacket packet;
            TimeoutTimer = new Timer(TimeoutTimerTicked, new AutoResetEvent(false), TIMEOUT_INITIAL_DELAY, TIMEOUT_PERIOD);
            
            while (this.continueRunning)
            {
                packet = networkWorker.ReceivePacket();
                NetworkStateMachine.TransitionEvent transitionEvent = NetworkStateMachine.TransitionEvent.SERVERSTARTED;

                //Special case, we timed out
                //Should query all Clients to make sure they are still alive
                if (packet == null)
                {
                    continue;
                }

                //We actually received something, increase stats
                this.serverStats.rcvdPkts++;

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
                this.networkStateMachine.DoTransition(transitionEvent, packet);
                this.serverStats.pktsProcessed++;
            }
            return;
        }

    #endregion

        #region Server Public interfaces

        public ServerStats getStats()
        {
            return this.serverStats;
        }

        public void BroadCastGameData(List<GameData> gameDataList)
        {
            foreach (GameData gameData in gameDataList)
            {
                BroadCastGameData(gameData);
            }
            return;
        }

        public void BroadCastGameData(GameData gameData)
        {
            foreach (ConnectionID connection in connections.Values)
            {
                SignalGameData(gameData, connection);
            }
            return;
        }

        public void SignalGameData(List<GameData> gameDataList, ConnectionID cid)
        {
            foreach (GameData gameData in gameDataList)
            {
                SignalGameData(gameData, cid);
            }
            return;
        }

        public void SignalGameData(GameData gameData, ConnectionID connectionID)
        {
            networkWorker.SendPacket(new GameDataPacket(connectionID.IPEndPoint, gameData));
            return;
        }

    }
        #endregion

    public class ServerStats
    {
        public long rcvdPkts = 0;
        public long sentPkts = 0;
        public long pktsProcessed = 0;
    }

}
