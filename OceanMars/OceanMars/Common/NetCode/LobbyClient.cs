using System;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// A client-side Lobby.
    /// </summary>
    public class LobbyClient : LobbyBase
    {

         /// <summary>
        /// Reference to the game client associated with this lobby.
        /// </summary>
        public new GameClient Game
        {
            get
            {
                return (GameClient)base.Game;
            }
            private set
            {
                base.Game = value;
            }
        }

        /// <summary>
        /// Create a new client-side lobby.
        /// </summary>
        /// <param name="gameClient">The game client associated with this lobby.</param>
        public LobbyClient(GameClient gameClient) : base(gameClient)
        {
            return;
        }

        /// <summary>
        /// A Player has joined, set them up in the system.
        /// </summary>
        /// <param name="gameData">The game data related to the character joining the session.</param>
        protected override void OnPlayerConnect(GameData gameData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A player has changed their character selection.
        /// </summary>
        /// <param name="gameData">The game data related to character selection.</param>
        protected override void OnPlayerSelectCharacter(GameData gameData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hanldes the character lock request
        /// </summary>
        /// <param name="gameData">The game data related to character locking.</param>
        protected override void OnPlayerLockCharacter(GameData gameData)
        {
            throw new NotImplementedException();
        }

    }
}
