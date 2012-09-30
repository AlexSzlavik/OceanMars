﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OceanMars.Client.Screens
{
    class LevelEditPauseScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor.
        /// </summary>
        public LevelEditPauseScreen()
            : base("Paused")
        {

            // Create our menu entries.
            MenuEntry resumeGameMenuEntry = new MenuEntry("Resume Game");
            MenuEntry optionsMenuEntry = new MenuEntry("Options");
            MenuEntry quitGameMenuEntry = new MenuEntry("Quit Game");
            
            // Hook up menu event handlers.
            optionsMenuEntry.Selected += OptionsMenuEntrySelected;
            quitGameMenuEntry.Selected += QuitGameMenuEntrySelected;
            resumeGameMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(resumeGameMenuEntry);
            MenuEntries.Add(optionsMenuEntry);
            MenuEntries.Add(quitGameMenuEntry);
            return;
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Resume playback when cancelling.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event arguments.</param>
        protected override void OnCancel(object sender, PlayerInputEventArgs e)
        {
            base.OnCancel(sender, e);
            return;
        }

        /// <summary>
        /// Resume playback when cancelling.
        /// </summary>
        /// <param name="sender">Object sender.</param>
        /// <param name="e">Event arguments.</param>
        protected override void OnCancel()
        {
            base.OnCancel();
            return;
        }

        /// <summary>
        /// Event handler for when the Quit Game menu entry is selected.
        /// </summary>
        void QuitGameMenuEntrySelected(object sender, PlayerInputEventArgs e)
        {
            const string message = "Quit to main menu?";
            MessageBoxScreen confirmQuitMessageBox = new MessageBoxScreen(message);
            confirmQuitMessageBox.Accepted += ConfirmQuitMessageBoxAccepted;
            ScreenManager.AddScreen(confirmQuitMessageBox);
            return;
        }

        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to quit" message box. This uses the loading screen to
        /// transition from the game back to the main menu screen.
        /// </summary>
        void ConfirmQuitMessageBoxAccepted(object sender, PlayerInputEventArgs e)
        {
            LoadingScreen.Load(ScreenManager, false, null, new BackgroundScreen(),
                                                           new MainMenuScreen());
            return;
        }

        /// <summary>
        /// Open the options menu from inside the game environment.
        /// </summary>
        /// <param name="sender">The object sender (menu object that was selected).</param>
        /// <param name="e">The player indices involved with picking the option.</param>
        void OptionsMenuEntrySelected(object sender, PlayerInputEventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen());
            return;
        }

        #endregion
    }
}