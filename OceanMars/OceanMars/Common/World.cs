﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class World : Entity
    {
        public State owner;

        public World(State owner)
            : base(Vector2.Zero, null)
        {
            this.owner = owner;
        }

        public override void registerChild(Entity child)
        {
            owner.registerEntity(child);
        }
    }
}
