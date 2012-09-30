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
            //DefaultLevel dl = new DefaultLevel(root);
            player = new TestMan(root);
            LevelPack lp = new LevelPack(root);
            //TestLevel tl = new TestLevel(root);
            //TestWall w = new TestWall(root);

            //root.addChild(dl);
            root.addChild(player);
            //root.addChild(tl);
            root.addChild(lp);
            //root.addChild(w);

        }
    }
}
