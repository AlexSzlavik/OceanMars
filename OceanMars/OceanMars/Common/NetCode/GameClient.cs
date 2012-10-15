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
                if (players[i] == null) continue; // Don't create players that didn't join
                SpawnPointEntity sp = level.spawnPoints[i];
                System.Diagnostics.Debug.WriteLine(players[i].PlayerID + " " + myPlayerID);
                TestMan tm = new TestMan(sp, (players[i].PlayerID == myPlayerID));

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

        /// <summary>
        /// User Level connection method
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void ConnectToGame(String host, int port)
        {
            Network.Connect(host, port);
            Lobby.JoinLobby();
        }
    }

}

