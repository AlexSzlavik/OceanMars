using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of game server logic.
    /// </summary>
    public class GameServer
    {
        /// <summary>
        /// The hierarchical tree that represents the state of the game.
        /// </summary>
        public State GameState
        {
            get;
            private set;
        }

        /// <summary>
        /// The underlying network server that is used by this game.
        /// </summary>
        public NetworkServer GameNetworkServer
        {
            get;
            private set;
        }

        /// <summary>
        /// The Lobby code. 
        /// Mostly the interface will interface with this. 
        /// Eventually it will hand control to the GameServers main logic
        /// </summary>
        public Lobby GameLobby
        {
            get;
            private set;
        }

        /// <summary>
        /// Whether or not this GameServer represents a host.
        /// </summary>
        public bool IsHosting
        {
            get;
            private set;
        }

        /// <summary>
        /// The list of players currently registered with this game server.
        /// </summary>
        public List<Player> Players
        {
            get;
            private set;
        }

        /// <summary>
        /// Create a new GameServer.
        /// </summary>
        /// <param name="port">The port to create the GameServer on.</param>
        public GameServer(int port, bool isHosting)
        {
            GameState = new State();
            Players = new List<Player>();
            GameNetworkServer = new NetworkServer(port);

            //We first give control to the Lobby
            GameLobby = new Lobby(this);
            GameNetworkServer.RegisterGameDataUpdater(GameLobby.UpdateGameState);
            IsHosting = isHosting;

            //Once we are done with the lobby we have it 
            //hand off it's connections and player setup
            //The lobby will then close down and the GameServer processses the rest of the 
            //information

            return;
        }

        /// <summary>
        /// Update the game state based on incoming game data.
        /// </summary>
        /// <param name="gameData">Received game data that should inform us about changing state, requests, etc.</param>
        public void UpdateGameState(GameData gameData)
        {
            return;
        }

        /// <summary>
        /// Register a new player with the game server.
        /// </summary>
        /// <param name="player">The player to register with the game server.</param>
        public void RegisterPlayer(Player player)
        {
            Players.Add(player);
            return;
        }

    }
}

