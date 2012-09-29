using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class World : Entity
    {
        public World()
            : base(Vector2.Zero)
        {
        }

        public override Matrix getWorldTransform() { return new Matrix(); }
    }
}
