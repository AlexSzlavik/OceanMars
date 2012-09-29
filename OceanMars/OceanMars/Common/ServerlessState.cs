using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class ServerlessState : State
    {
        public Entity player;

        public ServerlessState()
        {
            player = new Entity(new Vector2(10, 10));
            root.addChild(player);
        }
    }
}
