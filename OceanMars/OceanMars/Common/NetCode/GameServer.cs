using System;
using System.Collections.Generic;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of a game server that rests on top of the network stack.
    /// </summary>
    public class GameServer : GameBase
    {

        /// <summary>
        /// A mapping between players and connections.
        /// </summary>
        private Dictionary<Player, ConnectionID> PlayerToConnectionIDMap = new Dictionary<Player, ConnectionID>();

        /// <summary>
        /// A reverse mapping of connections to players
        /// </summary>
        private Dictionary<ConnectionID, Player> ConnectionIDToPlayerMap = new Dictionary<ConnectionID, Player>();

        /// <summary>
        /// Retrieve a connection ID from a player.
        /// </summary>
        /// <param name="player">The player to fetch a connection ID for.</param>
        /// <returns>A connection ID mapped to the provided player. If the player does not exist, returns null.</returns>
        public ConnectionID PlayerToConnectionID(Player player)
        {
            if (PlayerToConnectionIDMap.ContainsKey(player))
            {
                return PlayerToConnectionIDMap[player];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve a player from a connection ID.
        /// </summary>
        /// <param name="connectionID">The connection ID to fetch a player ID for.</param>
        /// <returns>A player mapped to the provided connection ID. If the connection was not mapped to a player, returns null.</returns>
        public Player ConnectionIDToPlayer(ConnectionID connectionID)
        {
            if (ConnectionIDToPlayerMap.ContainsKey(connectionID))
            {
                return ConnectionIDToPlayerMap[connectionID];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// The server lobby associated with this game server.
        /// </summary>
        public new LobbyServer Lobby
        {
            get
            {
                return (LobbyServer)base.Lobby;
            }
            private set
            {
                base.Lobby = value;
            }
        }

        /// <summary>
        /// Reference to the underlying network associated with this game server.
        /// </summary>
        public new NetworkServer Network
        {
            get
            {
                return (NetworkServer)base.Network;
            }
            private set
            {
                base.Network = value;
            }
        }

        /// <summary>
        /// The stack of IDs that are available to joining players.
        /// </summary>
        private Stack<int> availableIDs;

        /// <summary>
        /// Create a new GameServer.
        /// </summary>
        /// <param name="port">The port to create the GameServer on.</param>
        public GameServer(int port) : base(new NetworkServer(port))
        {
            availableIDs = new Stack<int>();
            for (int i = MAX_PLAYERS - 1; i > 0; i--) // Add id's to the players
            {
                availableIDs.Push(i);
            }

            PlayerToConnectionIDMap = new Dictionary<Player, ConnectionID>();
            ConnectionIDToPlayerMap = new Dictionary<ConnectionID, Player>();

            Lobby = new LobbyServer(this); // Give initial control to the lobby
            Network.RegisterGameDataUpdater(Lobby.UpdateLobbyState);

            // Once we're done with the lobby, the connections and players will be handed off to the game and the GameDataUpdater re-registered to GameServer.UpdateGameState

            return;
        }

        /// <summary>
        /// Register a new player with the game server.
        /// </summary>
        /// <param name="player">The player to register with the game server.</param>
        /// <returns>Returns the ID of the player to register.</returns>
        public override int RegisterPlayer(Player player)
        {
            PlayerToConnectionIDMap.Add(player, player.ConnectionID);
            ConnectionIDToPlayerMap.Add(player.ConnectionID, player);
            return availableIDs.Pop();
        }

        /// <summary>
        /// Unregister a player from the game server.
        /// </summary>
        /// <param name="player">The player to remove from the game server.</param>
        public override void UnregisterPlayer(Player player)
        {
            if (player != null)
            {
                availableIDs.Push(player.PlayerID);
                PlayerToConnectionIDMap.Remove(player);
                if (player.ConnectionID != null)
                {
                    ConnectionIDToPlayerMap.Remove(player.ConnectionID);
                }
            }
            return;
        }

        /// <summary>
        /// Update the state of the game based on received game data.
        /// </summary>
        /// <param name="gameData">The game data to use to update the game.</param>
        protected override void UpdateGameState(GameData gameData)
        {
            throw new NotImplementedException();
        }

    }

}
