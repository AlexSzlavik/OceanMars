using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;

namespace OceanMars.Client
{
    class FinishLineEntitySprite : Sprite
    {
        public FinishLineEntitySprite(View context, FinishLineEntity finishLine) :
            base(context, finishLine, new Vector2(2, 1), 1, 10, "redsquare")
        {
        }
    }
}
