using System;
using System.Diagnostics;
using System.Net;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// A class that represents a particular UDP connection.
    /// </summary>
    public class ConnectionID
    {
        
        /// <summary>
        /// The current ID to assign to new connections.
        /// </summary>
        private static int currentID = 0;

        /// <summary>
        /// The integer ID of this particular connection.
        /// </summary>
        public int ID
        {
            get;
            private set;
        }

        /// <summary>
        /// The IP end point associated with this connection.
        /// </summary>
        public IPEndPoint IPEndPoint
        {
            get;
            private set;
        }

        /// <summary>
        /// Number of sync packets missed.
        /// </summary>
        public int MissedSyncs
        {
            get;
            private set;
        }

        /// <summary>
        /// The underlying state machine for this connection.
        /// </summary>
        private NetworkStateMachine networkStateMachine;

        /// <summary>
        /// Create a new ConnectionID for a user.
        /// </summary>
        /// <param name="ipEndPoint"></param>
        public ConnectionID(IPEndPoint ipEndPoint)
        {
            networkStateMachine = new NetworkStateMachine(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED);
            InitStateMachine();
            MissedSyncs = 0;
            ID = currentID++;
            IPEndPoint = ipEndPoint;
            return;
        }

        
        /// <summary>
        /// Initialize the state machine for this connection.
        /// </summary>
        private void InitStateMachine()
        {
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CONNECTIONDISCONNECT, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CONNECTIONTIMEOUT, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONTIMEOUT, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, OnSync);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONTIMEOUT, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, delegate { });
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, OnSync);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, NetworkStateMachine.TransitionEvent.CLIENTCONNECTED_SYNCING, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, OnMissingSync);
            networkStateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONCONNECTED_SYNC, NetworkStateMachine.TransitionEvent.CONNECTIONTIMEOUT, NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED, delegate { });
            return;
        }
        
        /// <summary>
        /// Change the state of this connection ID through the internal network state machine.
        /// </summary>
        /// <param name="transitionEvent">The transition event to run to apply to the internal network state machine.</param>
        public void ChangeState(NetworkStateMachine.TransitionEvent transitionEvent)
        {
            lock (this)
            {
                networkStateMachine.DoTransition(transitionEvent, null);
            }
            return;
        }

        /// <summary>
        /// Get the current state of the internal network state machine.
        /// </summary>
        public NetworkStateMachine.NetworkState CurrentState
        {
            get
            {
                return networkStateMachine.CurrentState;
            }
        }

        /// <summary>
        /// Update this connection ID on receipt of a sync packet.
        /// </summary>
        /// <param name="packet">The packet that was received.</param>
        private void OnSync(NetworkPacket packet)
        {
            lock (this)
            {
                this.MissedSyncs = 0;
            }
            return;
        }

        /// <summary>
        /// Keep track of the number of SYNCS that have been missed to eventually transition into a TIMEOUT state.
        /// </summary>
        /// <param name="packet">The packet that was received.</param>
        private void OnMissingSync(NetworkPacket packet)
        {
            lock (this)
            {
                MissedSyncs += 1;
                Debug.WriteLine(String.Format("Missed {0} SYNCS", MissedSyncs));
            }
            return;
        }

        /// <summary>
        /// Whether or not this connection ID is currently connected.
        /// </summary>
        /// <returns>A boolean representing whether or not the internal network state machine is not disconnected.</returns>
        public bool IsConnected()
        {
            lock (this)
            {
                return networkStateMachine.CurrentState != NetworkStateMachine.NetworkState.CONNECTIONDISCONNECTED;
            }
        }
    }
}
