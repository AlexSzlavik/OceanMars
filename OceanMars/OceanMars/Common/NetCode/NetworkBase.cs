using System;
using System.Threading;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Delegate type used for subscription to game data.
    /// </summary>
    /// <param name="gameData">Game data used to update the state of the game.</param>
    public delegate void GameDataUpdater(GameData gameData);

    /// <summary>
    /// Abstract class representing the secondary (connection-level) tier of the network stack.
    /// </summary>
    public abstract class NetworkBase
    {

        #region Member variables

        protected bool continueRunning; // Whether or not to continue the client
        protected NetworkWorker networkWorker; // The worker thread used to send and receive data
        protected NetworkStateMachine networkStateMachine; // A network state machine run on the client
        protected Thread networkThread; // The thread used to control the network object

        #endregion

        /// <summary>
        /// The actual delegate used to update game data when appropriate packets are received.
        /// </summary>
        protected GameDataUpdater gameDataUpdater;

        /// <summary>
        /// Create a new NetworkBase.
        /// </summary>
        /// <param name="startingState">The state to start the network state machine in.</param>
        protected NetworkBase(NetworkStateMachine.NetworkState startingState)
        {
            networkStateMachine = new NetworkStateMachine(startingState);
            RegisterStateMachineTransitions();
            continueRunning = true;
            gameDataUpdater = null; // Set this to null initially
            networkThread = new Thread(NetworkMain);
            networkThread.IsBackground = true;
            return;
        }

        /// <summary>
        /// Register a delegate to handle updates to the game.
        /// </summary>
        /// <param name="gameDataUpdater">A delegate function to call when game data is received over the network.</param>
        public void RegisterGameDataUpdater(GameDataUpdater gameDataUpdater)
        {
            this.gameDataUpdater = gameDataUpdater;
            return;
        }

        /// <summary>
        /// The main execution loop for a Network object.
        /// </summary>
        protected abstract void NetworkMain();

        /// <summary>
        /// Register possible transitions inside the network state machine.
        /// </summary>
        protected abstract void RegisterStateMachineTransitions();

        /// <summary>
        /// Shutdown this particular network object.
        /// </summary>
        protected virtual void Shutdown()
        {
            continueRunning = false;
            if (networkWorker != null)
            {
                networkWorker.Disconnect();
            }
            return;
        }

        /// <summary>
        /// Callback function on receipt of game data.
        /// </summary>
        /// <param name="packet"></param>
        protected abstract void OnGameData(NetworkPacket packet);

        /// <summary>
        /// Callback function on receipt of a ping.
        /// </summary>
        /// <param name="packet">The packet received.</param>
        protected abstract void OnPing(NetworkPacket packet);

        /// <summary>
        /// Callback function on receipt of a SYNC.
        /// </summary>
        /// <param name="packet"></param>
        protected abstract void OnSync(NetworkPacket packet);

    }

}
