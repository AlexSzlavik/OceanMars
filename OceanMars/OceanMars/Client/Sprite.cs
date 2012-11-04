using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OceanMars.Common;

namespace OceanMars.Client
{
    public class Sprite
    {
        View context;
        protected Entity e;

        public Vector2 drawSize;

        Texture2D spriteStrip;

        int entityID;
        List<int> animationFrames;
        int frameTime;
        int elapsedTime;
        int currentFrame;
        int frameWidth;
        int frameHeight;
        Color color;
        Rectangle sourceRect;
        bool flipHorizontal = false;

        bool active;
        bool looping;

        public Sprite(View context, Entity e, Vector2 drawSize, int frameWidth, int frameTime, string spriteStripName)
        {
            this.context = context;
            this.e = e;
            this.entityID = e.id;
            this.drawSize = drawSize;

            setAnimationSpriteStrip(frameWidth, frameTime, spriteStripName);
        }

        internal virtual void draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            // Advance frame
            nextFrame(gameTime);

            Matrix temp =
                e.worldTransform *
                context.avatar.inverseWorldTransform *
                context.centreTransform;
            
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                null,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                temp);

            Rectangle destRest = new Rectangle((int)(-drawSize.X/2), (int)(-drawSize.Y/2), (int)drawSize.X, (int)drawSize.Y);
            //Rectangle destRest = new Rectangle(0, 0, (int)drawSize.X, (int)drawSize.Y);
            SpriteEffects se = flipHorizontal ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            spriteBatch.Draw(spriteStrip, destRest, sourceRect, Color.White, 0, Vector2.Zero, se, 0);

            /*spriteBatch.Draw(
                spriteStrip,
                Vector2.Zero,
                sourceRect,
                Color.White);*/

            /*spriteBatch.Draw(
                spriteStrip,
                - drawSize/2,
                sourceRect,
                Color.White,
                0.0f,
                drawSize / 2,
                1.0f,
                SpriteEffects.None,
                1);*/

            spriteBatch.End();

            // Draw children
            foreach (Entity child in e.children) {
                Sprite childSprite = context.sprites[child.id];
                childSprite.draw(gameTime, spriteBatch);
            }
        }

        public void setAnimationSpriteStrip(int frameWidth, int frameTime, String spriteStripName, bool flipHorizontal = false, bool looping = true, bool reset = true)
        {
            Texture2D chara = context.textureDict[spriteStripName];
            this.flipHorizontal = flipHorizontal;

            List<int> animationFrames = new List<int>(); // TODO: some way of loading animation
            for (int i = 0; i < chara.Width / frameWidth; i++)
            {
                animationFrames.Add(i);
            }

            if (reset)
                currentFrame = 0;

            initDrawable(chara, frameWidth, chara.Height, animationFrames, frameTime, Color.White, looping);
            if (frameTime != 0)
            {
                active = true;
            }
            else
            {
                active = false;
            }
        }

        public void initDrawable(Texture2D texture,
            int frameWidth, int frameHeight, List<int> animationFrames,
            int frametime, Color color, bool looping)
        {
            this.spriteStrip = texture;
            this.frameWidth = frameWidth;
            this.frameHeight = frameHeight;
            this.animationFrames = animationFrames;
            this.frameTime = frametime;
            this.color = color;
            this.looping = looping;

            sourceRect = new Rectangle(0 * frameWidth, 0, frameWidth, frameHeight);
        }

        public void nextFrame(GameTime gameTime)
        {
            // Do not update the game if we are not active
            if (active == false)
                return;

            // Update the elapsed time
            elapsedTime += (int)gameTime.ElapsedGameTime.TotalMilliseconds;

            // If the elapsed time is larger than the frame time
            // we need to switch frames
            if (elapsedTime > frameTime)
            {
                // Move to the next frame
                currentFrame++;

                // If the currentFrame is equal to frameCount reset currentFrame to zero
                if (currentFrame == animationFrames.Count)
                {
                    // If we are not looping deactivate the animation
                    if (looping == false)
                    {
                        active = false;
                        --currentFrame; //Very minor hack
                    }
                    else
                    {
                        currentFrame = 0;
                    }
                }

                // Reset the elapsed time to zero
                elapsedTime = 0;
            }

            int drawFrame = animationFrames[currentFrame];

            // Grab the correct frame in the image strip by multiplying the currentFrame index by the frame width
            sourceRect = new Rectangle(drawFrame * frameWidth, 0, frameWidth, frameHeight);
        }
    }
}
