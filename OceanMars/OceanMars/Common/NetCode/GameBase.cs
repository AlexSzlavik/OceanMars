﻿using System;
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
        /// Game states that have been received an not yet committed to the overall game state.
        /// </summary>
        protected List<GameData> gameStatesToCommit;

        /// <summary>
        /// Game states that must be sent out.
        /// </summary>
        protected Dictionary<int, GameData> gameStatesToSend;

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
        protected Player[] players;

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
            gameStatesToSend = new Dictionary<int, GameData>();
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
            GameData gameData = new GameData(GameData.GameDataType.Movement, transformData: transformData);
            gameStatesToSend.Add(entity.id, gameData);
            return;
        }

        /// <summary>
        /// Send out game state updates.
        /// </summary>
        public abstract void SendGameStates();

        /// <summary>
        /// Commit game states over top of the current state.
        /// </summary>
        public abstract void CommitGameStates();

        /// <summary>
        /// Update the game state based on incoming game data.
        /// </summary>
        /// <param name="gameData">Received game data that should inform us about changing state, requests, etc.</param>
        protected virtual void UpdateGameState(GameData gameData)
        {
            gameStatesToCommit.Add(gameData);
            return;
        }

        /// <summary>
        /// Handle changes to the phase of the world state.
        /// </summary>
        /// <param name="phase">The phase that we are transitioning into.</param>
        public void HandleStatePhaseChange(State.PHASE phase)
        {
            if (phase == State.PHASE.READY_FOR_CHANGES)
            {
                CommitGameStates();
            }
            else if (phase == State.PHASE.FINISHED_FRAME)
            {
                SendGameStates();
            }
            return;
        }

    }
}
