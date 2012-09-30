using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OceanMars.Common.NetCode;

namespace OceanMars.Common.NetCode
{
    /// <summary>
    /// The Lobby is the server side Logic lobby interface
    /// This will handle players connecting / disconnecting from the game
    /// It will also setup the game, LEVEL Select, Character Select, Team Select etc
    /// The menu code will probably query this information as we go
    /// </summary>
    public class Lobby
    {

        const int MAX_PLAYERS = 8; // Maximum number of players allowed in a lobby

        /// <summary>
        /// The stack of IDs that are available to joining players.
        /// </summary>
        Stack<int> availableIDs; 

        /// <summary>
        /// Reference to the game server associated with this lobby. gameserver
        /// </summary>
        public GameServer GameServer
        {
            get;
            private set;
        }

        /// <summary>
        /// Lobby constructor
        /// </summary>
        /// <param name="gameServer">The game server that this Lobby is associated with.</param>
        public Lobby(GameServer gameServer)
        {
            availableIDs = new Stack<int>();
            for (int i = MAX_PLAYERS - 1; i > 0; i--) // Add id's to the players
            {
                availableIDs.Push(i);
            }
            GameServer = gameServer;
            return;
        }

        /// <summary>
        /// Update hanlder from the network layer. This is only invoked until control passes back to the game server.
        /// </summary>
        /// <param name="gameData">Game data being used to update the state of the game.</param>
        public void UpdateGameState(GameData gameData)
        {
            switch (gameData.Type)
            {
                case GameData.GameDataType.Connect:
                    OnPlayerConnect(gameData);
                    break;
                case GameData.GameDataType.SelectCharacter:
                    OnSelectCharacter(gameData);
                    break;
                case GameData.GameDataType.LockCharacter:
                    onCharacterLock(gameData);
                    break;
            }
            return;
        }

        /// <summary>
        /// A Player has joined, set them up in the system.
        /// </summary>
        /// <param name="gameData">The game data related to the character joining the session.</param>
        private void OnPlayerConnect(GameData gameData)
        {
            switch ((GameData.ConnectionDetails)gameData.EventDetail)
            {
                case GameData.ConnectionDetails.IdReqest:
                    if (Player.ConnectionIDToPlayerID(gameData.ConnectionInfo) == -1)
                    {
                        Player newPlayer = new Player(availableIDs.Pop(),gameData.ConnectionInfo, GameServer);
                        GameData response = new GameData(GameData.GameDataType.Connect, newPlayer.PlayerID);
                        GameServer.GameNetworkServer.SignalGameData(response, newPlayer.ConnectionID);
                    }
                    break;
                case GameData.ConnectionDetails.Connected:
                    break;
                case GameData.ConnectionDetails.Disconnected:
                    break;
                case GameData.ConnectionDetails.Dropped:
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
        private void OnSelectCharacter(GameData gameData)
        {
            Player player = GameServer.Players[Player.ConnectionIDToPlayerID(gameData.ConnectionInfo)];
            player.CharacterSelection = gameData.EventDetail;
            GameServer.GameNetworkServer.BroadCastGameData(new GameData(GameData.GameDataType.SelectCharacter, player.PlayerID,gameData.EventDetail));
            return;
        }

        /// <summary>
        /// Hanldes the character lock request
        /// </summary>
        /// <param name="gameData"></param>
        private void onCharacterLock(GameData gameData)
        {
            Player player = GameServer.Players[Player.ConnectionIDToPlayerID(gameData.ConnectionInfo)];
            player.CharachterLocked = true;
            GameServer.GameNetworkServer.BroadCastGameData(new GameData(GameData.GameDataType.LockCharacter,player.PlayerID));
            return;
        }
    }
}
