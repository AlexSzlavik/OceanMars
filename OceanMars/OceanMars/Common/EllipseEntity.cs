using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class EllipseEntity : Entity
    {
        public Vector2 collisionEllipse { public get; private set; }

        public EllipseEntity(Vector2 size)
        {
            collisionEllipse = size;
        }

        public void testCollision(Entity entity)
        {
            //Assumes that the entity manager checks AABBs to see if testing collisions makes sense

            //Check if Entity is a SliderEntity

            //Check if Entity is an EllipseEntity
        }
    }
}
