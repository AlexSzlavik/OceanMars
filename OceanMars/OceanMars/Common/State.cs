using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class State
    {
        public enum PHASE { READY_FOR_CHANGES, PROCESSING_FRAME, FINISHED_FRAME }

        #region Listener Delegate Lists

        public delegate void EntityAdd(Entity e);
        private List<EntityAdd> EntityAddListeners = new List<EntityAdd>();

        public delegate void EntityRemove(Entity e);
        private List<EntityRemove> EntityRemoveListeners = new List<EntityRemove>();

        public delegate void TransformChange(Entity e);
        private List<TransformChange> TransformChangeListeners = new List<TransformChange>();

        public delegate void StatePhaseChange(PHASE p);
        private List<StatePhaseChange> StatePhaseChangeListeners = new List<StatePhaseChange>();

        #endregion

        public World root;
        public Dictionary<int, Entity> entities = new Dictionary<int,Entity>();

        private PHASE phaseBack = PHASE.READY_FOR_CHANGES;
        public PHASE phase
        {
            get
            {
                return phaseBack;
            }

            private set {
                phaseBack = value;
                foreach (StatePhaseChange spl in StatePhaseChangeListeners)
                {
                    spl.Invoke(phaseBack);
                }
            }
        }

        public void registerEntity(Entity e)
        {
            
            entities.Add(e.id, e);
            e.registerTransformChangeListener(OnTransformChange);

            // Notify people that we've added an entity
            for (int i = 0; i < EntityAddListeners.Count; i++)
            {
                EntityAddListeners[i].Invoke(e);
            }
        }

        public void OnTransformChange(Entity e)
        {
            foreach (TransformChange tcl in TransformChangeListeners)
            {
                tcl.Invoke(e);
            }
        }

        public State()
        {
            root = new World(this);
        }

        public void nextFrame()
        {
            phase = PHASE.PROCESSING_FRAME;
            foreach (int id in entities.Keys)
            {
                Entity child = entities[id];
                if (child.owned)
                {
                    if (child is EllipseEntity)
                    {
                        EllipseEntity ellipseEntity = (EllipseEntity)child;
                        ellipseEntity.testCollision(entities.Values.ToList()); // TODO: using ToList may be inefficient
                    }
                    else if (child is FreeEntity)
                    {
                        FreeEntity freeEntity = (FreeEntity)child;
                        freeEntity.testCollision(entities.Values.ToList()); // TODO: using ToList may be inefficient
                    }
                    // Do not do collisions for SliderEntities

                }
            }
            // NB: Phase changes trigger actions, do not remove the "un-necessary" phase change
            phase = PHASE.FINISHED_FRAME;
            phase = PHASE.READY_FOR_CHANGES;
        }

        #region Delegate Registration Functions

        public void registerTransformChange(TransformChange tcl)
        {
            TransformChangeListeners.Add(tcl);
        }

        public void registerStatePhaseChange(StatePhaseChange spl)
        {
            StatePhaseChangeListeners.Add(spl);
        }

        public void registerEntityAdd(EntityAdd e)
        {
            EntityAddListeners.Add(e);
        }

        public void registerEntityRemove(EntityRemove e)
        {
            EntityRemoveListeners.Add(e);
        }

        #endregion
    }
}
