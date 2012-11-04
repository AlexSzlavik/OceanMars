using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using OceanMars.Common;

namespace OceanMars.Client
{
    class TestManSprite : Sprite
    {
        TestMan e
        {
            get
            {
                return (TestMan)base.e;
            }

            set
            {
                base.e = value;
            }
        }

        public TestManSprite(View context, TestMan tm) :
            base(context,  tm, new Vector2(36, 36), 36, 10, "SonicWalking") {
        }

        internal override void draw(GameTime gameTime, Microsoft.Xna.Framework.Graphics.SpriteBatch spriteBatch)
        {

            // Check if the entity phase changed
            if (e.stateChanged)
            {
                if (e.groundState == Entity.GroundState.GROUND)
                {
                    if (e.moving == TestMan.MovingState.MOVING)
                    {
                        if (e.facing == TestMan.FacingState.LEFT)
                        {
                            setAnimationSpriteStrip(36, 20, "SonicWalking", false);
                        }
                        else if (e.facing == TestMan.FacingState.RIGHT)
                        {
                            setAnimationSpriteStrip(36, 20, "SonicWalking", true);
                        }
                    }
                    else if (e.moving == TestMan.MovingState.NOTMOVING)
                    {
                        if (e.facing == TestMan.FacingState.LEFT)
                        {
                            setAnimationSpriteStrip(36, 0, "SonicWalking", false);
                        }
                        else if (e.facing == TestMan.FacingState.RIGHT)
                        {
                            setAnimationSpriteStrip(36, 0, "SonicWalking", true);
                        }
                    }
                }
                else if (e.groundState == Entity.GroundState.AIR)
                {
                    if (e.facing == TestMan.FacingState.LEFT)
                    {
                        setAnimationSpriteStrip(36, 20, "SonicJumping", false, false, (e.groundState != e.lastGroundState));
                    }
                    else if (e.facing == TestMan.FacingState.RIGHT)
                    {
                        setAnimationSpriteStrip(36, 20, "SonicJumping", true, false, (e.groundState != e.lastGroundState));
                    }
                }

                // This is a hack for handling the state, but should probably be reset on frame in the Entity instead of here
                e.stateChanged = false;
            }

            base.draw(gameTime, spriteBatch);
        }
    }
}
