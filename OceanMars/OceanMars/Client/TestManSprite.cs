using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;

namespace OceanMars.Client
{
    class TestManSprite : Sprite
    {
        public TestManSprite(View context, TestMan tm) :
            base(context,  tm, new Vector2(30, 30), 30, 10, "whitesquare") {
        }
    }
}
