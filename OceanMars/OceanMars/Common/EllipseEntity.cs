using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class EllipseEntity : Entity
    {
        public Vector2 collisionEllipse;

        public EllipseEntity(Vector2 size) : base(size)
        {
            collisionEllipse = size;
        }

        public void testCollision(List<Entity> entities)
        {
            //Assumes that the state checks velocity to see if anything is actually moving
            //Assumes that the state checks AABBs to see if testing collisions makes sense

            foreach (Entity entity in entities) 
            {
                if (entity is SliderEntity)
                {
                    SliderEntity slider = (SliderEntity)entity;
                    Point origin = slider.endPoints[0];
                }
                else //entity is EllipseEntity
                {
                }
            }
        }
    }
}
