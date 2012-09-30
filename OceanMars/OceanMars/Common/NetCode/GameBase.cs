using System;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of the top-of-network-stack game.
    /// </summary>
    public abstract class GameBase
    {
        public const int MAX_PLAYERS = 8; // Maximum number of players allowed in a lobby

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
        /// Update the game state based on incoming game data.
        /// </summary>
        /// <param name="gameData">Received game data that should inform us about changing state, requests, etc.</param>
        protected abstract void UpdateGameState(GameData gameData);

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

    }
}
