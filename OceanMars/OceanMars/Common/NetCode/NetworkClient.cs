﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Threading;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Class representing a UDP packet-based client.
    /// </summary>
    public class NetworkClient : NetworkBase
    {

        #region Member Variables

        // Data used to control addressing and threading in the network client
        private Thread clientThread; // Thread used to host a network client
        private IPEndPoint serverEndPoint; // The address of the server

        // Data used to control the state of the network client


        // Data used to control ping flow (heartbeats) in the network client
        private Stopwatch pingStopwatch;
        private Timer pingPacketTimer;
        private long lastPing;

        // Semaphore to wait on for the server info to be known
        private Semaphore serverReadySemaphore, clientConnectedSemaphore;

        // Queues used to buffer game state changes that have been received
        private Queue<GameData> gameDataBuffer;

        private const int PING_INITIAL_DELAY = 1000; // Amount of time to wait before starting to ping heartbeats
        private const int PING_PERIOD = 500; // Amount of time to wait between ping heartbeats
        private const int CLIENT_TIMEOUT = 2000; // Amount of time until we are dead

        #endregion

        #region ClientInternalCode

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
            clientThread = new Thread(RunNetworkClientThread);
            clientThread.IsBackground = true;
            clientThread.Start();
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
        /// Callback on Disconnect events
        /// </summary>
        /// <param name="packet">A packet received that has caused us to disconnect.</param>
        private void OnDisconnect(NetworkPacket packet)
        {
            Debug.WriteLine(String.Format("Client has disconnected"));
            continueRunning = false;
            if (networkWorker != null)
            {
                networkWorker.Close();
                networkWorker.Exit();
            }
            return;
        }

        /// <summary>
        /// Transition action that occurs on pinging.
        /// </summary>
        /// <param name="packet">The network packet retrieved during this transition.</param>
        public void OnPing(NetworkPacket packet)
        {
            lock (pingStopwatch)
            {
                pingStopwatch.Stop();
                lastPing = pingStopwatch.ElapsedMilliseconds;
            }
            return;
        }

        /// <summary>
        /// Transition action that should occur when the state of the game changes.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        public void OnGameData(NetworkPacket packet)
        {
            // Add the state change object to the buffer for the UI
            lock (gameDataBuffer)
            {
                gameDataBuffer.Enqueue(new GameData(packet.DataArray));
            }
            return;
        }

        /// <summary>
        /// Transition action that should occur when syncing.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        public void OnSync(NetworkPacket packet)
        {
            SendSync();
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
        /// Main execution loop for network clients.
        /// </summary>
        private void RunNetworkClientThread()
        {
            networkStateMachine.DoTransition(NetworkStateMachine.TransitionEvent.CLIENTSTARTED, null);
            serverReadySemaphore.WaitOne();
            NetworkStateMachine.TransitionEvent transitionEvent = NetworkStateMachine.TransitionEvent.CLIENTSTARTED;

            while (continueRunning) // Loop and push packets into the state machine
            {
                NetworkPacket receivePacket = networkWorker.ReceivePacket(); // Grab a packet from the server
                if (receivePacket == null)
                    continue;
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
            ThreadPool.QueueUserWorkItem(new WaitCallback(DoPingTimerWork));
            return;
        }

        /// <summary>
        /// The worker thread for the client side ping timer
        /// This actually resets and starts the timer again if need be
        /// This should also transition to TIMEOUT if a certain threshold is reached
        /// </summary>
        /// <param name="eventArgs">Unused event arguments.</param>
        private void DoPingTimerWork(Object eventArgs)
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

        #endregion

        #region Public Client Code

        /// <summary>
        /// Send a synchronization message to the server.
        /// </summary>
        private void SendSync()
        {
            networkWorker.SendPacket(new SyncPacket(serverEndPoint));
            return;
        }

        /// <summary>
        /// Send a list of game data changes to the server.
        /// </summary>
        /// <param name="gameDataList">A list of game data to send to the server.</param>
        public void SendGameData(List<GameData> gameDataList)
        {
            foreach (GameData gameData in gameDataList)
            {
                SendGameData(gameData);
            }
            return;
        }

        /// <summary>
        /// Send a game data change to the server.
        /// </summary>
        /// <param name="gameData">The menu state to send to the server.</param>
        public void SendGameData(GameData gameData)
        {
            networkWorker.SendPacket(new GameDataPacket(serverEndPoint, gameData));
            return;
        }

        /// <summary>
        /// Receive an update about the game state.
        /// </summary>
        /// <returns></returns>
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

