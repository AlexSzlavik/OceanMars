using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using OceanMars.Client.GameStateManager;
using OceanMars.Common;


namespace OceanMars.Client.Screens
{
    /// <summary>
    /// This is a game component that implements IUpdateable.
    /// </summary>
    public class LevelEditScreen : GameScreen
    {
        #region Fields
        // Our things
        ContentManager content;
        LevelEditorState state = new LevelEditorState(); // TODO: need to instantiate this from server connection in some way, and player
        View context;
        bool pointSetDown = false;
        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public LevelEditScreen()
        {
            context = new View(state, state.player);
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
            context.textureDict.Add("whitecrosshair", content.Load<Texture2D>("Sprites/21x21crosshair"));
            context.textureDict.Add("blacksquare", content.Load<Texture2D>("Sprites/1x1blacksquare"));

            // After loading content, instantiate sprites
            foreach (int id in state.entities.Keys)
            {
                Entity e = state.entities[id];
                if (e is DefaultLevel)
                {
                    Sprite s = new DefaultLevelSprite(context, (DefaultLevel)e);
                    context.sprites.Add(id, s);
                }
                else if (e is EditorMan)
                {
                    Sprite s = new EditorManSprite(context, (EditorMan)e);
                    context.sprites.Add(id, s);
                }
                else if (e is TestWall)
                {
                    Sprite s = new TestWallSprite(context, (TestWall)e);
                    context.sprites.Add(id, s);
                }
            }

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
            state.nextFrame();
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

                if (keyboardState.IsKeyDown(Keys.Left))
                    movement.X--;

                if (keyboardState.IsKeyDown(Keys.Right))
                    movement.X++;

                if (keyboardState.IsKeyDown(Keys.Up))
                    movement.Y--;

                if (keyboardState.IsKeyDown(Keys.Down))
                    movement.Y++;

                if (keyboardState.IsKeyDown(Keys.Space) ||
                    gamePadState.Buttons.A == ButtonState.Pressed)
                {
                    if (pointSetDown == false)
                    {
                        Entity e = ((EditorMan)context.avatar).pointSet();
                        if (e != null)
                        {
                            if (e is TestWall)
                            {
                                Sprite s = new TestWallSprite(context, (TestWall)e);
                                context.sprites.Add(e.id, s);
                            }
                        }
                        pointSetDown = true;
                    }
                }

                if (keyboardState.IsKeyUp(Keys.Space) &&
                    gamePadState.Buttons.A == ButtonState.Released)
                {
                    pointSetDown = false;
                }

                if (keyboardState.IsKeyDown(Keys.B) || 
                    (gamePadState.Buttons.B == ButtonState.Pressed))
                {
                    ((FreeEntity)context.avatar).setBoostOn();
                }

                if (keyboardState.IsKeyUp(Keys.B) && 
                    (gamePadState.Buttons.B == ButtonState.Released))
                {
                    ((FreeEntity)context.avatar).setBoostOff();
                }


                Vector2 thumbstick = gamePadState.ThumbSticks.Left;

                movement.X += thumbstick.X;
                movement.Y -= thumbstick.Y;

                if (movement.Length() > 1)
                    movement.Normalize();

                context.avatar.velocity += movement;
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