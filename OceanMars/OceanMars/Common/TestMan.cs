using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common.NetCode;

namespace OceanMars.Common
{
    public class TestMan : EllipseEntity
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
                if (value != facingBack) signalStateChange();
                facingBack = value;
            }
        }

        public enum MovingState
        {
            MOVING,
            NOTMOVING
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
                if (value != movingBack) signalStateChange();
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
        

        public TestMan(Entity parent, bool owner = false, int id = -1)
            : base(new Vector2(30, 30), parent, owner, id)
        {

        }
    }
    
}
