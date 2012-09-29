using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;

namespace OceanMars.Client
{
    public class DefaultLevelSprite : Sprite
    {
        public DefaultLevelSprite(View context, DefaultLevel dl) :
            base(context,  dl, new Vector2(2400, 1000), 645, 10, "defaultlevel") {
        }
    }
}
