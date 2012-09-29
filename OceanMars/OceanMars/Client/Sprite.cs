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
        Entity e;

        Vector2 drawSize;

        Texture2D spriteStrip;

        int entityID;
        List<int> animationFrames;
        int frameTime;
        int currentFrame;
        int frameWidth;
        int frameHeight;
        Color color;

        bool active;
        bool looping;

        internal void draw(GameTime gameTime, SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                null,
                DepthStencilState.Default,
                RasterizerState.CullNone,
                null,
                e.getWorldTransform());



            spriteBatch.End();
        }

        public void setAnimationSpriteStrip(int frameWidth, int frameTime, String spriteStripName)
        {
            Texture2D chara = context.textureDict[spriteStripName];

            List<int> animationFrames = new List<int>(); // TODO: some way of loading animation
            for (int i = 0; i < chara.Width / frameWidth; i++)
            {
                animationFrames.Add(i);
            }

            initDrawable(chara, frameWidth, chara.Height, animationFrames, frameTime, Color.White, true);
            active = true;

            currentFrame = 0;
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
        }

        public void nextFrame(GameTime gameTime)
        {
            // Do not update the game if we are not active
            if (active == false)
                return;

            /*drawingPosition = worldPosition - context.viewPosition + new Vector2(1280 / 2, 720 / 2);

            // Update the elapsed time
            elapsedTime += (int)gameTime.ElapsedGameTime.TotalMilliseconds;

            // If the elapsed time is larger than the frame time
            // we need to switch frames
            if (elapsedTime > GetFrameTime())
            {
                // Move to the next frame
                currentFrame++;

                // If the currentFrame is equal to frameCount reset currentFrame to zero
                if (currentFrame == animationFrames.Count)
                {
                    currentFrame = 0;
                    // If we are not looping deactivate the animation
                    if (looping == false)
                        active = false;
                }

                //Console.WriteLine(this.spriteStrip.Name + ": " + this.GetFrameTime());

                // Reset the elapsed time to zero
                elapsedTime = 0;
            }

            int drawFrame = animationFrames[currentFrame];

            // Grab the correct frame in the image strip by multiplying the currentFrame index by the frame width
            sourceRect = new Rectangle(drawFrame * frameWidth, 0, frameWidth, frameHeight);

            // Grab the correct frame in the image strip by multiplying the currentFrame index by the frame width
            destinationRect = new Rectangle((int)drawingPosition.X - (int)(frameWidth * scale) / 2,
            (int)drawingPosition.Y - (int)(frameHeight * scale) / 2,
            (int)(frameWidth * scale),
            (int)(frameHeight * scale));*/
        }
    }
}
