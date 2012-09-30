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

        // Timeout related constants
        private const int TIMEOUT_INITIAL_DELAY = 2000;
        private const int TIMEOUT_PERIOD = 1000;
        private const int MAX_MISSED_SYNCS = 10;

        private Timer TimeoutTimer;

        private ServerStats globalStats = new ServerStats();

        #endregion

        //Connection state
        Dictionary<IPEndPoint, ConnectionID> connections = new Dictionary<IPEndPoint, ConnectionID>();

        /// <summary>
        /// Create a new network server.
        /// </summary>
        /// <param name="gameDataUpdater">The function to use to update the game when receiving game packets.</param>
        /// <param name="port">The port to create the network server on.</param>
        public NetworkServer(int port)
            : base(NetworkStateMachine.NetworkState.SERVERSTART)
        {
            serverThread = new Thread(ServerMainLoop);
            serverThread.Name = "Main Server";
            serverThread.Priority = ThreadPriority.AboveNormal;
            serverThread.IsBackground = true;

            gameDataUpdater = null;

            Debug.WriteLine("Starting Server");
            networkWorker = new NetworkWorker(port);
            serverThread.Start();

            networkStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.SERVERSTARTED, null);

            TimeoutTimer = new Timer(TimeoutTimerTicked, new AutoResetEvent(false), TIMEOUT_INITIAL_DELAY, TIMEOUT_PERIOD);
            return;
        }

        /// <summary>
        /// The sync timer timed out, we need to send out
        /// Syncs to all players
        /// </summary>
        /// <param name="eventArgs"></param>
        private void TimeoutTimerTicked(Object eventArgs)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(timerWork));
            return;
        }

        private void timerWork(Object data)
        {
            List<ConnectionID> rmList = new List<ConnectionID>();
            foreach (IPEndPoint ep in connections.Keys)
            {
                SyncPacket ps = new SyncPacket(ep);
                this.networkWorker.SendPacket(ps);
                this.globalStats.sentPkts++;
                connections[ep].changeState(NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING);
                if (connections[ep].MissedSyncs >= MAX_MISSED_SYNCS)
                    rmList.Add(connections[ep]);
            }
            foreach (ConnectionID con in rmList)
                connections.Remove(con.endpt);
        }


        /// <summary>
        /// Sets up the state machine for the main server
        /// </summary>
        protected override void RegisterStateMachineTransitions()
        {
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERSTART, NetworkStateMachine.TransitionEvent.SERVERSTARTED, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, delegate { });
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERHANDSHAKE, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onHandshake);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERGAMEDATA, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, OnGameData);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERPING, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onPing);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onSync);
            return;
        }

        /// <summary>
        /// A client wants to connect, so they initiated a handshake
        /// </summary>
        /// <param name="packet"></param>
        private void onHandshake(NetworkPacket packet)
        {
            if (!connections.ContainsKey(packet.Destination))
            {
                Debug.WriteLine("Server - New connection from: " + packet.Destination);
                connections[packet.Destination] = new ConnectionID(packet.Destination);
                Debug.WriteLine("Server - Added Connection: " + connections[packet.Destination].ID);
                HandshakePacket hs = new HandshakePacket(packet.Destination);
                networkWorker.SendPacket(hs);
                this.globalStats.sentPkts++;
            }
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
        private void onSync(NetworkPacket packet)
        {
            connections[packet.Destination].changeState(NetworkStateMachine.TransitionEvent.SERVERSYNC);
        }

        /// <summary>
        /// Handles the acking of a ping from clients
        /// </summary>
        /// <param name="packet"></param>
        private void onPing(NetworkPacket packet)
        {
            PingPacket ps = new PingPacket(packet.Destination);
            networkWorker.SendPacket(ps); //ACK the ping
            this.globalStats.sentPkts++;
        }

        /// <summary>
        /// Graceful shutdown method
        /// </summary>
        public void exit()
        {
            this.continueRunning = false;
        }

        /// <summary>
        /// Servers main event loop
        /// </summary>
        private void ServerMainLoop()
        {
            NetworkPacket packet;
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
                this.globalStats.rcvdPkts++;

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
                this.globalStats.pktsProcessed++;
            }
        }

    #endregion

        #region Server Public interfaces

        public ServerStats getStats()
        {
            return this.globalStats;
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
        }

        public void SignalGameData(GameData gameData, ConnectionID connectionID)
        {
            networkWorker.SendPacket(new GameDataPacket(connectionID.endpt, gameData));
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
