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
        public GameClient()
            : base(new NetworkClient())
        {

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

            SpawnPointEntity sp = level.spawnPoints[myPlayerID];
            TestMan tm = new TestMan(sp, true);
            players[myPlayerID].EntityID = tm.id;
            LocalPlayer.EntityID = players[myPlayerID].EntityID;    //hack?

            EntityData entityData = new EntityData(EntityData.EntityType.TestMan, tm.id, tm.transform);
            GameData gameData = new GameData(GameData.GameDataType.NewEntity, myPlayerID, 0, null, entityData);
            Network.SendGameData(gameData);
            return;
        }

        /// <summary>
        /// Get the entity associated with the local player.
        /// </summary>
        /// <returns>An entity object associated wtih local player object.</returns>
        public Entity GetPlayerEntity()
        {
            return GameState.entities[LocalPlayer.EntityID];
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

        public override void CommitGameStates()
        {
            lock (gameStatesToCommit)
            {
                for (int i = 0; i < gameStatesToCommit.Count; ++i)
                {
                    GameData gameState = gameStatesToCommit[i];
                    if (gameState != null)
                    {
                        switch (gameState.Type)
                        {
                            case GameData.GameDataType.Movement:
                                GameState.entities[gameState.TransformData.EntityID].transform = gameState.TransformData.GetMatrix();
                                break;
                            case GameData.GameDataType.PlayerTransform:
                                GameState.entities[players[gameState.TransformData.EntityID].EntityID].transform = gameState.TransformData.GetMatrix();
                                break;
                            case GameData.GameDataType.NewEntity:
                                //hack, need to use something more than TransformData
                                //assuming TestMan for now

                                TestMan testMan = new TestMan(GameState.root, false, gameState.EntityData.transformData.EntityID);
                                testMan.transform = gameState.TransformData.GetMatrix();
                                break;
                            default:
                                // throw new NotImplementedExption("Received unexpected gamestate");
                                break;
                        }

                    }
                }
                gameStatesToCommit.Clear();
            }
            return;
        }

        /// <summary>
        /// Send any built of game state changes over the network.
        /// </summary>
        public override void SendGameStates()
        {
            lock (gameStatesToSend)
            {
                Network.SendGameData(gameStatesToSend);
                gameStatesToSend.Clear();
            }
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
    }

}

