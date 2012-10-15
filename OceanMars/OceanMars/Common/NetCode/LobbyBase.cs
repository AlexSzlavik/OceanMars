using System;

namespace OceanMars.Common.NetCode
{
    /// <summary>
    /// Abstract base class representing a game lobby. Inherited classes will handle players connecting/disconnecting during game setup, level selection, character and team select etc.
    /// </summary>
    public abstract class LobbyBase
    {

        /// <summary>
        /// Reference to the game server associated with this lobby. gameserver
        /// </summary>
        protected GameBase Game
        {
            get;
            set;
        }

        /// <summary>
        /// Lobby constructor
        /// </summary>
        /// <param name="gameBase">The game base that this Lobby is associated with.</param>
        protected LobbyBase(GameBase gameBase)
        {
            Game = gameBase;
            return;
        }

        /// <summary>
        /// Update hanlder from the network layer. This is only invoked until control passes back to the game server.
        /// </summary>
        /// <param name="gameData">Game data being used to update the state of the game.</param>
        public void UpdateLobbyState(GameData gameData)
        {
            switch (gameData.Type)
            {
                case GameData.GameDataType.Connect:
                    OnPlayerConnect(gameData);
                    break;
                case GameData.GameDataType.SelectCharacter:
                    OnPlayerSelectCharacter(gameData);
                    break;
                case GameData.GameDataType.LockCharacter:
                    OnPlayerLockCharacter(gameData);
                    break;
                case GameData.GameDataType.GameStart:
                    OnGameStart(gameData);
                    break;
                default:
                    throw new ArgumentException();
            }
            return;
        }

        /// <summary>
        /// Run logic for a player connecting to or disconnecting from the game.
        /// </summary>
        /// <param name="gameData">The game data related to the player's actions.</param>
        protected abstract void OnPlayerConnect(GameData gameData);

        /// <summary>
        /// Run logic for a player choosing between different characters.
        /// </summary>
        /// <param name="gameData">The game data related to the player's actions.</param>
        protected abstract void OnPlayerSelectCharacter(GameData gameData);

        /// <summary>
        /// Run logic for when a player locks or unlocks a character.
        /// </summary>
        /// <param name="gameData">The game data related to the player's actions.</param>
        protected abstract void OnPlayerLockCharacter(GameData gameData);

        /// <summary>
        /// Handles a Game Start Request
        /// </summary>
        /// <param name="gameData">The game data related to character locking.</param>
        protected abstract void OnGameStart(GameData gameData);
   
    }

}
