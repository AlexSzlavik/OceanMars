using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace OceanMars.Common
{
    public abstract class MobileEntity : EllipseEntity
    {
        public enum FacingState
        {
            LEFT,
            RIGHT
        }

        private FacingState facingBack;
        public FacingState facing
        {
            get
            {
                return facingBack;
            }
            set
            {
                if (value != facingBack) OnEntityStateChange();
                facingBack = value;
            }
        }

        public enum MovingState
        {
            MOVING,
            NOTMOVING,
            SLIDING //TODO: should happen when the user is not inputting movement, but is moving anyway
        }

        private MovingState movingBack;
        public MovingState moving
        {
            get
            {
                return movingBack;
            }
            set
            {
                if (value != movingBack) OnEntityStateChange();
                movingBack = value;
            }
        }

        //TODO: new keyword??
        public Vector2 velocity
        {
            get
            {
                return base.velocity;
            }

            set
            {
                // hack: This needs an epsilon at least, and probably should consider more than x direction!
                // hack: this should probably be based on player input
                if (Math.Abs(velocity.X) < 0.01)
                {
                    moving = MovingState.NOTMOVING;
                }
                else
                {
                    moving = MovingState.MOVING;
                }

                base.velocity = value;
            }
        }

        public MobileEntity(Vector2 size, Entity parent, bool owner, int id)
            : base(size, parent, owner, id)
        {

        }
    }
}
