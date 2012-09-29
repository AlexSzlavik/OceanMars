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
        /// Create a new GameServer.
        /// </summary>
        public GameServer()
        {
            GameState = new State();
            return;
        }

    }
}

