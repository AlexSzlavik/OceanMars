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


namespace OceanMars.Client.Screens
{
    /// <summary>
    /// Base class for screens that contain a menu of options. The user can
    /// move up and down to select an entry, or cancel to back out of the screen.
    /// </summary>
    public abstract class MenuScreen : GameScreen
    {
        #region Fields

        List<MenuEntry> menuEntries = new List<MenuEntry>();
        int selectedEntry = 0;
        string menuTitle;
        bool typingInput;
        bool graphicalSelect;

        // Sound effects used by menus in general
        protected SoundEffect menuScrollSoundEffect;
        protected SoundEffect menuSelectSoundEffect;
        protected SoundEffect menuCancelSoundEffect;

        #endregion

        #region Properties

        /// <summary>
        /// Whether or not the screen is currently capturing typed input.
        /// </summary>
        public bool TypingInput
        {
            get { return typingInput; }
            protected set { typingInput = value; }
        }

        /// <summary>
        /// Gets the list of menu entries, so derived classes can add
        /// or change the menu contents.
        /// </summary>
        protected IList<MenuEntry> MenuEntries
        {
            get { return menuEntries; }
        }

        /// <summary>
        /// Whether or not the current menu is a graphical selection menu.
        /// </summary>
        protected bool GraphicalSelect
        {
            get { return graphicalSelect; }
        }

        /// <summary>
        /// The title of the menu.
        /// </summary>
        protected string MenuTitle
        {
            get { return menuTitle; }
            set { menuTitle = value; }
        }

        /// <summary>
        /// The item currently selected in the menu.
        /// </summary>
        protected int SelectedEntry
        {
            get { return selectedEntry; }
            set { selectedEntry = value; }
        }

        #endregion

        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public MenuScreen(string menuTitle, bool graphicalSelect = false)
        {
            this.menuTitle = menuTitle;
            this.graphicalSelect = graphicalSelect;
            TransitionOnTime = TimeSpan.FromSeconds(0.5);
            TransitionOffTime = TimeSpan.FromSeconds(0.5);
            return;
        }

