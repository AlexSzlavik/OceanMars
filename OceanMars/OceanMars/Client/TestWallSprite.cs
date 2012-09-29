using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OceanMars.Common;
using Microsoft.Xna.Framework;

namespace OceanMars.Client
{

    class TestWallSprite: Sprite
    {
        public TestWallSprite (View context, TestWall tm) :
            base(context, tm, new Vector2(200, 1), 1, 10, "blacksquare")
        {
        }
    }
}
