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
    public class NetworkServer
    {
        private Thread serverThread;
        private bool go = true;
        private NetworkWorker nw;
        private const int TIMEOUT_INITIAL_DELAY = 2000;
        private const int TIMEOUT_PERIOD = 1000;
        private const int MAX_MISSED_SYNCS = 10;

        private Timer TimeoutTimer;

        private ServerStats globalStats = new ServerStats();

        private NetworkStateMachine serverStateMachine;

        //Connection state
        Dictionary<IPEndPoint, ConnectionID> connections = new Dictionary<IPEndPoint, ConnectionID>();
        
        /// <summary>
        /// Delegate type used for subscription to game data.
        /// </summary>
        /// <param name="gameData">Game data used to update the state of the game.</param>
        public delegate void GameDataUpdater(GameData gameData);

        /// <summary>
        /// The actual delegate used to update game data when appropraite packets are received.
        /// </summary>
        private GameDataUpdater gameDataUpdater;

        /// <summary>
        /// Server Constructor
        /// </summary>
        /// <param name="port"></param>
        public NetworkServer(int port)
        {
            serverStateMachine = new NetworkStateMachine(NetworkStateMachine.NetworkState.SERVERSTART);
            initializeStateMachine();

            serverThread = new Thread(ServerMainLoop);
            serverThread.Name = "Main Server";
            serverThread.Priority = ThreadPriority.AboveNormal;
            serverThread.IsBackground = true;

            gameDataUpdater = null;

            Debug.WriteLine("Starting Server");
            this.nw = new NetworkWorker(port);
            serverThread.Start();

            serverStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.SERVERSTARTED, null);

            TimeoutTimer = new Timer(TimeoutTimerTicked, new AutoResetEvent(false), TIMEOUT_INITIAL_DELAY, TIMEOUT_PERIOD);
            return;
        }

        /// <summary>
        /// Register a delegate to handle updates to the game.
        /// </summary>
        /// <param name="gameDataUpdater">A delegate function to call when game data is received over the network.</param>
        public void RegisterGameDataUpdater(GameDataUpdater gameDataUpdater)
        {
            this.gameDataUpdater = gameDataUpdater;
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
                this.nw.SendPacket(ps);
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
        private void initializeStateMachine()
        {
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERSTART, NetworkStateMachine.TransitionEvent.SERVERSTARTED, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, delegate { });
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERHANDSHAKE, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onHandshake);
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERGAMEDATA, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, OnGameData);
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERPING, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onPing);
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onSync);
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
                nw.SendPacket(hs);
                this.globalStats.sentPkts++;
            }
        }

        /// <summary>
        /// Handles reception of game data updates from the client.
        /// </summary>
        /// <param name="packet">A packet that contains game data information.</param>
        private void OnGameData(NetworkPacket packet)
        {
            gameDataUpdater(new GameData(packet.DataArray));
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
            //nw.SendPacket(ps); //ACK the ping
            //this.globalStats.sentPkts++;
        }
        
        /// <summary>
        /// Graceful shutdown method
        /// </summary>
        public void exit()
        {
            this.go = false;
        }

        /// <summary>
        /// Servers main event loop
        /// </summary>
        private void ServerMainLoop()
        {
            NetworkPacket packet;
            while (this.go)
            {
                packet = nw.ReceivePacket();
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
                this.serverStateMachine.DoTransition(transitionEvent, packet);
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
            nw.SendPacket(new GameDataPacket(connectionID.endpt, gameData));
        }

    }
#endregion

#region Server Connection Classes

    public class ConnectionID
    {
        private static short ids = 0;
        public short ID;
        public IPEndPoint endpt;
        public long lastSYNC = -1;

        public int MissedSyncs
        {
            get;
            private set;
        }

        private NetworkStateMachine StateMachine;

        public ConnectionID(IPEndPoint ep)
        {
            StateMachine = new NetworkStateMachine(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED);
            initStateMachine();
            MissedSyncs = 0; 

            ID = ids++;
            this.endpt = ep;
        }

        private void initStateMachine()
        {
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CONNECTIONDISCONNECT, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CONNECTIONTIMEOUT, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONTIMEOUT, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, onSync );
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONTIMEOUT, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, delegate { });
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, onSync);
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, onMissingSync);
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, NetworkStateMachine.TransitionEvent.CONNECTIONTIMEOUT, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
        }

        public void changeState(NetworkStateMachine.TransitionEvent transitionEvent)
        {
            lock (this)
            {
                StateMachine.DoTransition(transitionEvent, null);
            }
        }

        private void onSync(NetworkPacket packet)
        {
            lock (this)
            {
                this.lastSYNC = 1;
                this.MissedSyncs = 0;
            }
        }

        /// <summary>
        /// Keep track of the number of Syncs we have missed
        /// and eventually transition into the TIMEOUT state
        /// </summary>
        /// <param name="packet"></param>
        private void onMissingSync(NetworkPacket packet)
        {
            lock (this)
            {
                MissedSyncs += 1;
                Debug.WriteLine(String.Format("Missed {0} SYNCS", MissedSyncs));
            }
        }

        public bool isConnected()
        {
            lock (this)
            {
                return StateMachine.CurrentState != NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED;
            }
        }
    }

    public class ServerStats 
    {
        public long rcvdPkts = 0;
        public long sentPkts = 0;
        public long pktsProcessed = 0;
    }

#endregion
}
