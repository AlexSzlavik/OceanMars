using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class State
    {
        private static Vector2 GRAVITY = new Vector2(0, 0f);

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

                    // For testing purposes, just add velocity
                    if (ellipseEntity.velocity != Vector2.Zero)
                    {
                        ellipseEntity.transform =
                            Matrix.CreateTranslation(new Vector3(ellipseEntity.velocity.X, ellipseEntity.velocity.Y, 0)) *
                            ellipseEntity.transform;
                    }
                }
                // Do not do collisions for SliderEntities
            }
        }
    }
}
