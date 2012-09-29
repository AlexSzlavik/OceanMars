using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Class representing a UPD packet-based client.
    /// </summary>
    public class RawClient
    {

        private Thread clientThread;
        private NetworkWorker nw;
        private IPEndPoint server;
        private bool go = true;
        private NetStateMachine clientStateMachine;
        public enum cState { DISCONNECTED, CONNECTED, TRYCONNECT };
        cState curState = cState.DISCONNECTED;

        
        private Stopwatch pingStopwatch = new Stopwatch();
        private long lastPing = -1;
        private Timer PingPacket_timeout_timer;

        // Semaphore to wait on for the server info to be known
        private Semaphore ready = new Semaphore(0, 1);
        private Semaphore connected = new Semaphore(0, 1);

        // Queue of state changes to be passed off the the UI
        private Queue<StateChange> buffer = new Queue<StateChange>();

        // Queue of menu changes to be passed off to the menu
        private Queue<MenuState> menuBuffer = new Queue<MenuState>();

        /// <summary>
        /// Create a new raw client.
        /// </summary>
        public RawClient()
        {
            Debug.WriteLine("Client Started");

            initStateMachine();

            // Create the thread
            clientThread = new Thread(this.newClientStartFunc);
            clientThread.Name = "mainClientThread";
            clientThread.IsBackground = true;

            clientThread.Start();

            clientStateMachine.DoTransition(NetStateMachine.TransitionEvent.CLIENTSTARTED, null);
        }

        /// <summary>
        /// Create a new state machine and create all transitions
        /// </summary>
        private void initStateMachine()
        {
            clientStateMachine = new NetStateMachine(NetStateMachine.NetState.CLIENTSTART);
            clientStateMachine.RegisterTransition(NetStateMachine.NetState.CLIENTSTART, NetStateMachine.TransitionEvent.CLIENTSTARTED, NetStateMachine.NetState.CLIENTDISCONNECTED, delegate {});
            clientStateMachine.RegisterTransition(NetStateMachine.NetState.CLIENTDISCONNECTED, NetStateMachine.TransitionEvent.CLIENTCONNECT, NetStateMachine.NetState.CLIENTTRYCONNECT, delegate { });
            clientStateMachine.RegisterTransition(NetStateMachine.NetState.CLIENTTRYCONNECT, NetStateMachine.TransitionEvent.CLIENTCONNECTED, NetStateMachine.NetState.CLIENTCONNECTED, onConnect);
            clientStateMachine.RegisterTransition(NetStateMachine.NetState.CLIENTCONNECTED, NetStateMachine.TransitionEvent.CLIENTDOPING, NetStateMachine.NetState.CLIENTCONNECTED, onPing);
        }

        public void onConnect(Packet p)
        {
            connected.Release();
            this.startPing();
        }

        public void onPing(Packet p)
        {
            lock (this)
            {
                pingStopwatch.Stop();
                this.lastPing = pingStopwatch.ElapsedMilliseconds;
            }
        }

        public bool connect(string host, int port)
        {
            // Store server info
            this.server = new IPEndPoint(IPAddress.Parse(host), port);

            //Spawn the client reader/writer threads
            this.nw = new NetworkWorker(server);

            // Client may now try to connect
            this.clientStateMachine.DoTransition(NetStateMachine.TransitionEvent.CLIENTCONNECT, null);

            // Inform the client thread that the server info is ready
            ready.Release();

            // Send the handshake request to the server
            this.handshake();

            // Wait for the connection to be established
            //connect should be a blocking call, so this blocks
            connected.WaitOne();

            return true;
        }

        public void exit()
        {
            this.go = false;
        }

        /// <summary>
        /// Re-implemenation of client with new state machine
        /// </summary>
        private void newClientStartFunc()
        {
            //Wait until the server information has been acquired
            ready.WaitOne();
            NetStateMachine.TransitionEvent transitionEvent = NetStateMachine.TransitionEvent.CLIENTSTARTED;

            //Event loop
            //Pull packets from the network layer and hand them to the state machine
            while (this.go)
            {
                //Pull packet
                Packet newPacket = nw.getNext();
                switch (newPacket.Type)
                {
                    case Packet.PacketType.HANDSHAKE:
                        transitionEvent = NetStateMachine.TransitionEvent.CLIENTCONNECTED;
                        break;
                    case Packet.PacketType.PING:
                        transitionEvent = NetStateMachine.TransitionEvent.CLIENTDOPING;
                        break;
                }
                this.clientStateMachine.DoTransition(transitionEvent,newPacket);    //This is amazing
            }
        }

        //Main routine, this does all the processing
        private void ClientstartFunc()
        {
            // Hold here until the server information has been provided
            ready.WaitOne();

            // Event Loop
            // Pull packets out of the network layer and handle them
            while (this.go)
            {
                Packet newPacket = nw.getNext(); // This is a blocking call! 

                // Handle timeout
                if (newPacket == null)
                {
                    Debug.WriteLine("Timeout on receive");
                    switch (curState)
                    {
                        case cState.TRYCONNECT:
                            // Did not receive the expected HANDSHAKE message
                            // Restart the handshake
                            this.handshake();
                            break;
                        case cState.CONNECTED:
                            // The server may have died, ping the server to find out
                            //this.pingServer();
                   //TODO Should probably not silently ignore this....
                            lock (this)
                            {
                                if (this.pingStopwatch.IsRunning)
                                {
                                    //this.curState = cState.DISCONNECTED;
                                    this.pingStopwatch.Stop();
                                    this.lastPing = 501;
                                }
                            }
                            break;
                        case cState.DISCONNECTED:
                        default:
                            // This should not happen, die screaming!
                            Environment.Exit(1);
                            break;
                    }
                }
                else
                {
                    //Console.Write("Received packet of type: ");
                    //Console.WriteLine(newPacket.ptype);

                    // Handle the new packet 
                    switch (newPacket.Type)
                    {
                        case Packet.PacketType.COMMAND:
                            Debug.WriteLine("Should not be getting CMD packets from the server...");
                            Environment.Exit(1);
                            break;
                        case Packet.PacketType.HANDSHAKE:
                            Debug.WriteLine("Handshake received from the server");

                            switch (curState)
                            {
                                case cState.TRYCONNECT:
                                    // The connection has succeeded!
                                    this.startPing();
                                    this.curState = cState.CONNECTED;
                                    break;
                                case cState.CONNECTED:
                                    // Repeat? This can be ignored ( I hope...)
                                    break;
                                case cState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case Packet.PacketType.STATECHANGE:
                            //Console.WriteLine("STC received from the server");

                            switch (curState)
                            {
                                case cState.TRYCONNECT:
                                    break;
                                case cState.CONNECTED:
                                    // Marshall the state change packet into an object
                                    StateChange newSTC = new StateChange(newPacket.DataArray);

                                    // Add the state change object to the buffer for the UI
                                    lock (this.buffer)
                                    {
                                        buffer.Enqueue(newSTC);
                                    }

                                    break;
                                case cState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case Packet.PacketType.MENUSTATECHANGE:
                            //Console.WriteLine("MSC received from the server");

                            switch (curState)
                            {
                                case cState.TRYCONNECT:
                                    break;
                                case cState.CONNECTED:
                                    // Marshall the state change packet into an object
                                    MenuState newMSC = new MenuState(newPacket.DataArray);

                                    // Add the state change object to the buffer for the UI
                                    lock (this.menuBuffer)
                                    {
                                        menuBuffer.Enqueue(newMSC);
                                    }

                                    break;
                                case cState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case Packet.PacketType.SYNC:
                            //Console.WriteLine("SYNC received from the server");
                            
                            switch (curState)
                            {
                                case cState.TRYCONNECT:
                                    break;
                                case cState.CONNECTED:
                                    syncServer();
                                    break;
                                case cState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case Packet.PacketType.PING:
                            //Console.WriteLine("PING received from the server");
                            
                            switch (curState)
                            {
                                case cState.TRYCONNECT:
                                    break;
                                case cState.CONNECTED:
                                    lock (this)
                                    {
                                        pingStopwatch.Stop();
                                        this.lastPing = pingStopwatch.ElapsedMilliseconds;
                                    }
                                    //Console.WriteLine("Ping"+lastPing);
                                    break;
                                case cState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        default:
                            Debug.WriteLine("Unknown packet type from the server...");
                            Environment.Exit(1);
                            break;
                    }
                }
            }
        }

        private void handshake()
        {
            HandshakePacket hs = new HandshakePacket(server);
            this.nw.commitPacket(hs);
        }

        private void startPing()
        {
            this.PingPacket_timeout_timer = new Timer(this.pingTimer, new AutoResetEvent(false), 1000, 500);
        }

        private void pingTimer(Object stateInfo)
        {
            lock (this)
            {
                if (pingStopwatch.IsRunning)
                    return;
                //Console.WriteLine("Sending ping");
                PingPacket pingPacket = new PingPacket(server);
                pingStopwatch.Reset();
                this.nw.commitPacket(pingPacket);
                pingStopwatch.Start();
            }
        }

        private void syncServer()
        {
            SyncPacket ss = new SyncPacket(server);
            this.nw.commitPacket(ss);
        }

        //OPERATORS
        public void sendCMD(List<Command> commands)
        {
            foreach (Command command in commands)
            {
                // Create the CMD Packet
                CommandPacket newCMD = new CommandPacket(server, command);

                // Add the CMD packet to the network worker's send queue
                this.nw.commitPacket(newCMD);
            }
        }

        public void sendMSC(List<MenuState> mscs)
        {
            foreach (MenuState m in mscs)
            {
                sendMSC(m);
            }
        }

        public void sendMSC(MenuState menuState)
        {
            // Create the MSC Packet
            MenuStateChangePacket newMSC = new MenuStateChangePacket(server, menuState);

            // Add the MSC packe to the network worker's send queue
            this.nw.commitPacket(newMSC);
        }

        // Called by the UI to acquire the latest state from the server
        public List<StateChange> rcvUPD()
        {
            List<StateChange> newStates = new List<StateChange>();

            // Acquire the buffer lock well emptying the buffer
            lock (this.buffer)
            {
                // Iterate over the buffer of states that have been acquired from the server
                while (buffer.Count > 0)
                {
                    newStates.Add(buffer.Dequeue());
                }
            }

            return newStates;
        }

        // Called by the Menu to acquire the latest menu state for char. selection
        public List<MenuState> rcvMenuState()
        {
            List<MenuState> newStates = new List<MenuState>();

            // Acquire the buffer lock well emptying the buffer
            lock (this.menuBuffer)
            {
                // Iterate over the buffer of states that have been acquired from the server
                while (menuBuffer.Count > 0)
                {
                    newStates.Add(menuBuffer.Dequeue());
                }
            }

            return newStates;
        }

        //Retrieve the last measured ping
        public long getPing()
        {
            lock (this)
                return this.lastPing;
        }
    }
}
