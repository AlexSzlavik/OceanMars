using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of the game program that rests on top of the network stack.
    /// </summary>
    public class GameClient
    {

        #region Static Members

        /// <summary>
        /// A mapping between players and connections.
        /// </summary>
        private Dictionary<Player, ConnectionID> PlayerToConnectionIDMap = new Dictionary<Player, ConnectionID>();

        /// <summary>
        /// A reverse mapping of connections to players
        /// </summary>
        private Dictionary<ConnectionID, Player> ConnectionIDToPlayerMap = new Dictionary<ConnectionID, Player>();

        #endregion

        #region Static Methods

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

        #endregion

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
        public NetworkServer GameNetworkServer
        {
            get;
            private set;
        }

        /// <summary>
        /// The Lobby code. 
        /// Mostly the interface will interface with this. 
        /// Eventually it will hand control to the GameServers main logic
        /// </summary>
        public Lobby GameLobby
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether or not this GameServer represents a host.
        /// </summary>
        public bool IsHosting
        {
            get;
            private set;
        }

        /// <summary>
        /// Create a new GameServer.
        /// </summary>
        /// <param name="port">The port to create the GameServer on.</param>
        public GameClient(int port, bool isHosting)
        {
            GameState = new State();
            GameNetworkServer = new NetworkServer(port);
            PlayerToConnectionIDMap = new Dictionary<Player, ConnectionID>();
            ConnectionIDToPlayerMap = new Dictionary<ConnectionID, Player>();

            //We first give control to the Lobby
            GameLobby = new Lobby(this);
            GameNetworkServer.RegisterGameDataUpdater(GameLobby.UpdateGameState);
            IsHosting = isHosting;

            //Once we are done with the lobby we have it 
            //hand off it's connections and player setup
            //The lobby will then close down and the GameServer processses the rest of the 
            //information

            return;
        }

        /// <summary>
        /// Update the game state based on incoming game data.
        /// </summary>
        /// <param name="gameData">Received game data that should inform us about changing state, requests, etc.</param>
        public void UpdateGameState(GameData gameData)
        {
            return;
        }

        /// <summary>
        /// Register a new player with the game client.
        /// </summary>
        /// <param name="player">The player to register with the game client.</param>
        public void RegisterPlayer(Player player)
        {
            PlayerToConnectionIDMap.Add(player, player.ConnectionID);
            ConnectionIDToPlayerMap.Add(player.ConnectionID, player);
            return;
        }

        /// <summary>
        /// Unregister a player from the game client.
        /// </summary>
        /// <param name="player">The player to remove from the game client.</param>
        public void UnregisterPlayer(Player player)
        {
            if (player != null)
            {
                PlayerToConnectionIDMap.Remove(player);
                if (player.ConnectionID != null)
                {
                    ConnectionIDToPlayerMap.Remove(player.ConnectionID);
                }
            }
            return;
        }

    }
}

