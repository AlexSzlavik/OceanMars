using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public class State : TransformChangeListener
    {
        public enum PHASE { READY_FOR_CHANGES, PROCESSING_FRAME, FINISHED_FRAME }

        private static Vector2 GRAVITY = new Vector2(0, 2.0f);

        private List<IStatePhaseListener> spListeners = new List<IStatePhaseListener>();
        private List<TransformChangeListener> scListeners = new List<TransformChangeListener>();

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
                foreach (IStatePhaseListener spl in spListeners)
                {
                    spl.handleStatePhaseChange(phaseBack);
                }
            }
        }

        public void registerEntity(Entity e)
        {
            entities.Add(e.id, e);
            e.addTransformChangeListener(this);
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
                        ellipseEntity.velocity += GRAVITY;
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
            phase = PHASE.FINISHED_FRAME;
        }

        public void handleTransformChange(Entity e)
        {
            foreach (TransformChangeListener scl in scListeners)
            {
                scl.handleTransformChange(e);
            }
        }

        public void addTransformChangeListener(TransformChangeListener tcl)
        {
            scListeners.Add(tcl);
        }

        public void addStatePhaseListener(IStatePhaseListener spl)
        {
            spListeners.Add(spl);
        }
    }
}
