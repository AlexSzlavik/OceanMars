#region File Description
//-----------------------------------------------------------------------------
// GameplayScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using System;
using System.Threading;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using OceanMars.Common;
using OceanMars.Client;
using OceanMars.Client.GameStateManager;
using OceanMars.Client.Screens;
using OceanMars.Common.NetCode;
#endregion

namespace SkyCrane.Screens
{
    /// <summary>
    /// This screen implements the actual game logic. It is just a
    /// placeholder to get the idea across: you'll probably want to
    /// put some more interesting gameplay in here!
    /// </summary>
    public class GameplayScreen : GameScreen
    {
        #region Fields

        // Our things
        GameClient game;
        ContentManager content;
        View context;
        private bool stillJumping = false;
        private bool stillHoldingJump = false;
        private bool firstRelease = false;
        private float GRAVITY = 2.0f;

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public GameplayScreen(GameClient gb)
        {
            game = gb;
            context = new View(gb.GameState, gb.getPlayerEntity());
        }

        private void createSprites(Entity root)
        {

            foreach (Entity e in root.children)
            {
                createSprites(e);
                if (e is DefaultLevel)
                {
                    Sprite s = new DefaultLevelSprite(context, (DefaultLevel)e);
                    context.sprites.Add(e.id, s);
                }
                else if (e is TestMan)
                {
                    Sprite s = new TestManSprite(context, (TestMan)e);
                    context.sprites.Add(e.id, s);
                }
                else if (e is TestWall)
                {
                    Sprite s = new TestWallSprite(context, (TestWall)e);
                    context.sprites.Add(e.id, s);
                }
            }
        }

        /// <summary>
        /// Load graphics content for the game.
        /// </summary>
        public override void LoadContent()
        {

            if (content == null)
            {
                content = new ContentManager(ScreenManager.Game.Services, "Content");
            }

            context.textureDict.Add("defaultlevel", content.Load<Texture2D>("Sprites/scenery"));
            context.textureDict.Add("whitesquare", content.Load<Texture2D>("Sprites/30x30whitesquare"));
            context.textureDict.Add("SonicWalking", content.Load<Texture2D>("Sprites/SonicWalking"));
            context.textureDict.Add("blacksquare", content.Load<Texture2D>("Sprites/1x1blacksquare"));
            context.textureDict.Add("localcoordplayer", content.Load<Texture2D>("Sprites/localcoordplayer"));

            // After loading content, instantiate sprites
            createSprites(game.GameState.root);
            

            return;
        }

        /// <summary>
        /// Unload graphics content used by the game.
        /// </summary>
        public override void UnloadContent()
        {
            content.Unload();
        }

        #endregion

        #region Update and Draw


        /// <summary>
        /// Updates the state of the game. This method checks the GameScreen.IsActive
        /// property, so the game will stop updating when the pause menu is active,
        /// or if you tab away to a different application.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            game.GameState.nextFrame();
        }


        /// <summary>
        /// Lets the game respond to player input. Unlike the Update method,
        /// this will only be called when the gameplay screen is active.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            if (input == null)
                throw new ArgumentNullException("input");

            KeyboardState keyboardState = input.currentKeyboardState;
            GamePadState gamePadState = input.currentGamePadState;

            // The game pauses either if the user presses the pause button, or if
            // they unplug the active gamepad. This requires us to keep track of
            // whether a gamepad was ever plugged in, because we don't want to pause
            // on PC if they are playing with a keyboard and have no gamepad at all!
            bool gamePadDisconnected = !gamePadState.IsConnected && input.gamePadWasConnected;

            if (input.IsPauseGame() || gamePadDisconnected)
            {
                //pauseSoundEffect.Play();
                ScreenManager.AddScreen(new PauseMenuScreen());
            }
            else
            {
                //if the game is not paused, handle user input

                #region Handle Jump Input

                Vector2 movement = Vector2.Zero;
                float movementAcceleration = context.avatar.movementAcceleration;
                if (context.avatar.groundState == Entity.GroundState.AIR)
                    movementAcceleration /= 10.0f;

                context.avatar.acceleration = Vector2.Zero;

                context.avatar.acceleration.Y = context.avatar.groundState == Entity.GroundState.WALL ? GRAVITY / 2.0f : GRAVITY;

                //stillHoldingJump makes sure that, once our character hits the ground, he doesn't jump
                //again until we let go of the jump button and press it again
                stillHoldingJump |= context.avatar.groundState == Entity.GroundState.AIR;

                if (stillHoldingJump &&
                    (keyboardState.IsKeyUp(Keys.Space) && gamePadState.Buttons.A == ButtonState.Released))
                    //don't interpret the first key press as actually pressed, so that coming in from
                    //the menu doesn't make us jump
                    //NOTE: Feels VERY hacky, is there a better way?
                    if (firstRelease == false)
                    {
                        firstRelease = true;
                    }
                    else
                    {
                        stillHoldingJump = false;
                    }

                if (keyboardState.IsKeyDown(Keys.Space) ||
                    gamePadState.Buttons.A == ButtonState.Pressed)
                {
                    //if (firstRelease == true)
                    {
                        if (((context.avatar.groundState == Entity.GroundState.GROUND ||
                              context.avatar.groundState == Entity.GroundState.WALL) &&
                             !stillHoldingJump) ||
                            (context.avatar.groundState == Entity.GroundState.AIR &&
                             stillJumping &&
                             Math.Abs(context.avatar.velocity.Y) < context.avatar.maxVelocity))
                        {
                            if (context.avatar.groundState == Entity.GroundState.WALL)
                                movementAcceleration *= -5;
                            context.avatar.acceleration.Y -= context.avatar.jumpAcceleration;
                            context.avatar.groundState = Entity.GroundState.AIR;
                            stillJumping = true;
                            stillHoldingJump = true;
                        }
                        else
                        {
                            //stillJumping makes sure we cannot jump infinitely
                            stillJumping = false;
                        }
                    }
                }

                #endregion

                #region Handle Left/Right Movement

                bool sliding = (keyboardState.IsKeyDown(Keys.LeftShift) ||
                                gamePadState.Buttons.B == ButtonState.Pressed);

                if (!sliding)
                {
                    if (keyboardState.IsKeyDown(Keys.Left))
                        context.avatar.acceleration.X = -movementAcceleration;

                    if (keyboardState.IsKeyDown(Keys.Right))
                        context.avatar.acceleration.X = movementAcceleration;

                    Vector2 thumbstick = gamePadState.ThumbSticks.Left;

                    if (thumbstick.X != 0)
                        context.avatar.acceleration.X = thumbstick.X * movementAcceleration;

                    if (keyboardState.IsKeyDown(Keys.LeftControl) ||
                        gamePadState.Buttons.RightShoulder == ButtonState.Pressed)
                        context.avatar.acceleration.X *= 2.0f;

                }

                context.avatar.velocity += context.avatar.acceleration;
                context.avatar.ignoreFriction = sliding;

                #endregion
            }
        }


        /// <summary>
        /// Draws the gameplay screen.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // This game has a blue background. Why? Because!
            ScreenManager.GraphicsDevice.Clear(ClearOptions.Target,
                                               Color.CornflowerBlue, 0, 0);

            // Our player and enemy are both actually just text strings.
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;

            context.draw(gameTime, spriteBatch);
        }


        #endregion
    }
}
