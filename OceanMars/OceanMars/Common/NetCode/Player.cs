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
        /// The next available PlayerId
        /// This is used in the Factory
        /// </summary>
        private static int nextPlayerId = 1;

        /// <summary>
        /// A Mapping between playerIDs and actual Connections
        /// </summary>
        private static Dictionary<int, ConnectionID> PlayerToConnectionMap = new Dictionary<int, ConnectionID>();

        /// <summary>
        /// A reverse mapping of connections to players
        /// </summary>
        private static Dictionary<ConnectionID, int> ConnectionToPlayerMap = new Dictionary<ConnectionID, int>();

        #endregion

        #region Static Methods

        /// <summary>
        /// Factory Pattern
        /// </summary>
        /// <returns></returns>
        public static Player CreateNewPlayer(ConnectionID connection)
        {
            Player p = new Player(nextPlayerId++, connection);
            Player.PlayerToConnectionMap.Add(p.PlayerID, connection);
            Player.ConnectionToPlayerMap.Add(connection, p.PlayerID);
            return p;
        }

        /// <summary>
        /// Lookup function
        /// </summary>
        /// <param name="playerId"></param>
        /// <returns></returns>
        public static ConnectionID PlayerToConnection(int playerId)
        {
            if(Player.PlayerToConnectionMap.ContainsKey(playerId))
                return Player.PlayerToConnectionMap[playerId];
            else
                return null;
        }

        /// <summary>
        /// Get a player ID from a connection
        /// </summary>
        /// <param name="connection"></param>
        /// <returns></returns>
        public static int ConnectionToPlayer(ConnectionID connection)
        {
            if(ConnectionToPlayerMap.ContainsKey(connection))
                return Player.ConnectionToPlayerMap[connection];
            else
                return -1;
        }

        #endregion

        #region Members

        public int PlayerID
        {
            get;
            private set;
        }

        public ConnectionID ConnectionID
        {
            get;
            private set;
        }

        public int CharacterSelection
        {
            get;
            set;
        }

        #endregion

        /// <summary>
        /// Constructor
        /// </summary>
        private Player(int playerID, ConnectionID connection)
        {
            this.PlayerID = playerID;
            this.ConnectionID = connection;
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
