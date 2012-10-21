using System;
using System.Collections.Generic;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of the top-of-network-stack game.
    /// </summary>
    public abstract class GameBase : TransformChangeListener, IStatePhaseListener
    {
        /// <summary>
        /// Maximum number of players in a game.
        /// </summary>
        public const int MAX_PLAYERS = 8;

        /// <summary>
        /// Game states that have been received and not yet committed to the overall game state.
        /// </summary>
        protected List<GameData> gameStatesToCommit;

        /// <summary>
        /// Game states that must be sent out to other games (either the client or the main server).
        /// </summary>
        protected List<GameData> gameStatesToSend;

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
            gameStatesToCommit = new List<GameData>();
            gameStatesToSend = new List<GameData>();
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

        /// <summary>
        /// Update an entity based on a transform.
        /// </summary>
        /// <param name="entity">The entity to update.</param>
        public virtual void HandleTransformChange(Entity entity)
        {
            // Generate a transform change packet, put it on stack
            TransformData transformData = new TransformData(entity.id, entity.transform);
            
            // TODO: This likely doesn't work. This neeeds to be fixed (the player ID and event detail might need changing, or we may simply need a new constructor).
            GameData gameData = new GameData(GameData.GameDataType.Movement, LocalPlayer.PlayerID, 0, transformData);

            gameStatesToSend.Add(gameData);
            return;
        }

        /// <summary>
        /// Commit incoming game states into the world.
        /// </summary>
        public void CommitGameStates()
        {
            lock (gameStatesToCommit)
            {
                for (int i = 0; i < gameStatesToCommit.Count; ++i)
                {
                    GameData gameState = gameStatesToCommit[i];
                    if (gameState != null)
                    {
                        switch (gameState.Type)
                        {
                            case GameData.GameDataType.Movement:
                                GameState.entities[gameState.TransformData.EntityID].transform = gameState.TransformData.GetMatrix();
                                break;
                            case GameData.GameDataType.PlayerTransform:
                                GameState.entities[players[gameState.TransformData.EntityID].EntityID].transform = gameState.TransformData.GetMatrix();
                                break;
                        }
                    }
                }
                gameStatesToCommit.Clear();
            }
            return;
        }

        /// <summary>
        /// Send out game state updates.
        /// </summary>
        public abstract void SendGameStates();

        /// <summary>
        /// Add incoming data to the game states.
        /// </summary>
        /// <param name="gameData">Received game data that should inform us about changing state, requests, etc.</param>
        protected virtual void AddGameState(GameData gameData)
        {
            lock (gameStatesToCommit)
            {
                gameStatesToCommit.Add(gameData);
            }
            return;
        }

        /// <summary>
        /// Handle changes to the phase of the world state.
        /// </summary>
        /// <param name="phase">The phase that we are transitioning into.</param>
        public void HandleStatePhaseChange(State.PHASE phase)
        {
            switch (phase)
            {
                case State.PHASE.FINISHED_FRAME:
                    SendGameStates();
                    break;
                case State.PHASE.READY_FOR_CHANGES:
                    CommitGameStates();
                    break;
                default:
                    throw new NotImplementedException("Unhandled state passed to GameBase");
            }
            return;
        }

        /// <summary>
        /// Start the game.
        /// </summary>
        public void StartGame()
        {
            Network.RegisterGameDataUpdater(AddGameState);
            return;
        }
    }
}
