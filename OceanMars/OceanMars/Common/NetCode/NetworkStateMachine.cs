using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// A generic state machine used to receive packets and act accordingly.
    /// </summary>
    public class NetworkStateMachine
    {

        /// <summary>
        /// The states that may be taken by the net state machine.
        /// </summary>
        public enum NetworkState
        {
            /// <summary>
            /// The client has just been initialized.
            /// </summary>
            CLIENTSTART,

            /// <summary>
            /// The client is not currently connected to the server.
            /// </summary>
            CLIENTDISCONNECTED,

            /// <summary>
            /// The client is attempting to connect to the server.
            /// </summary>
            CLIENTTRYCONNECT,

            /// <summary>
            /// The client is connected to the server.
            /// </summary>
            CLIENTCONNECTED,

            /// <summary>
            /// There was an error trying to connect to the server.
            /// </summary>
            CLIENTCONNECTEDERROR,

            /// <summary>
            /// An unhandled type of error occurred...The client is in really big trouble!
            /// </summary>
            CLIENTPANIC,


            SERVERSTART,


            SERVERACCEPTCONNECTIONS,

            SERVERSYNC,
            CONNECTIONCONNECTED,
            CONNECTIONDISCONNECTED,
            CONNECTIONTIMEOUT,
            CONNECTIONCONNECTED_SYNC

        }

        /// <summary>
        /// Types of events that can occur during transitions.
        /// </summary>
        public enum TransitionEvent
        {
            /// <summary>
            /// The client has been started.
            /// </summary>
            CLIENTSTARTED,

            /// <summary>
            /// The client is connecting.
            /// </summary>
            CLIENTCONNECT,

            /// <summary>
            /// The client is connected.
            /// </summary>
            CLIENTCONNECTED,

            /// <summary>
            /// The client is dropping.
            /// </summary>
            CLIENTDROPPING,

            /// <summary>
            /// Client received a packet containing game data.
            /// </summary>
            CLIENTGAMEDATA,

            /// <summary>
            /// Client received a Sync request from the server.
            /// </summary>
            CLIENTSYNC,

            SERVERSTARTED,
            SERVERHANDSHAKE,
            SERVERSYNC,
            SERVERPING,
            SERVERGAMEDATA,
            SERVERNEWCONNECTION,
            SERVERDISCONNECTION,
            CONNECTIONDISCONNECT,
            CONNECTIONTIMEOUT,
            CLIENTCONNECTED_SYNCING

        }

        /// <summary>
        /// Delegate type used to execute actions when transitions occur.
        /// </summary>
        public delegate void TransitionAction(NetworkPacket packet);

        /// <summary>
        /// The current state of the NetStateMachine.
        /// </summary>
        public NetworkState CurrentState
        {
            get;
            protected set;
        }

        /// A transition table used to move to new states in the state machine.
        protected Dictionary<Tuple<NetworkState, TransitionEvent>, Tuple<NetworkState, TransitionAction>> TransitionTable;

        /// <summary>
        /// Create a new NetStateMachine.
        /// </summary>
        /// <param name="startingState">The state to begin the NetStateMachine in.</param>
        public NetworkStateMachine(NetworkState startingState)
        {
            CurrentState = startingState;
            TransitionTable = new Dictionary<Tuple<NetworkState,TransitionEvent>,Tuple<NetworkState,TransitionAction>>();
            return;
        }

        /// <summary>
        /// Register a new transition inside the state machine.
        /// </summary>
        /// <param name="previousState">The state being transitioned out of.</param>
        /// <param name="eventPacket">The type of packet received over the network.</param>
        /// <param name="nextState">The state to transition into.</param>
        /// <param name="action">An action to perform before transitioning.</param>
        public void RegisterTransition(NetworkState previousState, TransitionEvent transEvent, NetworkState nextState, TransitionAction action)
        {
            TransitionTable.Add(new Tuple<NetworkState, TransitionEvent>(previousState, transEvent), new Tuple<NetworkState, TransitionAction>(nextState, action));
            return;
        }

        /// <summary>
        /// Perform a transion between states.
        /// </summary>
        /// <param name="transEvent">An event that is occuring when a packet arrives.</param>
        /// <param name="packet">The packet received over the network.</param>
        public void DoTransition(TransitionEvent transEvent, NetworkPacket packet)
        {
            Tuple<NetworkState, TransitionAction> transition = null;
            try
            {
                transition = TransitionTable[new Tuple<NetworkState,TransitionEvent>(CurrentState,transEvent)];
                transition.Item2(packet);
                CurrentState = transition.Item1;
            }
            catch (Exception error)
            {
                Debug.WriteLine("You managed to break the NetStateMachine. Congratulations, asshole: {0}", new Object[] {error.Message});
                Debug.WriteLine("Violating Transition: {0} - {1} - {2}",new Object[] {CurrentState,transEvent,packet.Type});
                throw error;
            }
            return;
        }

    }

}
