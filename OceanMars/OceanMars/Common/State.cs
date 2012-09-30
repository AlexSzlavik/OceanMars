using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class State : TransformChangeListener
    {
        private static Vector2 GRAVITY = new Vector2(0, 2.0f);

        private List<TransformChangeListener> scListeners = new List<TransformChangeListener>();

        public void handleTransformChange(Entity e)
        {
            foreach (TransformChangeListener scl in scListeners)
            {
                scl.handleTransformChange(e);
            }
        }

        public World root;
        public Dictionary<int, Entity> entities = new Dictionary<int,Entity>();

        public void registerEntity(Entity e)
        {
            entities.Add(e.id, e);
            e.addTransformChangeListener(this);
        }

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
