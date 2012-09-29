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

                Matrix.CreateScale(5, 1, 1) *
                Matrix.CreateRotationZ((float)Math.PI / 4) *
                w1.transform *
                Matrix.CreateTranslation(0, 150, 0);
        }
    }
}
