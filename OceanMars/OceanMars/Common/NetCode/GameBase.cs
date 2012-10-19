using System;
using System.Collections.Generic;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of the top-of-network-stack game.
    /// </summary>
    public abstract class GameBase : TransformChangeListener, IStatePhaseListener
    {
        public const int MAX_PLAYERS = 8; // Maximum number of players allowed in a lobby
        public bool sorryPeter = false;

        public List<GameData> GameStatesToCommit = new List<GameData>();
        public List<GameData> GameStatesToSend = new List<GameData>();

        /// <summary>
        /// The hierarchical tree that represents the state of the game.
        /// </summary>
        public State GameState
        {
            get;
            private set;
        }

        /// <summary>
        /// The underlying network server that is used by this game.
        /// </summary>
        protected NetworkBase Network
        {
            get;
            set;
        }

        /// <summary>
        /// The players that are known by the game.
        /// </summary>
        public Player[] players { get; protected set; }

        /// <summary>
        /// The player that is local to this machine.
        /// </summary>
        public Player LocalPlayer
        {
            get;
            set;
        }

        /// <summary>
        /// Register a player with the game and return their ID.
        /// </summary>
        /// <param name="player">The player to register.</param>
        /// <returns>An integer representing the new ID of the registered player.</returns>
        public abstract int RegisterPlayer(Player player);

        /// <summary>
        /// Unregister a player from the game.
        /// </summary>
        /// <param name="player">The player to unregister.</param>
        public virtual void UnregisterPlayer(Player player)
        {
            if (player != null) // Don't remove already-removed players
            {
                players[player.PlayerID] = null;
            }
            return;
        }

        /// <summary>
        /// The lobby associated with this particular GameBase.
        /// </summary>
        protected LobbyBase Lobby
        {
            get;
            set;
        }

        /// <summary>
        /// Instantiate the base components of a game.
        /// </summary>
        /// <param name="port">The port to open the GameNetworkServer on.</param>
        protected GameBase(NetworkBase network)
        {
            players = new Player[MAX_PLAYERS]; // Defaults to null elements (unlike C, you don't have to set the elements)
            GameState = new State();
            Network = network;

            GameState.addStatePhaseListener(this);
            GameState.addTransformChangeListener(this);

            return;
        }

        /// <summary>
        /// Fetch a player with the given ID.
        /// </summary>
        /// <param name="playerID">The ID of the player to fetch.</param>
        /// <returns>A player with the input ID. Returns null if no such player exists.</returns>
        public Player GetPlayer(int playerID)
        {
            return players[playerID];
        }

        public virtual void handleTransformChange(Entity e)
        {
            // Generate a transform change packet, put it on stack
            TransformData td = new TransformData(e.id, e.transform);
            GameData gd = new GameData(GameData.GameDataType.Movement, LocalPlayer.PlayerID, 0, td);
            GameStatesToSend.Add(gd);
        }

        public abstract void sendGameStates();

        public void commitGameStates()
        {
            //take a snapeshot of the GameStatesToCommit in case more are added while we're looping
            int gsLength = GameStatesToCommit.Count;

            for (int i = 0; i < gsLength; ++i)
            {
                GameData gs = GameStatesToCommit[i];
                if (gs != null)
                {
                    if (gs.Type == GameData.GameDataType.Movement)
                    {
                        int id = gs.TransformData.EntityID;
                        GameState.entities[id].transform = gs.TransformData.getMatrix();
                    }
                    else if (gs.Type == GameData.GameDataType.PlayerTransform)
                    {
                        int id = players[gs.TransformData.EntityID].EntityID;
                        GameState.entities[id].transform = gs.TransformData.getMatrix();
                    }
                }
            }

            //clear the game states we've just committed
            GameStatesToCommit.RemoveRange(0, gsLength);
        }

        /// <summary>
        /// Update the game state based on incoming game data.
        /// </summary>
        /// <param name="gameData">Received game data that should inform us about changing state, requests, etc.</param>
        protected virtual void UpdateGameState(GameData gameData)
        {
            GameStatesToCommit.Add(gameData);
        }

        public void handleStatePhaseChange(State.PHASE phase)
        {
            if (phase == State.PHASE.READY_FOR_CHANGES)
            {
                commitGameStates();
            }
            else if (phase == State.PHASE.FINISHED_FRAME)
            {
                sendGameStates();
            }
        }

        /// <summary>
        /// Start the game
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void startGame()
        {
            Network.RegisterGameDataUpdater(this.UpdateGameState);
            sorryPeter = true;
        }
    }
}
