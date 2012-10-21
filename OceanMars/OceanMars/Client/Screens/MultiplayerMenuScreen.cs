#region File Description
//-----------------------------------------------------------------------------
// MultiplayerMenuScreen.cs
//
// Microsoft XNA Community Game Platform
// Copyright (C) Microsoft Corporation. All rights reserved.
//-----------------------------------------------------------------------------
#endregion

#region Using Statements
using Microsoft.Xna.Framework;
using OceanMars.Common.NetCode;
using System.Threading;
using OceanMars.Screens;
#endregion

namespace OceanMars.Client.Screens
{
    /// <summary>
    /// The main menu screen is the first thing displayed when the game starts up.
    /// </summary>
    class MultiplayerMenuScreen : MenuScreen
    {
        #region Initialization

        /// <summary>
        /// Constructor fills in the menu contents.
        /// </summary>
        public MultiplayerMenuScreen()
            : base("Multiplayer")
        {
            // Create our menu entries.
            MenuEntry hostGameMenuEntry = new MenuEntry("Host Game");
            MenuEntry joinGameMenuEntry = new MenuEntry("Join Game");
            MenuEntry back = new MenuEntry("Back");

            // Hook up menu event handlers.
            hostGameMenuEntry.Selected += HostGameMenuEntrySelected;
            joinGameMenuEntry.Selected += JoinGameMenuEntrySelected;
            back.Selected += OnCancel;

            // Add entries to the menu.
            MenuEntries.Add(hostGameMenuEntry);
            MenuEntries.Add(joinGameMenuEntry);
            MenuEntries.Add(back);
            return;
        }

        #endregion

        #region Handle Input

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void HostGameMenuEntrySelected(object sender, PlayerInputEventArgs e)
        {
            GameServer gs = new GameServer(9999);
            GameClient gc = new GameClient();
            gc.ConnectToGame("127.0.0.1", 9999);
            ScreenManager.AddScreen(new LobbyScreen(gc, gs));

            return;
        }

        /// <summary>
        /// Event handler for when the Play Game menu entry is selected.
        /// </summary>
        void JoinGameMenuEntrySelected(object sender, PlayerInputEventArgs e)
        {
            ScreenManager.AddScreen(new HostSettingsMenuScreen());
            return;
        }

        #endregion
    }
}