        /// <summary>
        /// Loads graphics content for this screen. This uses the shared ContentManager
        /// provided by the Game class, so the content will remain loaded forever.
        /// Whenever a subsequent MessageBoxScreen tries to load this same content,
        /// it will just get back another reference to the already loaded data.
        /// </summary>
        public override void LoadContent()
        {
            ContentManager content = ScreenManager.Game.Content;
            menuScrollSoundEffect = content.Load<SoundEffect>("SoundFX/menu_scroll");
            menuSelectSoundEffect = content.Load<SoundEffect>("SoundFX/menu_select");
            menuCancelSoundEffect = content.Load<SoundEffect>("SoundFX/menu_cancel");
            return;
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Responds to user input, changing the selected entry and accepting
        /// or cancelling the menu.
        /// </summary>
        public override void HandleInput(InputState input)
        {
            // Variables that will be set during input checking
            int toggleDirection;

            if (typingInput) // The user is typing input
            {
                bool inputAccepted = input.IsMenuSelect();
                bool inputCancelled = input.IsMenuCancel();
                bool inputBackspace = input.IsBackspace();
                String keysTyped = input.TypeableInput();

                if (inputAccepted || inputCancelled)
                {
                    typingInput = false;
                }
                if (inputAccepted || inputCancelled || inputBackspace || keysTyped != String.Empty)
                {
                    OnTyped(selectedEntry, inputAccepted, inputCancelled, inputBackspace, keysTyped);
                }
            }
            else // The user is performing normal menu input
            {
                // Move to the previous menu entry?
                if (input.IsMenuUp())
                {
                    if (!graphicalSelect)
                    {
                        menuScrollSoundEffect.Play();
                    }
                    do
                    {
                        selectedEntry--;
                        if (selectedEntry < 0)
                        {
                            selectedEntry = menuEntries.Count - 1;
                        }
                    } while (!menuEntries[selectedEntry].Enabled);
                }

                // Move to the next menu entry?
                if (input.IsMenuDown())
                {
                    if (!graphicalSelect)
                    {
                        menuScrollSoundEffect.Play();
                    }
                    do
                    {
                        selectedEntry++;
                        if (selectedEntry >= menuEntries.Count)
                        {
                            selectedEntry = 0;
                        }
                    } while (!menuEntries[selectedEntry].Enabled);
                }

                // Accept or cancel the menu?
                if (menuEntries[selectedEntry].Toggleable && input.IsMenuToggle(out toggleDirection))
                {
                    if (!graphicalSelect)
                    {
                        menuSelectSoundEffect.Play();
                    }
                    OnSelectEntry(selectedEntry, input.IsMenuSelect(), false, toggleDirection);
                }
                else if (input.IsMenuSelect())
                {
                    if (!graphicalSelect)
                    {
                        menuSelectSoundEffect.Play();
                    }
                    OnSelectEntry(selectedEntry, true, false, 0);
                }
                else if (input.IsMenuCancel())
                {
                    menuCancelSoundEffect.Play();
                    OnCancel();
                }
            }
            return;
        }

        /// <summary>
        /// Handler for when the user has chosen a menu entry.
        /// </summary>
        protected virtual void OnSelectEntry(int entryIndex, bool menuAccepted, bool menuCancelled, int toggleDirection)
        {
            if (menuEntries[entryIndex].Enabled)
            {
                menuEntries[entryIndex].OnSelectEntry(menuAccepted, menuCancelled, toggleDirection);
            }
            return;
        }

        /// <summary>
        /// Handler for when the user has cancelled the menu.
        /// </summary>
        protected virtual void OnCancel()
        {
            ExitScreen();
            return;
        }


        /// <summary>
        /// Helper overload makes it easy to use OnCancel as a MenuEntry event handler.
        /// </summary>
        protected virtual void OnCancel(object sender, PlayerInputEventArgs e)
        {
            OnCancel();
            return;
        }

        /// <summary>
        /// Handler for when the user types data into a text-accepting menu entry.
        /// </summary>
        protected virtual void OnTyped(int entryIndex, bool typingAccepted, bool typingCancelled, bool typingBackspace, String keysTyped)
        {
            menuEntries[entryIndex].OnInputTyped(typingAccepted, typingCancelled, typingBackspace, keysTyped);
            return;
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// Allows the screen the chance to position the menu entries. By default
        /// all menu entries are lined up in a vertical list, centered on the screen.
        /// </summary>
        protected virtual void UpdateMenuEntryLocations()
        {
            if (!graphicalSelect)
            {
                // Make the menu slide into place during transitions, using a
                // power curve to make things look more interesting (this makes
                // the movement slow down as it nears the end).
                float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

                // start at Y = 175; each X value is generated per entry
                Vector2 position = new Vector2(0f, 175f);

                // update each menu entry's location in turn
                for (int i = 0; i < menuEntries.Count; i++)
                {
                    MenuEntry menuEntry = menuEntries[i];

                    // each entry is to be centered horizontally
                    position.X = ScreenManager.GraphicsDevice.Viewport.Width / 2 - menuEntry.GetWidth(this) / 2;

                    if (ScreenState == ScreenState.TransitionOn)
                    {
                        position.X -= transitionOffset * 256;
                    }
                    else
                    {
                        position.X += transitionOffset * 512;
                    }

                    // set the entry's position
                    menuEntry.Position = position;

                    // move down for the next entry the size of this entry
                    position.Y += menuEntry.GetHeight(this);
                }
            }
            return;
        }


        /// <summary>
        /// Updates the menu.
        /// </summary>
        public override void Update(GameTime gameTime, bool otherScreenHasFocus,
                                                       bool coveredByOtherScreen)
        {
            base.Update(gameTime, otherScreenHasFocus, coveredByOtherScreen);

            // Update each nested MenuEntry object.
            for (int i = 0; i < menuEntries.Count; i++)
            {
                bool isSelected = IsActive && (i == selectedEntry);

                menuEntries[i].Update(this, isSelected, gameTime);
            }
            return;
        }

        /// <summary>
        /// Draws the menu.
        /// </summary>
        public override void Draw(GameTime gameTime)
        {
            // make sure our entries are in the right place before we draw them
            UpdateMenuEntryLocations();

            GraphicsDevice graphics = ScreenManager.GraphicsDevice;
            SpriteBatch spriteBatch = ScreenManager.SpriteBatch;
            SpriteFont font = ScreenManager.Font;

            spriteBatch.Begin();

            // Draw each menu entry in turn.
            if (!graphicalSelect)
            {
                for (int i = 0; i < menuEntries.Count; i++)
                {
                    MenuEntry menuEntry = menuEntries[i];
                    bool isSelected = IsActive && (i == selectedEntry);
                    menuEntry.Draw(this, isSelected, gameTime);
                }
            }

            // Make the menu slide into place during transitions, using a
            // power curve to make things look more interesting (this makes
            // the movement slow down as it nears the end).
            float transitionOffset = (float)Math.Pow(TransitionPosition, 2);

            // Draw the menu title centered on the screen
            Vector2 titlePosition = new Vector2(graphics.Viewport.Width / 2, 80);
            Vector2 titleOrigin = font.MeasureString(menuTitle) / 2;
            Color titleColor = new Color(192, 192, 192) * TransitionAlpha;
            float titleScale = 1.25f;
            titlePosition.Y -= transitionOffset * 100;
            spriteBatch.DrawString(font, menuTitle, titlePosition, titleColor, 0,
                                   titleOrigin, titleScale, SpriteEffects.None, 0);
            spriteBatch.End();
            return;
        }


        #endregion
    }
}
