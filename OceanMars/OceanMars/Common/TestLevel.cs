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
            TestWall w1 = new TestWall(this, new Vector2(-1000, 1000), new Vector2(-1000,500));
            TestWall w2 = new TestWall(this, new Vector2(-1000, 500), new Vector2(-700, -50));
            TestWall w3 = new TestWall(this, new Vector2(-700, -50), new Vector2(500, -50));
            TestWall w4 = new TestWall(this, new Vector2(500, -50), new Vector2(1000, 100));
            TestWall w5 = new TestWall(this, new Vector2(1000, 100), new Vector2(1000, 1000));
            TestWall w6 = new TestWall(this, new Vector2(100, 200), new Vector2(-200, 200));
            TestWall w7 = new TestWall(this, new Vector2(200, 400), new Vector2(400, 400));
            TestWall w8 = new TestWall(this, new Vector2(400, 400), new Vector2(400, 600));
            TestWall w9 = new TestWall(this, new Vector2(800, 600), new Vector2(400, 600));
            TestWall w10 = new TestWall(this, new Vector2(-200, -50), new Vector2(200, -20));

            this.addChild(w1);
            this.addChild(w2);
            this.addChild(w3);
            this.addChild(w4);
            this.addChild(w5);
            this.addChild(w6);
            this.addChild(w7);
            this.addChild(w8);
            this.addChild(w9);
            this.addChild(w10);
        }
    }
}
