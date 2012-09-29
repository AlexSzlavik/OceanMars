using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class ServerlessState : State
    {
        public TestMan player;

        public ServerlessState()
        {
            DefaultLevel dl = new DefaultLevel();
            player = new TestMan();
            TestWall wall = new TestWall();

            root.addChild(dl);
            root.addChild(player);
            root.addChild(wall);
        }
    }
}
