using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;

namespace OceanMars.Common
{
    class EditorMan : FreeEntity
    {
        public EditorMan(Entity parent) : base(new Vector2(21, 21), parent) {
        }
    }
}
