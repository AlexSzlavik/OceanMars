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
        /// <summary>
        /// Reference to the upstairs gameserver
        /// </summary>
        private GameServer MainGameServer;

        /// <summary>
        /// Lobby constructor
        /// </summary>
        public Lobby(GameServer MainGameServer)
        {
            this.MainGameServer = MainGameServer;
        }

        /// <summary>
        /// Update hanlder from Network Layer
        /// This is only invoked until we pass control back to the Gameserver
        /// </summary>
        /// <param name="data"></param>
        public void UpdateGameState(GameData data)
        {
            switch (data.Type)
            {
                case GameData.GameDataType.Connect:
                    onPlayerConnect(data);
                    break;
                case GameData.GameDataType.SelectCharacter:
                    OnSelectCharacter(data);
                    break;
            }
            return;
        }

        /// <summary>
        /// A Player has joined, set them up in the system
        /// </summary>
        /// <param name="data"></param>
        private void onPlayerConnect(GameData data )
        {
            if (Player.ConnectionToPlayer(data.ConnectionInfo) == -1)
            {
                Player newPlayer = Player.CreateNewPlayer(data.ConnectionInfo);
                MainGameServer.Players.Add(newPlayer);
                GameData response = new GameData(GameData.GameDataType.Connect, newPlayer.PlayerID);
                MainGameServer.GameNetworkServer.SignalGameData(response, newPlayer.ConnectionID);
            }
            return;
        }

        /// <summary>
        /// A player has changed their character selection.
        /// </summary>
        /// <param name="gameData">The gamedata related to character selection.</param>
        private void OnSelectCharacter(GameData gameData)
        {
            Player player = MainGameServer.Players[Player.ConnectionToPlayer(gameData.ConnectionInfo)];
            player.CharacterSelection = gameData.EventDetail;
            MainGameServer.GameNetworkServer.BroadCastGameData(new GameData(GameData.GameDataType.SelectCharacter, player.PlayerID,gameData.EventDetail));
            return;
        }
    }
}
