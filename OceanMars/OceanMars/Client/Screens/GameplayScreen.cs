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
        bool stillJumping = false;
        bool stillHoldingJump = false;
        bool firstRelease = false;

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
                Vector2 movement = Vector2.Zero;
                float movementSpeed = context.avatar.inAir ? 0.25f : 2.5f;

                stillHoldingJump |= context.avatar.inAir;

                if (stillHoldingJump &&
                    (keyboardState.IsKeyUp(Keys.Space) && gamePadState.Buttons.A == ButtonState.Released))
                    if (firstRelease == false)
                    {
                        firstRelease = true;
                    }
                    else
                    {
                        stillHoldingJump = false;
                    }

                if (keyboardState.IsKeyDown(Keys.Left))
                    movement.X -= movementSpeed;

                if (keyboardState.IsKeyDown(Keys.Right))
                    movement.X += movementSpeed;

                //TODO: SLIDING VELOCITY CURRENTLY AFFECTS JUMP HEIGHT; IT SHOULDN'T

                //if (keyboardState.IsKeyDown(Keys.Up))
                //    movement.Y--;

                //if (keyboardState.IsKeyDown(Keys.Down))
                //    movement.Y++;

                if (keyboardState.IsKeyDown(Keys.Space) ||
                    gamePadState.Buttons.A == ButtonState.Pressed)
                {
                    //if (firstRelease == true)
                    {
                        if ((!context.avatar.inAir &&
                             !stillHoldingJump) ||
                            (context.avatar.inAir &&
                             stillJumping &&
                             stillHoldingJump &&
                             Math.Abs(context.avatar.velocity.Y) < context.avatar.maxVelocity))
                        {
                            context.avatar.velocity.Y -= context.avatar.jumpAcceleration;
                            context.avatar.inAir = true;
                            stillJumping = true;
                            stillHoldingJump = true;
                        }
                        else
                        {
                            stillJumping = false;
                        }
                    }
                }


                Vector2 thumbstick = gamePadState.ThumbSticks.Left;

                movement.X += thumbstick.X * movementSpeed;
                movement.Y -= thumbstick.Y * movementSpeed;

                //if (movement.Length() > 1)
                //    movement.Normalize();

                context.avatar.velocity += 1 * movement;
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
