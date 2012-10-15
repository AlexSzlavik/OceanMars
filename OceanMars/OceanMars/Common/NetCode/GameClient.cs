using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of a game client program that rests on top of the network stack.
    /// </summary>
    public class GameClient : GameBase
    {

        /// <summary>
        /// A mapping of player IDs to entity IDs.
        /// </summary>
        private Dictionary<int, int> playerIDToEntity;

        /// <summary>
        /// The client lobby associated with this game client.
        /// </summary>
        public new LobbyClient Lobby
        {
            get
            {
                return (LobbyClient)base.Lobby;
            }
            private set
            {
                base.Lobby = value;
            }
        }

        /// <summary>
        /// Reference to the underlying network associated with this game client.
        /// </summary>
        public new NetworkClient Network
        {
            get
            {
                return (NetworkClient)base.Network;
            }
            private set
            {
                base.Network = value;
            }
        }

        /// <summary>
        /// Create a new GameClient.
        /// </summary>
        /// <param name="port">The port to create the GameServer on.</param>
        public GameClient() : base(new NetworkClient())
        {
            playerIDToEntity = new Dictionary<int, int>();

            Lobby = new LobbyClient(this); // Give initial control to the lobby
            Network.RegisterGameDataUpdater(Lobby.UpdateLobbyState);

            // TODO: Once we're done with the lobby, the connections and players will be handed off to the game and the GameDataUpdater re-registered to GameClient.UpdateGameState

            return;
        }

        /// <summary>
        /// After choosing the level and number of players, start the game.
        /// </summary>
        /// <param name="levelID">The ID of the level to initialize.</param>
        /// <param name="myPlayerID">The ID of the player character.</param>
        public void SetupGameState(int levelID, int myPlayerID)
        {
            Level level = new Level(GameState.root, LevelPack.levels[levelID]);
            GameState.root.addChild(level);

            for (int i = 0; i < players.Length; i++)
            {
                Player player = GetPlayer(i);
                if (player != null)
                {
                    SpawnPointEntity sp = level.spawnPoints[i];
                    TestMan tm = new TestMan(sp, (i == myPlayerID));
                    playerIDToEntity[i] = tm.id;
                    sp.addChild(tm);
                }
            }
            return;
        }

        /// <summary>
        /// Register a new player with the game client.
        /// </summary>
        /// <param name="player">The player to register with the game client.</param>
        /// <returns>Returns the ID of the player to register.</returns>
        public override int RegisterPlayer(Player player)
        {
            players[player.PlayerID] = player;
            return player.PlayerID;
        }

        /// <summary>
        /// Send any built of game state changes over the network.
        /// </summary>
        public override void SendGameStates()
        {
            Network.SendGameData(gameStatesToSend);
            gameStatesToSend.Clear();
            return;
        }

        /// <summary>
        /// Connect the client to a hosting server.
        /// </summary>
        /// <param name="host">The host address.</param>
        /// <param name="port">The port number to connect to.</param>
        public void ConnectToGame(String host, int port)
        {
            Network.Connect(host, port);
            Lobby.JoinLobby();
            return;
        }

        /// <summary>
        /// Commit any updated game states into the state of the world.
        /// </summary>
        public override void CommitGameStates()
        {
            for (int i = 0; i < gameStatesToCommit.Count; i += 1)
            {
                GameData currentData = gameStatesToCommit[i];
                switch (currentData.Type)
                {
                    case GameData.GameDataType.InitClientState:
                        SetupGameState(currentData.EventDetail, currentData.PlayerID);
                        break;
                    case GameData.GameDataType.Movement:
                        GameState.entities[currentData.TransformData.EntityID].transform = currentData.TransformData.getMatrix();
                        break;
                    case GameData.GameDataType.PlayerTransform:
                        GameState.entities[playerIDToEntity[currentData.TransformData.EntityID]].transform = currentData.TransformData.getMatrix();
                        break;
                    default:
                        throw new NotImplementedException("Unhandled state passed to GameClient");
                }
            }
            gameStatesToCommit.Clear();
            return;
        }
    }

}

