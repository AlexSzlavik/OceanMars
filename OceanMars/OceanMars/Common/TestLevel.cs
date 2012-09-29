using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class TestLevel : Entity
    {
        public TestLevel(Entity parent) : base (new Vector2(0, 0), parent)
        {
            TestWall w1 = new TestWall(this);

            this.addChild(w1);

            w1.transform =
                Matrix.CreateTranslation(0, 150, 0) *
                w1.transform *
                Matrix.CreateRotationZ((float)Math.PI / 4) *
                Matrix.CreateScale(2, 1, 1);
        }
    }
}
