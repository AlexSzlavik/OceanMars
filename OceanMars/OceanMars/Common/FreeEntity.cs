using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    class FreeEntity : Entity
    {
        public float friction;

        public float frictionThreshold;

        public FreeEntity(Vector2 size, Entity parent) : base(size, parent)
        {
            // Defaults
            friction = 0.85f;
            frictionThreshold = 0.01f;
        }

        public void testCollision(List<Entity> entities)
        {
            velocity = applyAirFriction(velocity);
            transform = Matrix.CreateTranslation(new Vector3(velocity.X, velocity.Y, 0)) * transform;
        }

        public Vector2 applyAirFriction(Vector2 velocity)
        {
            Vector2 vp = velocity * friction;
            if (velocity.Length() < frictionThreshold) return Vector2.Zero;
            return vp;
        }
    }
}
