using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class State
    {
        private static Vector2 GRAVITY = new Vector2(0, 0.98f);

        public World root;
        public Dictionary<int, Entity> entities = new Dictionary<int,Entity>();

        public State()
        {
            root = new World(this);
        }

        public void nextFrame()
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
