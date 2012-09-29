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


    public class NetworkServer
    {
        private Thread serverThread;
        private bool go = true;
        private NetworkWorker nw;

        private ServerStats globalStats = new ServerStats();

        //Connection state
        Dictionary<IPEndPoint, ConnectionID> connections = new Dictionary<IPEndPoint, ConnectionID>();
        List<Command> commandQ = new List<Command>();
        Queue<Tuple<ConnectionID, MenuState>> mscQ = new Queue<Tuple<ConnectionID, MenuState>>();

        public NetworkServer(int port)
        {
            serverThread = new Thread(runThis);
            serverThread.Name = "Main Server";
            serverThread.Priority = ThreadPriority.AboveNormal;
            serverThread.IsBackground = true;

            Debug.WriteLine("Starting Server");
            this.nw = new NetworkWorker(port);
            serverThread.Start();
        }

        public void exit()
        {
            this.go = false;
        }

        private void runThis()
        {
            NetworkPacket p;
            while (this.go)
            {
                p = nw.ReceivePacket();
                if (p == null)
                {
                    foreach (IPEndPoint ep in connections.Keys)
                    {
                        SyncPacket ps = new SyncPacket(ep);
                        this.nw.SendPacket(ps);
                    }
                    continue;
                }
                //Console.WriteLine(p.ptype);

                switch (p.Type)
                {
                    case NetworkPacket.PacketType.HANDSHAKE:
                        if (!connections.ContainsKey(p.Destination))
                        {
                            Debug.WriteLine("Server - New connection from: " + p.Destination);
                            connections[p.Destination] = new ConnectionID(p.Destination);
                            Debug.WriteLine("Server - Added Connection: " + connections[p.Destination].ID);
                            HandshakePacket hs = new HandshakePacket(p.Destination);
                            nw.SendPacket(hs);
                        }
                        break;

                    case NetworkPacket.PacketType.STATECHANGE:
                        //Console.WriteLine("Server - Receivd State Change from client... who do they think they are?");
                        Environment.Exit(1);
                        break;

                    case NetworkPacket.PacketType.SYNC:
                        if (connections.ContainsKey(p.Destination))
                        {
                            //Console.WriteLine("Server - SYNC Reply from: " + connections[p.Dest].ID);
                        }
                        else
                        {
                            Debug.WriteLine("Server - ERROR Unregistered SYNC");
                        }
                        break;

                    case NetworkPacket.PacketType.PING:
                        if (connections.ContainsKey(p.Destination))
                        {
                            //Console.WriteLine("Server - Ping from connection: " + connections[p.Dest].ID);
                            PingPacket ps = new PingPacket(p.Destination);
                            nw.SendPacket(ps); //ACK the ping
                        }
                        else
                        {
                            Debug.WriteLine("Server ERROR - Unregistered PING");
                        }
                        break;

                    case NetworkPacket.PacketType.COMMAND:
                        //Actually handle this
                        //Console.WriteLine("Server - Got CMD from: " + connections[p.Dest].ID);
                        Command cmd = new Command(p.DataArray);
                        lock (commandQ)
                            this.commandQ.Add(cmd);
                        break;
                    case NetworkPacket.PacketType.MENUSTATECHANGE:
                        //Actually handle this
                        //Console.WriteLine("Server - Got MSC from: " + connections[p.Dest].ID);
                        MenuState msc = new MenuState(p.DataArray);
                        if (connections.ContainsKey(p.Destination))
                        {
                            ConnectionID cid = connections[p.Destination];
                            Tuple<ConnectionID, MenuState> newMQ = new Tuple<ConnectionID, MenuState>(cid, msc);
                            lock (mscQ)
                                this.mscQ.Enqueue(newMQ);
                        }
                        break;
                }
                this.globalStats.rcvdPkts++;
            }
        }

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

    public class ConnectionID
    {
        private static short ids = 0;
        public short ID;
        public IPEndPoint endpt;
        public long lastSYNC = -1;

        public ConnectionID(IPEndPoint ep)
        {
            ID = ids++;
            this.endpt = ep;
        }
    }

    public class ServerStats 
    {
        public long rcvdPkts = 0;
        public long sentPkts = 0;
        public long pktsProcessed = 0;
    }
}
