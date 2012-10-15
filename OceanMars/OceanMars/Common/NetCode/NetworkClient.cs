using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.Collections.ObjectModel;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Class representing a UDP packet-based client.
    /// </summary>
    public class NetworkClient : NetworkBase
    {

        #region Member Variables

        private IPEndPoint serverEndPoint; // The address of the server

        // Data used to control ping flow (heartbeats) in the network client
        private Stopwatch pingStopwatch;
        private Timer pingPacketTimer;
        private long lastPing;

        private Semaphore serverReadySemaphore, clientConnectedSemaphore; // Semaphores used for flow control
        private Queue<GameData> gameDataBuffer; // Queue used to store received game data information

        private const int PING_INITIAL_DELAY = 1000; // Amount of time to wait before starting to ping heartbeats
        private const int PING_PERIOD = 500; // Amount of time to wait between ping heartbeats
        private const int CLIENT_TIMEOUT = 2000; // Amount of time until we are dead

        #endregion

        #region Internal Code

        /// <summary>
        /// Create a new raw client.
        /// </summary>
        public NetworkClient() : base(NetworkStateMachine.NetworkState.CLIENTSTART)
        {
            // Set up some member variables
            pingStopwatch = new Stopwatch();
            lastPing = -1;
            pingPacketTimer = null;

            // Set up locking primatives and state queues
            serverReadySemaphore = new Semaphore(0, 1);
            clientConnectedSemaphore = new Semaphore(0, 1);
            gameDataBuffer = new Queue<GameData>();

            // Create the actual client thread
            networkThread.Start();
            return;
        }

        /// <summary>
        /// Main execution loop for network clients.
        /// </summary>
        protected override void NetworkMain()
        {
            serverReadySemaphore.WaitOne();
            NetworkStateMachine.TransitionEvent transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTSTARTED;

            while (continueRunning) // Loop and push packets into the state machine
            {
                NetworkPacket receivePacket = networkWorker.ReceivePacket(); // Grab a packet from the server

                if (receivePacket == null)
                {
                    continue;
                }

                switch (receivePacket.Type)
                {
                    case NetworkPacket.PacketType.HANDSHAKE:
                        transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTCONNECTED;
                        break;
                    case NetworkPacket.PacketType.PING:
                        transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTPINGING;
                        break;
                    case NetworkPacket.PacketType.GAMEDATA:
                        transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTGAMEDATA;
                        break;
                    case NetworkPacket.PacketType.SYNC:
                        transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTSYNC;
                        break;
                }
                networkStateMachine.DoTransition(transitionEvent, receivePacket); // This is amazing
            }

            return;
        }

        /// <summary>
        /// Register state machine transitions.
        /// </summary>
        protected override void RegisterStateMachineTransitions()
        {
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTSTART, NetworkStateMachine.TransitionEvent.CLIENTSTARTED, NetworkStateMachine.NetworkState.CLIENTDISCONNECTED, delegate {});
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTDISCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTCONNECT, NetworkStateMachine.NetworkState.CLIENTTRYCONNECT, delegate { });
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTTRYCONNECT, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED, NetworkStateMachine.NetworkState.CLIENTCONNECTED, OnConnect);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTPINGING, NetworkStateMachine.NetworkState.CLIENTCONNECTED, OnPing);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTGAMEDATA, NetworkStateMachine.NetworkState.CLIENTCONNECTED, OnGameData);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTSYNC, NetworkStateMachine.NetworkState.CLIENTCONNECTED, OnSync);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CLIENTCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTTIMEOUT, NetworkStateMachine.NetworkState.CLIENTDISCONNECTED, OnDisconnect);
            networkStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.CLIENTSTARTED, null);
            return;
        }

        /// <summary>
        /// Shutdown this particular network client.
        /// </summary>
        protected override void Shutdown()
        {
            if (pingStopwatch.IsRunning)
            {
                pingStopwatch.Stop();
            }
            pingPacketTimer.Dispose();
            base.Shutdown();
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
                networkStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.CLIENTCONNECT, null); // Client may now try to connect
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
        /// Begin sending ping heartbeats to the server.
        /// </summary>
        private void StartPinging()
        {
            pingPacketTimer = new Timer(PingTimerTicked, new AutoResetEvent(false), PING_INITIAL_DELAY, PING_PERIOD);
            return;
        }

        /// <summary>
        /// Handler function for ping timer ticks.
        /// </summary>
        /// <param name="eventArgs">Event arguments.</param>
        private void PingTimerTicked(Object eventArgs)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(SendPingOnTick));
            return;
        }

        /// <summary>
        /// Worker thread function to deal with ping timer ticks and possible transition into the TIMEOUT state.
        /// </summary>
        /// <param name="eventArgs">Unused event arguments.</param>
        private void SendPingOnTick(Object eventArgs)
        {
            lock (pingStopwatch)
            {
                if (pingStopwatch.IsRunning)
                {
                    if (pingStopwatch.ElapsedMilliseconds > CLIENT_TIMEOUT)
                    {
                        pingStopwatch.Stop();
                        pingPacketTimer.Dispose();
                        networkStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.CLIENTTIMEOUT, null);
                    }
                    return;
                }
                pingStopwatch.Reset();
                networkWorker.SendPacket(new PingPacket(serverEndPoint));
                pingStopwatch.Start();
            }
            return;
        }

        /// <summary>
        /// Callback function upon connecting to a server.
        /// </summary>
        /// <param name="packet">The network packet retrieved during this transition.</param>
        private void OnConnect(NetworkPacket packet)
        {
            clientConnectedSemaphore.Release();
            StartPinging();
            return;
        }

        /// <summary>
        /// Callback function on disconnect events.
        /// </summary>
        /// <param name="packet">A packet received that has caused us to disconnect.</param>
        protected void OnDisconnect(NetworkPacket packet)
        {
            Debug.WriteLine(String.Format("Client has disconnected"));
            Shutdown();
            return;
        }

        /// <summary>
        /// Callback function on receipt of game data.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        protected override void OnGameData(NetworkPacket packet)
        {
            gameDataUpdater(new GameData(packet.DataArray));
            return;
        }

        /// <summary>
        /// Callback function on receipt of a ping.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        protected override void OnPing(NetworkPacket packet)
        {
            lock (pingStopwatch)
            {
                pingStopwatch.Stop();
                lastPing = pingStopwatch.ElapsedMilliseconds;
            }
            return;
        }

        /// <summary>
        /// Callback function on receipt of a SYNC packet.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        protected override void OnSync(NetworkPacket packet)
        {
            SendSync();
            return;
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
        /// Send a synchronization message to the server.
        /// </summary>
        private void SendSync()
        {
            networkWorker.SendPacket(new SyncPacket(serverEndPoint));
            return;
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Send a list of game data information to the server.
        /// </summary>
        /// <param name="gameDataList">A list of game data to send to the server.</param>
        public void SendGameData(List<GameData> gameDataList)
        {
            for (int i = 0; i < gameDataList.Count; i += 1)
            {
                SendGameData(gameDataList[i]);
            }
            return;
        }

        /// <summary>
        /// Send a unit of game data to the server.
        /// </summary>
        /// <param name="gameData">The game data to send to the server.</param>
        public void SendGameData(GameData gameData)
        {
            networkWorker.SendPacket(new GameDataPacket(serverEndPoint, gameData));
            return;
        }

        /// <summary>
        /// Receive an update about the game state.
        /// </summary>
        /// <returns>A list of GameData that represents update sates.</returns>
        public List<GameData> ReceiveGameData()
        {
            List<GameData> gameDataList = new List<GameData>();
            lock (gameDataBuffer)
            {
                while (gameDataBuffer.Count > 0) // Iterate over the buffer of states that have been acquired from the server
                {
                    gameDataList.Add(gameDataBuffer.Dequeue());
                }
            }
            return gameDataList;
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

        #endregion

    }

}

