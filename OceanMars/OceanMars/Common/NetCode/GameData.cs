using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace OceanMars.Common.NetCode
{
    public class GameData : IMarshable
    {

         /// <summary>
        /// The type of game data being delivered.
        /// </summary>
        public enum GameDataType
        {

            /// <summary>
            /// Initial connection packet to retrieve a player number.
            /// </summary>
            Connect = 0,

            /// <summary>
            /// Select a particular character on the menu screen.
            /// </summary>
            SelectCharacter,

            /// <summary>
            /// Lock a particular character on the menu screen.
            /// </summary>
            LockCharacter,

            /// <summary>
            /// The host has started the game.
            /// </summary>
            GameStart

        }

        /// <summary>
        /// Details about a connection packet.
        /// </summary>
        public enum ConnectionDetails
        {

            /// <summary>
            /// A request for an id from the server or a response assigning an id.
            /// </summary>
            IdReqest = 0,

            /// <summary>
            /// Information about connected clients.
            /// </summary>
            Connected,

            /// <summary>
            /// Request for disconnect or information about unconnected clients.
            /// </summary>
            Disconnected,

            /// <summary>
            /// The player was dropped from the session.
            /// </summary>
            Dropped

        }

        /// <summary>
        /// Details about a lock packet.
        /// </summary>
        public enum LockCharacterDetails
        {
            /// <summary>
            /// Request to lock or information that a character is locked.
            /// </summary>
            Locked = 0,

            /// <summary>
            /// Request to unlock or information that a character is unlocked.
            /// </summary>
            Unlocked

        }

        /// <summary>
        /// The type associated with this packet.
        /// </summary>
        public GameDataType Type
        {
            get;
            set;
        }

        /// <summary>
        /// The id of the player that performed the action.
        /// </summary>
        public int PlayerId
        {
            get;
            set;
        }

        /// <summary>
        /// The extra detail associated with this event.
        /// </summary>
        public int EventDetail
        {
            get;
            set;
        }

        public ConnectionID ConnectionInfo
        {
            get;
            set;
        }

        /// <summary>
        /// Create a new piece of GameData from a byte array representation.
        /// </summary>
        /// <param name="byteArray">A byte array to create a GameData packet from.</param>
        public GameData(byte[] byteArray)
        {
            using (MemoryStream memoryStream = new MemoryStream(byteArray))
            {
                using (BinaryReader binaryReader = new BinaryReader(memoryStream))
                {
                    Type = (GameDataType)binaryReader.ReadByte();
                    PlayerId = (int)binaryReader.ReadByte();
                    EventDetail = (int)binaryReader.ReadByte();
                }
            }
            
            return;
        }

        /// <summary>
        /// Create a new GameData packet.
        /// </summary>
        /// <param name="gameDataType">The type associated with this packet.</param>
        /// <param name="playerId">The id of the player that performed the action.</param>
        /// <param name="eventDetail">The extra detail associated with this event.</param>
        public GameData(GameDataType gameDataType, int playerId = 0, int eventDetail = 0)
        {
            Type = gameDataType;
            PlayerId = playerId;
            EventDetail = eventDetail;
            return;
        }

        /// <summary>
        /// Get an array of bytes representing the packet.
        /// </summary>
        /// <returns>An array of bytes representing this packet state.</returns>
        public byte[] GetByteArray()
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
                {
                    binaryWriter.Write((byte)Type);
                    binaryWriter.Write((byte)PlayerId);
                    binaryWriter.Write((byte)EventDetail);
                    return memoryStream.ToArray();
                }
            }
        }

    }
}
