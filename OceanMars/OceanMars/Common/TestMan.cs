using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common.NetCode;

namespace OceanMars.Common
{
    class TestMan : EllipseEntity
    {
        public TestMan(Entity parent) : base(new Vector2(30, 30), parent) {
        }

        public override StateChange createStateChange()
        {
            StateChange sc = base.createStateChange();

            return sc;
        }
    }
    
}
