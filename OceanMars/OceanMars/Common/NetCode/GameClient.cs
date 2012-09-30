using System;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Abstraction of a game client program that rests on top of the network stack.
    /// </summary>
    public class GameClient : GameBase
    {

        /// <summary>
        /// The client lobby associated with this game client.
        /// </summary>
        public new LobbyClient Lobby
        {
            get
            {
                return (LobbyClient)base.Lobby;
            }
            private set
            {
                base.Lobby = value;
            }
        }

        /// <summary>
        /// Reference to the underlying network associated with this game client.
        /// </summary>
        public new NetworkClient Network
        {
            get
            {
                return (NetworkClient)base.Network;
            }
            private set
            {
                base.Network = value;
            }
        }

        /// <summary>
        /// Create a new GameClient.
        /// </summary>
        /// <param name="port">The port to create the GameServer on.</param>
        public GameClient() : base(new NetworkClient())
        {
            Lobby = new LobbyClient(this); // Give initial control to the lobby
            Network.RegisterGameDataUpdater(Lobby.UpdateLobbyState);

            // Once we're done with the lobby, the connections and players will be handed off to the game and the GameDataUpdater re-registered to GameClient.UpdateGameState

            return;
        }

        /// <summary>
        /// Register a new player with the game client.
        /// </summary>
        /// <param name="player">The player to register with the game client.</param>
        /// <returns>Returns the ID of the player to register.</returns>
        public override int RegisterPlayer(Player player)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Unregister a player from the game client.
        /// </summary>
        /// <param name="player">The player to remove from the game client.</param>
        public override void UnregisterPlayer(Player player)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Update the state of the game based on received game data.
        /// </summary>
        /// <param name="gameData">The game data to use to update the game.</param>
        protected override void UpdateGameState(GameData gameData)
        {
            throw new NotImplementedException();
        }

    }

}

