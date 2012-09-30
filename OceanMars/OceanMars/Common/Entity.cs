using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common.NetCode;

namespace OceanMars.Common
{
    public class Entity
    {
        public static int next_id = 0;

        public int id;
        public Vector2 collisionBox;
        public Entity parent = null;
        public List<Entity> children = new List<Entity>();

        private bool owned = false; // Represents whether this Entity is owned by the machine running (could be client or server)

        public bool worldTransformDirty = false;
        public bool inverseWorldTransformDirty = false;

        List<TransformChangeListener> tcListeners = new List<TransformChangeListener>();
        public Entity(Vector2 collisionBox, Entity parent) {
            this.collisionBox = collisionBox;
            this.id = next_id++;
            this.parent = parent;
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

                if(owned) notifyTransformChange(); // Only notify of things we have jurisdiction over
            }
        }

        public Vector2 velocity;
        public float jumpAcceleration = 10.0f;
        public float maxVelocity = 50.0f;
        public bool inAir = false;

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

        public Entity(Vector2 collisionBox)
        {
            this.collisionBox = collisionBox;
            this.id = next_id++;
        }

        public void addTransformChangeListener(TransformChangeListener tcl)
        {
            tcListeners.Add(tcl);
        }

        public void notifyTransformChange()
        {
            foreach (TransformChangeListener tcl in tcListeners)
            {
                tcl.handleTransformChange(this);
            }
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

    }
}
