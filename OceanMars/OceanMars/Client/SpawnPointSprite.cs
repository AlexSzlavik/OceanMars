using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OceanMars.Common;
using Microsoft.Xna.Framework;

namespace OceanMars.Client
{
    class SpawnPointSprite : Sprite
    {
        public SpawnPointSprite(View context, SpawnPointEntity tm) :
            base(context,  tm, new Vector2(21, 21), 21, 10, "whitecrosshair") {
        }
    }
}
