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
    public class NetStateMachine
    {

        /// <summary>
        /// The states that may be taken by the net state machine.
        /// </summary>
        public enum NetState
        {
            /// <summary>
            /// The client has just been initialized.
            /// </summary>
            CLEINTSTART,

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
            CLIENTPANIC

        }

        /// <summary>
        /// Delegate type used to execute actions when transitions occur.
        /// </summary>
        public delegate void TransitionAction(Packet packet);

        /// <summary>
        /// The current state of the NetStateMachine.
        /// </summary>
        public NetState CurrentState
        {
            get;
            private set;
        }

        /// <summary>
        /// A transition table used to move to new states in the state machine.
        /// </summary>
        private Dictionary<Tuple<NetState, Packet.PacketType>, Tuple<NetState, TransitionAction>> TransitionTable
        {
            get;
            set;
        }

        /// <summary>
        /// Create a new NetStateMachine.
        /// </summary>
        /// <param name="startingState">The state to begin the NetStateMachine in.</param>
        public NetStateMachine(NetState startingState)
        {
            CurrentState = startingState;
            return;
        }

        /// <summary>
        /// Register a new transition inside the state machine.
        /// </summary>
        /// <param name="previousState">The state being transitioned out of.</param>
        /// <param name="eventPacket">The type of packet received over the network.</param>
        /// <param name="nextState">The state to transition into.</param>
        /// <param name="action">An action to perform before transitioning.</param>
        public void RegisterTransition(NetState previousState, Packet.PacketType eventPacket, NetState nextState, TransitionAction action)
        {
            TransitionTable.Add(new Tuple<NetState, Packet.PacketType>(previousState, eventPacket), new Tuple<NetState, TransitionAction>(nextState, action));
            return;
        }

        /// <summary>
        /// Perform a transion between states.
        /// </summary>
        /// <param name="packet">The packet received over the network.</param>
        public void DoTransition(Packet packet)
        {
            try
            {
                Tuple<NetState, TransitionAction> transition = TransitionTable[new Tuple<NetState, Packet.PacketType>(CurrentState, packet.Type)];
                transition.Item2(packet);
                CurrentState = transition.Item1;
            }
            catch (Exception error)
            {
                Debug.WriteLine("You managed to break the NetStateMachine. Congratulations, asshole: {0}", error.Message);
            }
            return;
        }

    }

}
