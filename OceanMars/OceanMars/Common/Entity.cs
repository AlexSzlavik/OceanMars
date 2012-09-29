using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class Entity
    {
        public static int next_id = 0;

        public int id;
        public Vector2 collisionBox;
        public Entity parent = null;
        public List<Entity> children = new List<Entity>();

        public bool worldTransformDirty = false;
        public bool inverseWorldTransformDirty = false;

        public Entity(Vector2 collisionBox, Entity parent) {
            this.collisionBox = collisionBox;
            this.id = next_id++;
            this.parent = parent;
        }

        public void addChild(Entity child)
        {
            children.Add(child);
            child.parent = this;

            registerChild(child);
        }

        public virtual void registerChild(Entity child)
        {
            parent.registerChild(child);
        }

        // When changed, will invalidate world transform matrix and all children
        private Matrix transformBack = Matrix.Identity;
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
        private Matrix worldTransformBack = Matrix.Identity;
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

            private set
            {
                worldTransformBack = value;
            }
        }

        // Handles caching of inverse world transformation as above
        private Matrix inverseWorldTransformBack = Matrix.Identity;
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

            private set {
                inverseWorldTransformBack = value;
            }
        }
    }
}
