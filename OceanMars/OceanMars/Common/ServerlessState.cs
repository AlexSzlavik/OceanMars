using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class ServerlessState : State
    {
        public EllipseEntity player;

        public ServerlessState()
        {
            DefaultLevel dl = new DefaultLevel();
            player = new EllipseEntity(new Vector2(10, 10));

            root.addChild(dl);
            root.addChild(player);

            entities.Add(dl.id, dl);
            entities.Add(player.id, player);
        }
    }
}
