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

        public bool worldTransformDirty = false;
        public bool inverseWorldTransformDirty = false;

        // When changed, will invalidate world transform matrix and all children
        private Matrix transformBack;
        public Matrix transform
        {
            get
            {
                return transformBack;
            }

            set
            {
                transformBack = value;
                worldTransformDirty = true;
                inverseWorldTransformDirty = true;
                foreach (Entity child in children)
                {
                    child.worldTransformDirty = true;
                    child.inverseWorldTransformDirty = true;
                }
            }
        }

        public Vector2 velocity;

        // Handles caching of world transform matrix to avoid redundant work
        private Matrix worldTransformBack;
        public Matrix worldTransform
        {
            get
            {
                if (worldTransformDirty)
                {
                    worldTransformBack = parent.worldTransform * transform;
                    worldTransformDirty = false;
                }
                return worldTransformBack;
            }

            private set;
        }

        // Handles caching of inverse world transformation as above
        private Matrix inverseWorldTransformBack;
        public Matrix inverseWorldTransform
        {
            get
            {
                if (inverseWorldTransformDirty)
                {
                    inverseWorldTransformBack = Matrix.Invert(worldTransform);
                    inverseWorldTransformDirty = false;
                }
                return inverseWorldTransformBack;
            }

            private set;
        }

        public virtual Matrix getWorldTransform() { return parent.getWorldTransform() * transform; }
    }
}
