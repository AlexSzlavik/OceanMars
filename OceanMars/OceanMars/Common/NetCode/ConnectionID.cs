using System;
using System.Diagnostics;
using System.Net;

namespace OceanMars.Common.NetCode
{

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
            StateMachine.RegisterTransition(NetworkStateMachine.NetworkState.CONNECTIONTIMEOUT, NetworkStateMachine.TransitionEvent.SERVERSYNC, NetworkStateMachine.NetworkState.CONNECTIONCONNECTED, onSync);
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
}
