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
        public GameClient() : base(new NetworkClient())
        {
            Lobby = new LobbyClient(this); // Give initial control to the lobby
            Network.RegisterGameDataUpdater(Lobby.UpdateLobbyState);

            // Once we're done with the lobby, the connections and players will be handed off to the game and the GameDataUpdater re-registered to GameClient.UpdateGameState

            return;
        }

        /// <summary>
        /// After choosing the level and number of players, start the game
        /// </summary>
        public void setupGameState(int levelID, int myPlayerID)
        {
            Entity root = GameState.root;

            Level level = new Level(root, LevelPack.levels[levelID]);
            root.addChild(level);

            for (int i = 0; i < players.Length; i++)
            {
                SpawnPointEntity sp = level.spawnPoints[i];
                TestMan tm = new TestMan(sp, (i == myPlayerID));

                players[i].EntityID = tm.id;
                sp.addChild(tm);
            }
        }

        public Entity getPlayerEntity()
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

        public override void sendGameStates()
        {
            List<GameData> lgd = new List<GameData>(GameStatesToSend.Values);
            Network.SendGameData(lgd);
            GameStatesToSend.Clear();
        }

        public override void commitGameStates()
        {
            foreach (GameData gs in GameStatesToCommit)
            {
                if (gs.Type == GameData.GameDataType.Movement)
                {
                    int id = gs.TransformData.EntityID;
                    GameState.entities[id].transform = gs.TransformData.getMatrix();
                }
                else if (gs.Type == GameData.GameDataType.PlayerTransform)
                {
                    int id = PlayerIDToEntity[gs.TransformData.EntityID];
                    GameState.entities[id].transform = gs.TransformData.getMatrix();
                }
                else if (gs.Type == GameData.GameDataType.InitClientState)
                {
                    setupGameState(gs.EventDetail, gs.PlayerID);
                    // START the game!!
                }
            }

            GameStatesToCommit.Clear();
        }
    }

}

