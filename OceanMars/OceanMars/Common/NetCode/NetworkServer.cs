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

        private Timer TimeoutTimer;

        private ServerStats globalStats = new ServerStats();

        private NetworkStateMachine serverStateMachine;

        //Connection state
        Dictionary<IPEndPoint, ConnectionID> connections = new Dictionary<IPEndPoint, ConnectionID>();
        List<Command> commandQ = new List<Command>();
        Queue<Tuple<ConnectionID, MenuState>> mscQ = new Queue<Tuple<ConnectionID, MenuState>>();


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

            Debug.WriteLine("Starting Server");
            this.nw = new NetworkWorker(port);
            serverThread.Start();

            serverStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.SERVERSTARTED, null);

            TimeoutTimer = new Timer(TimeoutTimerTicked, new AutoResetEvent(false), TIMEOUT_INITIAL_DELAY, TIMEOUT_PERIOD);
        }

        /// <summary>
        /// The sync timer timed out, we need to send out
        /// Syncs to all players
        /// </summary>
        /// <param name="eventArgs"></param>
        private void TimeoutTimerTicked(Object eventArgs)
        {
            foreach (IPEndPoint ep in connections.Keys)
            {
                SyncPacket ps = new SyncPacket(ep);
                this.nw.SendPacket(ps);
                this.globalStats.sentPkts++;
                //connections[ep].changeState(NetworkStateMachine.TransitionEvent.CONN
            }
            return;
        }

        /// <summary>
        /// Sets up the state machine for the main server
        /// </summary>
        private void initializeStateMachine()
        {
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERSTART, NetworkStateMachine.TransitionEvent.SERVERSTARTED, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, delegate { });
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERHANDSHAKE, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onHandshake);
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERCOMMAND, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onCommand);
            serverStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, NetworkStateMachine.TransitionEvent.SERVERMENUSTATECHANGE, NetworkStateMachine.NetworkState.SERVERACCEPTCONNECTIONS, onMenuStateChange);
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
        /// Server receives a Command packet
        /// This needs to be added to the commandQ and handled
        /// </summary>
        /// <param name="packet"></param>
        private void onCommand(NetworkPacket packet)
        {
            Command cmd = new Command(packet.DataArray);
            lock (commandQ)
                this.commandQ.Add(cmd);
        }

        /// <summary>
        /// Handles Menu state changes from clients
        /// </summary>
        /// <param name="packet"></param>
        private void onMenuStateChange(NetworkPacket packet)
        {
            MenuState msc = new MenuState(packet.DataArray);
            if (connections.ContainsKey(packet.Destination))
            {
                ConnectionID cid = connections[packet.Destination];
                Tuple<ConnectionID, MenuState> newMQ = new Tuple<ConnectionID, MenuState>(cid, msc);
                lock (mscQ)
                    this.mscQ.Enqueue(newMQ);
            }
        }

        /// <summary>
        /// Handles Sync acks from clients
        /// </summary>
        /// <param name="packet"></param>
        private void onSync(NetworkPacket packet)
        {
            //TODO
        }

        /// <summary>
        /// Handles the acking of a ping from clients
        /// </summary>
        /// <param name="packet"></param>
        private void onPing(NetworkPacket packet)
        {
            PingPacket ps = new PingPacket(packet.Destination);
            nw.SendPacket(ps); //ACK the ping
            this.globalStats.sentPkts++;
            connections[packet.Destination].changeState(NetworkStateMachine.TransitionEvent.SERVERPING);
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
                    case NetworkPacket.PacketType.COMMAND:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERCOMMAND;
                        break;
                    case NetworkPacket.PacketType.SYNC:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERSYNC;
                        break;
                    case NetworkPacket.PacketType.PING:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERPING;
                        break;
                    case NetworkPacket.PacketType.MENUSTATECHANGE:
                        transitionEvent = NetworkStateMachine.TransitionEvent.SERVERMENUSTATECHANGE;
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

        public List<Command> getCMD()
        {
            List<Command> ret=new List<Command>();
            lock (commandQ)
            {
                foreach (Command c in this.commandQ)
                    ret.Add(c);
                commandQ.Clear();
            }
            return ret;
        }

        public List<Tuple<ConnectionID, MenuState>> getMSC()
        {
            List<Tuple<ConnectionID, MenuState>> ret = new List<Tuple<ConnectionID, MenuState>>();

            lock (mscQ)
            {
                while (mscQ.Count > 0)
                {
                    ret.Add(mscQ.Dequeue());
                }
            }

            return ret;
        }

        public void broadcastSC(List<StateChange> list)
        {
            //Console.WriteLine("Sending # of SCs: {0}", list.Count);
            foreach (StateChange sc in list)
            {
                foreach (KeyValuePair<IPEndPoint, ConnectionID> d in connections)
                {
                    //Console.WriteLine("Server - Sent StateChange to: " + d.Value.ID);
                    StateChangePacket p = new StateChangePacket(d.Key, sc);
                    this.nw.SendPacket(p);
                }
            }
        }

        public void signalSC(List<StateChange> list, ConnectionID cid)
        {
            foreach (StateChange sc in list)
            {
                StateChangePacket p = new StateChangePacket(cid.endpt, sc);
                nw.SendPacket(p);
            }
        }

        public void broadcastMSC(List<MenuState> list)
        {
            foreach (MenuState msc in list)
            {
                broadcastMSC(msc);
            }
            return;
        }

        public void broadcastMSC(MenuState menuState)
        {
            foreach (KeyValuePair<IPEndPoint, ConnectionID> d in connections)
            {
                MenuStateChangePacket p = new MenuStateChangePacket(d.Key, menuState);
                this.nw.SendPacket(p);
            }
            return;
        }

        public void signalMSC(List<MenuState> list, ConnectionID cid)
        {
            foreach (MenuState m in list)
            {
                signalMSC(m, cid);
            }
        }

        public void signalMSC(MenuState menuState, ConnectionID connectionID)
        {
            MenuStateChangePacket p = new MenuStateChangePacket(connectionID.endpt, menuState);
            nw.SendPacket(p);
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

        private NetworkStateMachine StateMachine;

        public ConnectionID(IPEndPoint ep)
        {
            StateMachine = new NetworkStateMachine(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED);
            initStateMachine();

            ID = ids++;
            this.endpt = ep;
        }

        private void initStateMachine()
        {
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CONNECTIONDISCONNECT, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CONNECTIONTIMEOUT, NetworkStateMachine.NetworkState.CONNECTIONTIMEOUT, delegate { });
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONTIMEOUT, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, delegate { });
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.SERVERPING, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, onPing);
        }

        public void changeState(NetworkStateMachine.TransitionEvent transitionEvent)
        {
            StateMachine.DoTransition(transitionEvent, null);
        }

        private void onPing(NetworkPacket packet)
        {
            this.lastSYNC = 1;
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
