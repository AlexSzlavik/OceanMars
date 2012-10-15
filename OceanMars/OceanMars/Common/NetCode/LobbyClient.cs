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
            GameData response;
            switch ((GameData.ConnectionDetails)gameData.EventDetail)
            {
                case GameData.ConnectionDetails.IdReqest:
                    Game.LocalPlayer = new Player(null, Game, gameData.PlayerID);
                    break;
                case GameData.ConnectionDetails.Connected: // Register a new player on a client
                    new Player(null, Game, gameData.PlayerID);
                    break;
                case GameData.ConnectionDetails.Disconnected: // Drop a connected player from the client
                case GameData.ConnectionDetails.Dropped:
                        Game.UnregisterPlayer(Game.GetPlayer(gameData.PlayerID));
                    break;
                default:
                    throw new ArgumentException();
            }
            return;
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

        /// <summary>
        /// Join the game lobby
        /// </summary>
        public void JoinLobby()
        {
            //Do we want to actually join the lobby on top of joinging the Server?
            GameData changeRequest = new GameData(GameData.GameDataType.Connect, 0, (int)GameData.ConnectionDetails.IdReqest);
            Game.Network.SendGameData(changeRequest);
        }

        /// <summary>
        /// The uesr makes a change to the character selection
        /// </summary>
        /// <param name="character"></param>
        public void SelectCharacter(int character)
        {
            GameData changeRequest = new GameData(GameData.GameDataType.SelectCharacter, Game.LocalPlayer.PlayerID, character);
            Game.Network.SendGameData(changeRequest);
        }

        /// <summary>
        /// The uesr makes a change to the character selection
        /// </summary>
        /// <param name="character"></param>
        public void LockCharacter()
        {
            GameData changeRequest = new GameData(GameData.GameDataType.LockCharacter, Game.LocalPlayer.PlayerID);
            Game.Network.SendGameData(changeRequest);
        }

        /// <summary>
        /// The Host requests the game to start
        /// </summary>
        /// <param name="character"></param>
        public void StartGame()
        {
            GameData changeRequest = new GameData(GameData.GameDataType.GameStart,Game.LocalPlayer.PlayerID);
            Game.Network.SendGameData(changeRequest);
        }

        /// <summary>
        /// Handles a Game Start Request
        /// </summary>
        /// <param name="gameData">The game data related to character locking.</param>
        protected override void OnGameStart(GameData gameData)
        {
            Game.startGame();
            return;
        }

        /// <summary>
        /// Sends a start game request to the server
        /// NOTE: Only a Client THAT ALSO HAS A SERVER SHOULD BE ABLE TO DO THIS
        /// Meaning this is a temporary hack too!
        /// </summary>
        /// <param name="gameData">The game data related to character locking.</param>
        public void SendLaunchPacket()
        {
            Game.Network.SendGameData(new GameData(GameData.GameDataType.GameStart));
            return;
        }
    }
}
