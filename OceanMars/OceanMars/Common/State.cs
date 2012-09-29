﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class State
    {
        private static Vector2 GRAVITY = new Vector2(0, -9.8f);

        public World root = new World();
        Dictionary<int, Entity> entities = new Dictionary<int,Entity>();

        public State()
        {
        }

        void nextFrame()
        {
            foreach (Entity child in root.children)
            {
                if (child is EllipseEntity)
                {
                    EllipseEntity ellipseEntity = (EllipseEntity)child;
                    ellipseEntity.velocity += GRAVITY;
                    ellipseEntity.testCollision(entities.Values.ToList()); // TODO: using ToList may be inefficient
                }
                // Do not do collisions for SliderEntities
            }
        }
    }
}
