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
        /// Create a new GameServer.
        /// </summary>
        /// <param name="port">The port to create the GameServer on.</param>
        public GameServer(int port)
        {
            GameState = new State();
            GameNetworkServer = new NetworkServer(port);
            GameNetworkServer.RegisterGameDataUpdater(UpdateGameState);
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

    }
}

