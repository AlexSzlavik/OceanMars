using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;

namespace OceanMars.Client
{
    class EditorManSprite : Sprite
    {
        public EditorManSprite(View context, EditorMan tm) :
            base(context,  tm, new Vector2(30, 30), 30, 10, "whitesquare") {
        }
    }
}
