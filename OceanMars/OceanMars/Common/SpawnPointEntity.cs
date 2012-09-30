using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class SpawnPointEntity : Entity
    {
        public Vector2 spawn;

        public SpawnPointEntity (Entity parent, Vector2 spawn) : base (spawn, parent)
        {
            this.spawn = spawn;
        }
    }
}
