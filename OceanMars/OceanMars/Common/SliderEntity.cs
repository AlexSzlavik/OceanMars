using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class SliderEntity : LineEntity
    {
        public float friction;
        public float staticFriction;
        public String name = "ill defined";

        public float frictionThreshold;

        public SliderEntity(Vector2 endPoint1, Vector2 endPoint2, Entity parent)
            : base(endPoint1, endPoint2, parent)
        {
            // Defaults
            friction = 1.0f;
            staticFriction = 0.0f;
            frictionThreshold = 0.01f;
        }

        public Vector2 applyFriction(Vector2 velocity)
        {
            Vector2 vp = velocity * friction;
            if(velocity.Length() < frictionThreshold) return Vector2.Zero;
            return vp;
        }
    }
}
