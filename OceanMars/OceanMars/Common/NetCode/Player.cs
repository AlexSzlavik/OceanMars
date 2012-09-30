using System;

namespace OceanMars.Common.NetCode
{
    public class Player
    {

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
        /// Boolean to check whether this player's selection is locked.
        /// </summary>
        public bool CharacterLocked
        {
            get;
            set;
        }

        /// <summary>
        /// Create a new player, register it, and assign an ID.
        /// </summary>
        /// <param name="connection">The connection this player is associated with.</param>
        /// <param name="gameBase">The game server that this player will be registered under.</param>
        /// <param name="playerID">PlayerID to pass in. This only needs to be defined by clients.</param>
        public Player(ConnectionID connection, GameBase gameBase, int playerID = -1)
        {
            ConnectionID = connection;
            CharacterSelection = -1;
            CharacterLocked = false;
            if (playerID < 0)
            {
                PlayerID = gameBase.RegisterPlayer(this);
            }
            else
            {
                PlayerID = playerID;
                gameBase.RegisterPlayer(this);
            }
            return;
        }

    }
}
