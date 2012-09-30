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
        public GameClient GameClient
        {
            get;
            private set;
        }

        /// <summary>
        /// Lobby constructor
        /// </summary>
        /// <param name="gameServer">The game server that this Lobby is associated with.</param>
        public Lobby(GameClient gameServer)
        {
            availableIDs = new Stack<int>();
            for (int i = MAX_PLAYERS - 1; i > 0; i--) // Add id's to the players
            {
                availableIDs.Push(i);
            }
            GameClient = gameServer;
            return;
        }

        /// <summary>
        /// Update hanlder from the network layer. This is only invoked until control passes back to the game server.
        /// </summary>
        /// <param name="gameData">Game data being used to update the state of the game.</param>
        public void UpdateGameState(GameData gameData)
        {
            if (GameClient.IsHosting)
            {
                switch (gameData.Type)
                {
                    case GameData.GameDataType.Connect:
                        OnPlayerConnectServer(gameData);
                        break;
                    case GameData.GameDataType.SelectCharacter:
                        OnSelectCharacterServer(gameData);
                        break;
                    case GameData.GameDataType.LockCharacter:
                        OnCharacterLockServer(gameData);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            else
            {
                switch (gameData.Type)
                {
                    case GameData.GameDataType.Connect:
                        OnPlayerConnectClient(gameData);
                        break;
                    case GameData.GameDataType.SelectCharacter:
                        OnSelectCharacterClient(gameData);
                        break;
                    case GameData.GameDataType.LockCharacter:
                        OnCharacterLockClient(gameData);
                        break;
                    default:
                        throw new ArgumentException();
                }
            }
            return;
        }

        /// <summary>
        /// A Player has joined, set them up in the system.
        /// </summary>
        /// <param name="gameData">The game data related to the character joining the session.</param>
        private void OnPlayerConnectServer(GameData gameData)
        {
            GameData response;
            Player player = GameClient.ConnectionIDToPlayer(gameData.ConnectionInfo);
            switch ((GameData.ConnectionDetails)gameData.EventDetail)
            {
                case GameData.ConnectionDetails.IdReqest:
                    if (player == null) // If this is a new player, assign them a new ID, otherwise just resend the old id
                    {
                        player = new Player(availableIDs.Pop(), gameData.ConnectionInfo, GameClient);
                    }
                    response = new GameData(GameData.GameDataType.Connect, player.PlayerID);
                    GameClient.GameNetworkServer.SignalGameData(response, gameData.ConnectionInfo);
                    // TODO: Re-broadcast changes to clients
                    break;
                case GameData.ConnectionDetails.Disconnected:
                case GameData.ConnectionDetails.Dropped:
                    if (player != null) // This is a known player based on their connection, so we can drop them
                    {
                        availableIDs.Push(player.PlayerID);
                        GameClient.UnregisterPlayer(player);

                        // TODO: Re-broadcast changes to clients
                        
                    }
                    break;
                default:
                    throw new ArgumentException();
            }
            return;
        }

        /// <summary>
        /// A Player has joined, set them up in the system.
        /// </summary>
        /// <param name="gameData">The game data related to the character joining the session.</param>
        private void OnPlayerConnectClient(GameData gameData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// A player has changed their character selection.
        /// </summary>
        /// <param name="gameData">The game data related to character selection.</param>
        private void OnSelectCharacterServer(GameData gameData)
        {
            Player player = GameClient.ConnectionIDToPlayer(gameData.ConnectionInfo);
            player.CharacterSelection = gameData.EventDetail;
            GameClient.GameNetworkServer.BroadCastGameData(new GameData(GameData.GameDataType.SelectCharacter, player.PlayerID,gameData.EventDetail));
            return;
        }

        /// <summary>
        /// A player has changed their character selection.
        /// </summary>
        /// <param name="gameData">The game data related to character selection.</param>
        private void OnSelectCharacterClient(GameData gameData)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Hanldes the character lock request
        /// </summary>
        /// <param name="gameData">The game data related to character locking.</param>
        private void OnCharacterLockServer(GameData gameData)
        {
            Player player = GameClient.ConnectionIDToPlayer(gameData.ConnectionInfo);
            player.CharacterLocked = true;
            GameClient.GameNetworkServer.BroadCastGameData(new GameData(GameData.GameDataType.LockCharacter,player.PlayerID));
            return;
        }

        /// <summary>
        /// Hanldes the character lock request
        /// </summary>
        /// <param name="gameData">The game data related to character locking.</param>
        private void OnCharacterLockClient(GameData gameData)
        {
            Player player = GameClient.ConnectionIDToPlayer(gameData.ConnectionInfo);
            player.CharacterLocked = true;
            GameClient.GameNetworkServer.BroadCastGameData(new GameData(GameData.GameDataType.LockCharacter, player.PlayerID));
            return;
        }
    }
}
