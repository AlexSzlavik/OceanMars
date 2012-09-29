using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class Entity
    {
        public int id;
        public Vector2 collisionBox;
        public Entity parent;
        public List<Entity> children;
        public Matrix transform;
        public Vector2 velocity;

        public virtual Matrix getWorldTransform() { return parent.getWorldTransform() * transform; }
    }
}
