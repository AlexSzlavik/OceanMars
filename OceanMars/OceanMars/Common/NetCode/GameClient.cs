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
            GameState.root.addChild(level);

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] == null) continue; // Don't create players that didn't join
                SpawnPointEntity sp = level.spawnPoints[i];
                System.Diagnostics.Debug.WriteLine(players[i].PlayerID + " " + myPlayerID);
                TestMan tm;

                //supercalifragilistic uber hacky:
                if (players[i].PlayerID != myPlayerID)
                {
                    int additionalHackyAddition = players[i].PlayerID < myPlayerID ? Entity.next_id : Entity.next_id - 1;
                    tm = new TestMan(sp,
                                     (players[i].PlayerID == myPlayerID),
                                     players[i].PlayerID * (int.MaxValue / GameBase.MAX_PLAYERS) + additionalHackyAddition - myPlayerID * (int.MaxValue / GameBase.MAX_PLAYERS));
                }
                else
                {
                    tm = new TestMan(sp, (players[i].PlayerID == myPlayerID));
                }

                System.Diagnostics.Debug.WriteLine("EntityID: " + tm.id);
                players[i].EntityID = tm.id;
                sp.addChild(tm);

                //hack?
                if (players[i].PlayerID == myPlayerID)
                {
                    LocalPlayer.EntityID = players[i].EntityID;
                }
            }
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

        /// <summary>
        /// Send any built of game state changes over the network.
        /// </summary>
        public override void SendGameStates()
        {
            lock (gameStatesToCommit)
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

