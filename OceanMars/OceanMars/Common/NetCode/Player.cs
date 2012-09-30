using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Common.NetCode
{
    public class Player
    {

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
        public bool CharacterLocked
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
        public Player(int playerID, ConnectionID connection, GameClient gameServer)
        {
            PlayerID = playerID;
            ConnectionID = connection;
            CharacterSelection = -1;
            CharacterLocked = false;
            gameServer.RegisterPlayer(this);
            return;
        }

    }
}
