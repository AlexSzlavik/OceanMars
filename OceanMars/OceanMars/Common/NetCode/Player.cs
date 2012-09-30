using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Common.NetCode
{
    public class Player
    {
        #region Static Members

        /// <summary>
        /// A mapping between player IDs and actual connections.
        /// </summary>
        private static Dictionary<int, ConnectionID> PlayerToConnectionMap = new Dictionary<int, ConnectionID>();

        /// <summary>
        /// A reverse mapping of connections to players
        /// </summary>
        private static Dictionary<ConnectionID, int> ConnectionToPlayerMap = new Dictionary<ConnectionID, int>();

        #endregion

        #region Static Methods

        /// <summary>
        /// Retrieve a connection ID from a player ID.
        /// </summary>
        /// <param name="playerId">The player ID to fetch a connection ID for.</param>
        /// <returns>A connection ID mapped to the provided player ID. If the player does not exist, returns null.</returns>
        public static ConnectionID PlayerToConnection(int playerId)
        {
            if (Player.PlayerToConnectionMap.ContainsKey(playerId))
            {
                return Player.PlayerToConnectionMap[playerId];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve a player ID from a connection ID.
        /// </summary>
        /// <param name="connectionID">The connection ID to fetch a player ID for.</param>
        /// <returns>A player ID mapped to the provided connection ID. If the connection was not mapped to a player, returns -1.</returns>
        public static int ConnectionIDToPlayerID(ConnectionID connectionID)
        {
            if (ConnectionToPlayerMap.ContainsKey(connectionID))
            {
                return Player.ConnectionToPlayerMap[connectionID];
            }
            else
            {
                return -1;
            }
        }

        #endregion

        #region Members

        /// <summary>
        /// The numerical ID of the player.
        /// </summary>
        public int PlayerID
        {
            get;
            private set;
        }

        /// <summary>
        /// The internet connection ID associated with the player.
        /// </summary>
        public ConnectionID ConnectionID
        {
            get;
            private set;
        }

        /// <summary>
        /// The character selection choice the player has made.
        /// </summary>
        public int CharacterSelection
        {
            get;
            set;
        }

        /// <summary>
        /// Boolean to check whether this players Selection is locked
        /// </summary>
        public bool CharachterLocked
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Create a new Player, register it in the dictionary, and add it to a game server.
        /// </summary>
        /// <param name="playerID">The ID to assign to the player.</param>
        /// <param name="connection">The connection this player is associated with.</param>
        /// <param name="gameServer">The game server that this player will be registered under.</param>
        public Player(int playerID, ConnectionID connection, GameServer gameServer)
        {
            PlayerID = playerID;
            ConnectionID = connection;
            Player.PlayerToConnectionMap.Add(PlayerID, connection);
            Player.ConnectionToPlayerMap.Add(connection, PlayerID);
            gameServer.RegisterPlayer(this);
            return;
        }

    }
}
