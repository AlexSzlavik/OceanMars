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
    public class NetworkClient
    {

        // Data used to control addressing and threading in the network clinet
        private Thread clientThread; // Thread used to host a network client
        private NetworkWorker networkWorker; // The worker thread used to send and receive data
        private IPEndPoint serverEndPoint; // The address of the server
        private bool continueRunning; // Whether or not to continue the client

        // Data used to control the state of the network client
        private NetworkStateMachine clientStateMachine; // A network state machine run on the client
        private enum NetworkClientState { DISCONNECTED, CONNECTED, TRYCONNECT };
        private NetworkClientState currentState;

        // Data used to control ping flow (heartbeats) in the network client
        private Stopwatch pingStopwatch;
        private long lastPing;
        private Timer pingPacketTimer;

        // Semaphore to wait on for the server info to be known
        private Semaphore serverReadySemaphore, clientConnectedSemaphore;

        // Queues used to buffer game state changes that have been received
        private Queue<StateChange> uiStateBuffer;
        private Queue<MenuState> menuStateBuffer;

        private const int PING_INITIAL_DELAY = 1000; // Amount of time to wait before starting to ping heartbeats
        private const int PING_PERIOD = 500; // Amount of time to wait between ping heartbeats

        /// <summary>
        /// Create a new raw client.
        /// </summary>
        public NetworkClient()
        {
            // Set up some member variables
            continueRunning = true;
            pingStopwatch = new Stopwatch();
            lastPing = -1;
            pingPacketTimer = null;
            currentState = NetworkClientState.DISCONNECTED;

            // Set up locking primatives and state queues
            serverReadySemaphore = new Semaphore(0, 1);
            clientConnectedSemaphore = new Semaphore(0, 1);
            uiStateBuffer = new Queue<StateChange>();
            menuStateBuffer = new Queue<MenuState>();

            InitStateMachine(); // Set up the state machine

            // Create the actual client thread
            clientThread = new Thread(RunNetworkClientThread);
            clientThread.IsBackground = true;
            clientThread.Start();

            clientStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.CLIENTSTARTED, null);
            return;
        }

        /// <summary>
        /// Create a new state machine and register all transitions associated with network clients.
        /// </summary>
        private void InitStateMachine()
        {
            clientStateMachine = new NetworkStateMachine(NetworkStateMachine.NetworkState.CLIENTSTART);
            clientStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTSTART, NetworkStateMachine.TransitionEvent.CLIENTSTARTED, NetworkStateMachine.NetworkState.CLIENTDISCONNECTED, delegate {});
            clientStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTDISCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTCONNECT, NetworkStateMachine.NetworkState.CLIENTTRYCONNECT, delegate { });
            clientStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTTRYCONNECT, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED, NetworkStateMachine.NetworkState.CLIENTCONNECTED, OnConnect);
            clientStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTDROPPING, NetworkStateMachine.NetworkState.CLIENTCONNECTED, OnPing);
            return;
        }

        /// <summary>
        /// Transition action that occurs on connecting to a server.
        /// </summary>
        /// <param name="packet">The network packet retrieved during this transition.</param>
        public void OnConnect(NetworkPacket packet)
        {
            clientConnectedSemaphore.Release();
            StartPinging();
            return;
        }

        /// <summary>
        /// Transition action that occurs on pinging.
        /// </summary>
        /// <param name="packet">The network packet retrieved during this transition.</param>
        public void OnPing(NetworkPacket packet)
        {
            lock (this)
            {
                pingStopwatch.Stop();
                lastPing = pingStopwatch.ElapsedMilliseconds;
            }
            return;
        }

        /// <summary>
        /// Connect to the game server.
        /// </summary>
        /// <param name="host">The name of the host to connect to.</param>
        /// <param name="port">The port on the host to connect to.</param>
        /// <returns>Returns a boolean representing whether or not the connection was succesful.</returns>
        public bool Connect(String host, int port)
        {
            try
            {
                serverEndPoint = new IPEndPoint(IPAddress.Parse(host), port); // Store server info
                networkWorker = new NetworkWorker();//Spawn the client reader/writer threads
                clientStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.CLIENTCONNECT, null); // Client may now try to connect
                serverReadySemaphore.Release(); // Inform the client thread that the server info is ready
                SendHandshake(); // Send the handshake request to the server
                clientConnectedSemaphore.WaitOne(); // Wait for the connection to be established
                return true;
            }
            catch (Exception error)
            {
                Debug.WriteLine("Unable to connect to server: {0}", error.Message);
                return false;
            }
        }

        /// <summary>
        /// Terminate the client and any associated network worker threads.
        /// </summary>
        public void Exit()
        {
            continueRunning = false;
            if (networkWorker != null)
            {
                networkWorker.Exit();
            }
            return;
        }

        /// <summary>
        /// Main execution loop for network clients.
        /// </summary>
        private void RunNetworkClientThread()
        {
            serverReadySemaphore.WaitOne();
            NetworkStateMachine.TransitionEvent transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTSTARTED;

            while (continueRunning) // Loop and push packets into the state machine
            {
                NetworkPacket receivePacket = networkWorker.ReceivePacket(); // Grab a packet from the server
                switch (receivePacket.Type)
                {
                    case NetworkPacket.PacketType.HANDSHAKE:
                        transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTCONNECTED;
                        break;
                    case NetworkPacket.PacketType.PING:
                        transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTDROPPING;
                        break;
                }
                clientStateMachine.DoTransition(transitionEvent, receivePacket); // This is amazing
            }
        }

        //Main routine, this does all the processing
        private void ClientstartFunc()
        {
            // Hold here until the server information has been provided
            serverReadySemaphore.WaitOne();

            // Event Loop
            // Pull packets out of the network layer and handle them
            while (this.continueRunning)
            {
                NetworkPacket newPacket = networkWorker.ReceivePacket(); // This is a blocking call! 

                // Handle timeout
                if (newPacket == null)
                {
                    Debug.WriteLine("Timeout on receive");
                    switch (currentState)
                    {
                        case NetworkClientState.TRYCONNECT:
                            // Did not receive the expected HANDSHAKE message
                            // Restart the handshake
                            this.SendHandshake();
                            break;
                        case NetworkClientState.CONNECTED:
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
                        case NetworkClientState.DISCONNECTED:
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
                        case NetworkPacket.PacketType.COMMAND:
                            Debug.WriteLine("Should not be getting CMD packets from the server...");
                            Environment.Exit(1);
                            break;
                        case NetworkPacket.PacketType.HANDSHAKE:
                            Debug.WriteLine("Handshake received from the server");

                            switch (currentState)
                            {
                                case NetworkClientState.TRYCONNECT:
                                    // The connection has succeeded!
                                    this.StartPinging();
                                    this.currentState = NetworkClientState.CONNECTED;
                                    break;
                                case NetworkClientState.CONNECTED:
                                    // Repeat? This can be ignored ( I hope...)
                                    break;
                                case NetworkClientState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case NetworkPacket.PacketType.STATECHANGE:
                            //Console.WriteLine("STC received from the server");

                            switch (currentState)
                            {
                                case NetworkClientState.TRYCONNECT:
                                    break;
                                case NetworkClientState.CONNECTED:
                                    // Marshall the state change packet into an object
                                    StateChange newSTC = new StateChange(newPacket.DataArray);

                                    // Add the state change object to the buffer for the UI
                                    lock (this.uiStateBuffer)
                                    {
                                        uiStateBuffer.Enqueue(newSTC);
                                    }

                                    break;
                                case NetworkClientState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case NetworkPacket.PacketType.MENUSTATECHANGE:
                            //Console.WriteLine("MSC received from the server");

                            switch (currentState)
                            {
                                case NetworkClientState.TRYCONNECT:
                                    break;
                                case NetworkClientState.CONNECTED:
                                    // Marshall the state change packet into an object
                                    MenuState newMSC = new MenuState(newPacket.DataArray);

                                    // Add the state change object to the buffer for the UI
                                    lock (this.menuStateBuffer)
                                    {
                                        menuStateBuffer.Enqueue(newMSC);
                                    }

                                    break;
                                case NetworkClientState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case NetworkPacket.PacketType.SYNC:
                            //Console.WriteLine("SYNC received from the server");
                            
                            switch (currentState)
                            {
                                case NetworkClientState.TRYCONNECT:
                                    break;
                                case NetworkClientState.CONNECTED:
                                    SyncServer();
                                    break;
                                case NetworkClientState.DISCONNECTED:
                                default:
                                    // This should not happen, die screaming!
                                    Environment.Exit(1);
                                    break;
                            }

                            break;
                        case NetworkPacket.PacketType.PING:
                            //Console.WriteLine("PING received from the server");
                            
                            switch (currentState)
                            {
                                case NetworkClientState.TRYCONNECT:
                                    break;
                                case NetworkClientState.CONNECTED:
                                    lock (this)
                                    {
                                        pingStopwatch.Stop();
                                        this.lastPing = pingStopwatch.ElapsedMilliseconds;
                                    }
                                    //Console.WriteLine("Ping"+lastPing);
                                    break;
                                case NetworkClientState.DISCONNECTED:
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

        /// <summary>
        /// Send a handshake packet to the server.
        /// </summary>
        private void SendHandshake()
        {
            networkWorker.SendPacket(new HandshakePacket(serverEndPoint));
            return; 
        }

        /// <summary>
        /// Begin sending ping heartbeats to the server.
        /// </summary>
        private void StartPinging()
        {
            pingPacketTimer = new Timer(PingTimerTicked, new AutoResetEvent(false), PING_INITIAL_DELAY, PING_PERIOD);
            return;
        }

        /// <summary>
        /// Handle ticks from the ping timer.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        private void PingTimerTicked(Object eventArgs)
        {
            lock (this)
            {
                if (pingStopwatch.IsRunning)
                {
                    return;
                }
                pingStopwatch.Reset();
                networkWorker.SendPacket(new PingPacket(serverEndPoint));
                pingStopwatch.Start();
            }
            return;
        }

        /// <summary>
        /// Send a synchronization message to the server.
        /// </summary>
        private void SyncServer()
        {
            networkWorker.SendPacket(new SyncPacket(serverEndPoint));
            return;
        }

        /// <summary>
        /// Send a list of commands to the server.
        /// </summary>
        /// <param name="commandList">A list of commands to send to the server.</param>
        public void sendCMD(List<Command> commandList)
        {
            foreach (Command command in commandList)
            {
                networkWorker.SendPacket(new CommandPacket(serverEndPoint, command));
            }
            return;
        }

        /// <summary>
        /// Send a list of menu state changes to the server.
        /// </summary>
        /// <param name="menuStateList">A list of menu states to send to the server.</param>
        public void SendMenuStateChange(List<MenuState> menuStateList)
        {
            foreach (MenuState menuState in menuStateList)
            {
                SendMenuStateChange(menuState);
            }
            return;
        }

        /// <summary>
        /// Send a menu state change to the server.
        /// </summary>
        /// <param name="menuState">The menu state to send to the server.</param>
        public void SendMenuStateChange(MenuState menuState)
        {
            networkWorker.SendPacket(new MenuStateChangePacket(serverEndPoint, menuState));
            return;
        }

        /// <summary>
        /// Receive an update about the game state.
        /// </summary>
        /// <returns></returns>
        public List<StateChange> ReceiveStateUpdate()
        {
            List<StateChange> newStates = new List<StateChange>();
            lock (uiStateBuffer)
            {
                while (uiStateBuffer.Count > 0) // Iterate over the buffer of states that have been acquired from the server
                {
                    newStates.Add(uiStateBuffer.Dequeue());
                }
            }
            return newStates;
        }

        /// <summary>
        /// Acquire any recent updates to the menu state from the character selection.
        /// </summary>
        /// <returns>A list of changes to the state of the menu.</returns>
        public List<MenuState> ReceiveMenuState()
        {
            List<MenuState> newStates = new List<MenuState>();
            lock (menuStateBuffer)
            { 
                while (menuStateBuffer.Count > 0) // Iterate over the buffer of states that have been acquired from the server
                {
                    newStates.Add(menuStateBuffer.Dequeue());
                }
            }
            return newStates;
        }

        /// <summary>
        /// Retrieve the last measured ping recorded by the client.
        /// </summary>
        /// <returns>A long value representing the last ping.</returns>
        public long GetLastPing()
        {
            lock (this)
            {
                return lastPing;
            }
        }

    }

}

