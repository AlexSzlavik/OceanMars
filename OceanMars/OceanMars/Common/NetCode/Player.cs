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
        /// Lookup function
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static ConnectionID PlayerToConnection(int playerId)
        {
            return Player.PlayerToConnectionMap[playerId];
        }

        /// <summary>
        /// Get a player ID from a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static int ConnectionToPlayer(ConnectionID connection)
        {
            return Player.ConnectionToPlayerMap[connection];
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
            gameServer.RegisterPlayer(this);
            return;
        }

        /// <summary>
        /// Destructor, Clean up the map
        /// </summary>
        ~Player()
        {
            Player.PlayerToConnectionMap.Remove(this.PlayerID);
        }
    }
}
