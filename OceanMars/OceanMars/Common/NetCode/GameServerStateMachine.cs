using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// A generic state machine used to receive packets and act accordingly.
    /// </summary>
    public class GameServerStateMachine
    {

        /// <summary>
        /// The states that may be taken by the net state machine.
        /// </summary>
        public enum GameServerState
        {
        }

        /// <summary>
        /// Types of events that can occur during transitions.
        /// </summary>
        public enum GameServerTransition
        {

        }

        /// <summary>
        /// Delegate type used to execute actions when transitions occur.
        /// </summary>
        public delegate void TransitionAction(GameData gameData);

        /// <summary>
        /// The current state of the NetStateMachine.
        /// </summary>
        public GameServerState CurrentState
        {
            get;
            protected set;
        }

        /// A transition table used to move to new states in the state machine.
        protected Dictionary<Tuple<GameServerState, GameServerTransition>, Tuple<GameServerState, TransitionAction>> TransitionTable;

        /// <summary>
        /// Create a new NetStateMachine.
        /// </summary>
        /// <param name="startingState">The state to begin the NetStateMachine in.</param>
        public GameServerStateMachine(GameServerState startingState)
        {
            CurrentState = startingState;
            TransitionTable = new Dictionary<Tuple<GameServerState,GameServerTransition>,Tuple<GameServerState,TransitionAction>>();
            return;
        }

        /// <summary>
        /// Register a new transition inside the state machine.
        /// </summary>
        /// <param name="previousState">The state being transitioned out of.</param>
        /// <param name="eventPacket">The type of packet received over the network.</param>
        /// <param name="nextState">The state to transition into.</param>
        /// <param name="action">An action to perform before transitioning.</param>
        public void RegisterTransition(GameServerState previousState, GameServerTransition transEvent, GameServerState nextState, TransitionAction action)
        {
            TransitionTable.Add(new Tuple<GameServerState, GameServerTransition>(previousState, transEvent), new Tuple<GameServerState, TransitionAction>(nextState, action));
            return;
        }

        /// <summary>
        /// Perform a transion between states.
        /// </summary>
        /// <param name="transEvent">An event that is occuring when a packet arrives.</param>
        /// <param name="packet">The packet received over the network.</param>
        public void DoTransition(GameServerTransition transEvent, GameData packet)
        {
            Tuple<GameServerState, TransitionAction> transition = null;
            try
            {
                transition = TransitionTable[new Tuple<GameServerState,GameServerTransition>(CurrentState,transEvent)];
                transition.Item2(packet);
                CurrentState = transition.Item1;
            }
            catch (Exception error)
            {
                Debug.WriteLine("You managed to break the GameServerStateMachine. Congratulations, asshole: {0}", new Object[] {error.Message});
                Debug.WriteLine("Violating Transition: {0} - {1} - {2}",new Object[] {CurrentState,transEvent,packet});
                throw error;
            }
            return;
        }

    }

}
