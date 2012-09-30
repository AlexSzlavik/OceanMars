using Microsoft.Xna.Framework;
using OceanMars.Client.GameStateManager;
using SkyCrane.Screens;


namespace OceanMars.Client.Screens
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    class MainMenuScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MainMenuScreen()
            : base("Main Menu")
        {
            // Create our menu entries.
            MenuEntry newGameMenuEntry = new MenuEntry("New Game");
            MenuEntry multiplayerMenuEntry = new MenuEntry("Multiplayer");
            MenuEntry levelEditEntry = new MenuEntry("Level Editor");
            MenuEntry optionsMenuEntry = new MenuEntry("Options");
            MenuEntry exitMenuEntry = new MenuEntry("Exit");

            // Hook up menu event handlers.
            newGameMenuEntry.Selected += NewGameMenuEntrySelected;
            multiplayerMenuEntry.Selected += MultiplayerMenuEntrySelected;
            levelEditEntry.Selected += LevelEditEntrySelected;
            optionsMenuEntry.Selected += OptionsMenuEntrySelected;
            exitMenuEntry.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(newGameMenuEntry);
            MenuEntries.Add(multiplayerMenuEntry);
            MenuEntries.Add(levelEditEntry);
            MenuEntries.Add(optionsMenuEntry);
            MenuEntries.Add(exitMenuEntry);
            return;
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void NewGameMenuEntrySelected(object sender, PlayerInputEventArgs e)
        {
            ScreenManager.AddScreen(new GameplayScreen());
            return;
        }

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void MultiplayerMenuEntrySelected(object sender, PlayerInputEventArgs e)
        {
            ScreenManager.AddScreen(new MultiplayerMenuScreen());
            return;
        }

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void LevelEditEntrySelected(object sender, PlayerInputEventArgs e)
        {
            ScreenManager.AddScreen(new LevelEditScreen());
            return;
        }

        /// <summary>
        /// Event handler for when the Options menu entry is selected.
        /// </summary>
        void OptionsMenuEntrySelected(object sender, PlayerInputEventArgs e)
        {
            ScreenManager.AddScreen(new OptionsMenuScreen());
            return;
        }


        /// <summary>
        /// When the user cancels the main menu, ask if they want to exit the sample.
        /// </summary>
        protected override void OnCancel()
        {
            const string message = "Return to desktop?";
            MessageBoxScreen confirmExitMessageBox = new MessageBoxScreen(message);
            confirmExitMessageBox.Accepted += ConfirmExitMessageBoxAccepted;
            ScreenManager.AddScreen(confirmExitMessageBox);
            return;
        }


        /// <summary>
        /// Event handler for when the user selects ok on the "are you sure
        /// you want to exit" message box.
        /// </summary>
        void ConfirmExitMessageBoxAccepted(object sender, PlayerInputEventArgs e)
        {
            ScreenManager.Game.Exit();
            return;
        }


        #endregion
    }
}
