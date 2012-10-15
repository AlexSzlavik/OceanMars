using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of a game server that rests on top of the network stack.
    /// </summary>
    public class GameServer : GameBase, TransformChangeListener, IStatePhaseListener
    {

        /// <summary>
        /// A mapping between players and connections.
        /// </summary>
        private Dictionary<Player, ConnectionID> PlayerToConnectionIDMap;

        /// <summary>
        /// A reverse mapping of connections to players
        /// </summary>
        private Dictionary<ConnectionID, Player> ConnectionIDToPlayerMap;

        /// <summary>
        /// Retrieve a connection ID from a player.
        /// </summary>
        /// <param name="player">The player to fetch a connection ID for.</param>
        /// <returns>A connection ID mapped to the provided player. If the player does not exist, returns null.</returns>
        public ConnectionID PlayerToConnectionID(Player player)
        {
            if (PlayerToConnectionIDMap.ContainsKey(player))
            {
                return PlayerToConnectionIDMap[player];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve a player from a connection ID.
        /// </summary>
        /// <param name="connectionID">The connection ID to fetch a player ID for. Null will return the LocalPlayer.</param>
        /// <returns>A player mapped to the provided connection ID. If the connection was not mapped to a player, returns null.</returns>
        public Player ConnectionIDToPlayer(ConnectionID connectionID)
        {
            if (connectionID == null)
            {
                return LocalPlayer;
            }
            else if (ConnectionIDToPlayerMap.ContainsKey(connectionID))
            {
                return ConnectionIDToPlayerMap[connectionID];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// The server lobby associated with this game server.
        /// </summary>
        public new LobbyServer Lobby
        {
            get
            {
                return (LobbyServer)base.Lobby;
            }
            private set
            {
                base.Lobby = value;
            }
        }

        /// <summary>
        /// Reference to the underlying network associated with this game server.
        /// </summary>
        public new NetworkServer Network
        {
            get
            {
                return (NetworkServer)base.Network;
            }
            private set
            {
                base.Network = value;
            }
        }

        /// <summary>
        /// The stack of IDs that are available to joining players.
        /// </summary>
        private Stack<int> availableIDs;

        /// <summary>
        /// Create a new GameServer.
        /// </summary>
        /// <param name="port">The port to create the GameServer on.</param>
        public GameServer(int port) : base(new NetworkServer(port))
        {
            availableIDs = new Stack<int>();
            for (int i = MAX_PLAYERS - 1; i >= 0; i--) // Add id's to the players
            {
                availableIDs.Push(i);
            }

            PlayerToConnectionIDMap = new Dictionary<Player, ConnectionID>();
            ConnectionIDToPlayerMap = new Dictionary<ConnectionID, Player>();

            Lobby = new LobbyServer(this); // Give initial control to the lobby
            Network.RegisterGameDataUpdater(Lobby.UpdateLobbyState);

            // Once we're done with the lobby, the connections and players will be handed off to the game and the GameDataUpdater re-registered to GameServer.UpdateGameState

            LocalPlayer = new Player(null, this); // Create and register self as a player (do this last as it needs access to the completed GameBase)
            return;
        }

        /// <summary>
        /// After choosing the level and number of players, start the game by sending everyone the level and a player ID
        /// </summary>
        public void SetupAndSendGameState(int levelID)
        {
            Level level = new Level(GameState.root, LevelPack.levels[levelID]);
            GameState.root.addChild(level);

            // Send level and a player ID to each client
            for (int i = 0; i < players.Length; i += 1)
            {
                Player player = GetPlayer(i);
                if (player != null)
                {
                    Network.SendGameData(new GameData(GameData.GameDataType.InitClientState, player.PlayerID, levelID), PlayerToConnectionID(player));

                    // Create a new testman at the given spawn point for each player
                    SpawnPointEntity sp = level.spawnPoints[i];
                    TestMan tm = new TestMan(sp);
                    sp.addChild(tm);
                }
            }
            return;
        }

        /// <summary>
        /// Register a new player with the game server.
        /// </summary>
        /// <param name="player">The player to register with the game server.</param>
        /// <returns>Returns the ID of the player to register.</returns>
        public override int RegisterPlayer(Player player)
        {
            PlayerToConnectionIDMap.Add(player, player.ConnectionID);
            if (player.ConnectionID != null) // Check if we're registering the local player
            {
                ConnectionIDToPlayerMap.Add(player.ConnectionID, player);
            }
            int playerID = availableIDs.Pop();
            players[playerID] = player;
            return playerID;
        }

        /// <summary>
        /// Unregister a player from the game server.
        /// </summary>
        /// <param name="player">The player to remove from the game client.</param>
        public override void UnregisterPlayer(Player player)
        {
            availableIDs.Push(player.PlayerID);
            PlayerToConnectionIDMap.Remove(player);
            if (player.ConnectionID != null) // Don't unregister the local player as he's not in this list already
            {
                ConnectionIDToPlayerMap.Remove(player.ConnectionID);
            }
            base.UnregisterPlayer(player);
            return;
        }

        /// <summary>
        /// Update the state over the game based on incoming game data.
        /// </summary>
        /// <param name="gameData">The received game data.</param>
        protected override void UpdateGameState(GameData gameData)
        {
            for (int i = 0; i < players.Length; i++ ) // Forward the received information to other machines (but not the one received from)
            {
                Player player = GetPlayer(i);
                if (i == gameData.PlayerID || player == null)
                {
                    continue;
                }
                Network.SendGameData(gameData, PlayerToConnectionID(players[i]));
                gameStatesToCommit.Add(gameData);
            }
            return;
        }

        /// <summary>
        /// Send new game state information to all players.
        /// </summary>
        public override void SendGameStates()
        {
            for (int i = 0; i < players.Length; i += 1)
            {
                Player player = GetPlayer(i);
                if (player != null)
                {
                    Network.SendGameData(gameStatesToSend, PlayerToConnectionID(players[i]));
                }
            }
            gameStatesToSend.Clear();
            return;
        }

        /// <summary>
        /// Commit the game state data into the state of the world.
        /// </summary>
        public override void CommitGameStates()
        {
            for (int i = 0; i < gameStatesToCommit.Count; i += 1)
            {
                GameData currentData = gameStatesToCommit[i];
                switch (currentData.Type)
                {
                    case GameData.GameDataType.Movement:
                        GameState.entities[currentData.TransformData.EntityID].transform = currentData.TransformData.GetMatrix();
                        break;
                    case GameData.GameDataType.PlayerTransform:
                        GameState.entities[currentData.TransformData.EntityID].transform = currentData.TransformData.GetMatrix();
                        break;
                    default:
                        throw new NotImplementedException("Unhandled state passed to GameServer");
                }
            }
            return;
        }

    }

}
