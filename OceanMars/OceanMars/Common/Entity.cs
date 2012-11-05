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
        public Vector2 velocity;
        public Vector2 acceleration;
        public bool ignoreFriction = false; //better place to put this? maybe a physics entity needed?
        public float movementAcceleration = 5.0f;
        public float jumpAcceleration = 10.0f;
        public float maxVelocity = 35.0f;
        public enum GroundState
        {
            GROUND,
            AIR,
            WALLSLIDE_LEFT,
            WALLSLIDE_RIGHT
            //WATER
            //LAVA
        }

        public bool onWall()
        {
            return groundState == Entity.GroundState.WALLSLIDE_LEFT || groundState == Entity.GroundState.WALLSLIDE_RIGHT;
        }

        public GroundState lastGroundState
        {
            private set;
            get;
        }
        private GroundState groundBack;
        public GroundState groundState
        {
            get
            {
                return groundBack;
            }
            set
            {
                if (value != groundBack)
                {
                    groundBack = value;
                    OnEntityStateChange();
                }
            }
        }

        public bool stateChanged = false;

        public static int next_id = 0;

        public int id;
        public Vector2 collisionBox;
        public Entity parent = null;
        public List<Entity> children = new List<Entity>();

        public bool owned = false; // Represents whether this Entity is owned by the machine running (could be client or server)

        public bool worldTransformDirty = false;
        public bool inverseWorldTransformDirty = false;

        public delegate void TransformChange(Entity e);
        private List<TransformChange> TransformChangeListeners = new List<TransformChange>();

        public delegate void EntityStateChange(Entity e);
        private List<EntityStateChange> EntityStateChangeListeners = new List<EntityStateChange>();

        public Entity(Vector2 collisionBox, Entity parent, bool owner = false, int id = -1) {
            this.collisionBox = collisionBox;
            this.id = id < 0 ? next_id++ : id;
            this.parent = parent;
            this.owned = owner;
            groundState = GroundState.GROUND;

            if (parent != null)
            {
                parent.addChild(this);
            }
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

                OnTransformChange();
            }
        }

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

        public void registerTransformChangeListener(TransformChange tcl)
        {
            TransformChangeListeners.Add(tcl);
        }

        public void registerEntityStateChangeListener(EntityStateChange escl)
        {
            EntityStateChangeListeners.Add(escl);
        }

        public void OnTransformChange()
        {
            if (!owned) return;
            foreach (TransformChange tcl in TransformChangeListeners)
            {
                tcl.Invoke(this);
            }
        }

        protected void OnEntityStateChange()
        {
            stateChanged = true;
            lastGroundState = groundBack;

            if (!owned) return;

            if (this is MobileEntity)
            {
                MobileEntity m = (MobileEntity)this;
            }

            foreach (EntityStateChange esc in EntityStateChangeListeners)
            {
                esc.Invoke(this);
            }
        }
        
        public void addChild(Entity child)
        {
            // Add the calling entity to the child list
            children.Add(child);
            // Call up the tree in order to register the child with the state
            registerChild(child);
        }

        public virtual void registerChild(Entity child)
        {
            parent.registerChild(child);
        }

    }
}
