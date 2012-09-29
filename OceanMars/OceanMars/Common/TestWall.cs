using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class TestWall : SliderEntity
    {
            public TestWall (Entity parent) : base (new Vector2(-100, 0), new Vector2(100, 0), parent) {
            }
    }
}
