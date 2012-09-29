using System.IO;
using System.Net;

namespace OceanMars.Common.NetCode
{

    /// <summary>
    /// Class that represents a generic UDP packet as sent and received by our applications.
    /// </summary>
    public class NetworkPacket
    {

        /// <summary>
        /// Different potential types of packets.
        /// </summary>
        public enum PacketType { HANDSHAKE = 0, COMMAND = 1, STATECHANGE = 2, SYNC = 3, PING = 4, MENUSTATECHANGE = 5 };

        /// <summary>
        /// The type of packet 
        /// </summary>
        public PacketType Type
        {
            get;
            protected set;
        }

        /// <summary>
        /// The endpoint destination for this particular packet.
        /// </summary>
        public IPEndPoint Destination
        {
            get;
            private set;
        }

        /// <summary>
        /// The data contents of the packet.
        /// </summary>
        public byte[] DataArray
        {
            get;
            private set;
        }

        /// <summary>
        /// A memory stream representation of the contents of the packet.
        /// </summary>
        private MemoryStream DataStream
        {
            get;
            set;
        }

        /// <summary>
        /// Create a new packet.
        /// </summary>
        /// <param name="type">The type of the packet.</param>
        /// <param name="destination">The destination for the packet.</param>
        public NetworkPacket(PacketType type, IPEndPoint destination)
        {
            Type = type;
            Destination = destination;
            DataArray = new byte[200];
            DataStream = new MemoryStream();
            DataStream.WriteByte((byte)type);
            return;
        }

        /// <summary>
        /// Add content to the packet.
        /// </summary>
        /// <param name="content">The content to pass along to the packet data stream.</param>
        protected void AddContent(byte[] content)
        {
            DataStream.Write(content, 0, content.Length);
            return;
        }

        /// <summary>
        /// Push the data in the contained data stream into the data array.
        /// </summary>
        protected void FinalizeData()
        {
            DataArray = DataStream.ToArray();
            return;
        }

    }

    /// <summary>
    /// A packet that represents a handshake between servers and clients.
    /// </summary>
    public class HandshakePacket : NetworkPacket
    {

        /// <summary>
        /// Create a new HandshakePacket.
        /// </summary>
        /// <param name="destination">The endpoint destination for this particular packet.</param>
        public HandshakePacket(IPEndPoint destination) : base(PacketType.HANDSHAKE, destination)
        {
            FinalizeData();
            return;
        }

    }

    /// <summary>
    /// A packet that represents a game command.
    /// </summary>
    public class CommandPacket : NetworkPacket
    {

        /// <summary>
        /// Create a new CommandPacket.
        /// </summary>
        /// <param name="destination">The endpoint destination for this particular packet.</param>
        /// <param name="command">The command to send over the network.</param>
        public CommandPacket(IPEndPoint destination, Command command) : base(PacketType.COMMAND, destination)
        {
            AddContent(command.GetByteArray());
            FinalizeData();
            return;
        }

    }

    /// <summary>
    /// A packet that represents a change in state.
    /// </summary>
    public class StateChangePacket : NetworkPacket
    {

        /// <summary>
        /// Create a new StateChangePacket.
        /// </summary>
        /// <param name="destination">The endpoint destination for this particular packet.</param>
        /// <param name="stateChange">The state change to send over the network.</param>
        public StateChangePacket(IPEndPoint destination, StateChange stateChange) : base(PacketType.STATECHANGE, destination)
        {
            AddContent(stateChange.GetByteArray());
            FinalizeData();
            return;
        }

    }

    /// <summary>
    /// A packet that represents a change in state inside a menu.
    /// </summary>
    public class MenuStateChangePacket : NetworkPacket
    {

        /// <summary>
        /// Create a new MenuStateChangePacket.
        /// </summary>
        /// <param name="destination">The endpoint destination for this particular packet.</param>
        /// <param name="menuState">The change in state to menus that is passed over the network.</param>
        public MenuStateChangePacket(IPEndPoint destination, MenuState menuState) : base(PacketType.MENUSTATECHANGE, destination)
        {
            AddContent(menuState.GetByteArray());
            FinalizeData();
            return;
        }

    }

    /// <summary>
    /// A packet used to send ping messages.
    /// </summary>
    public class PingPacket : NetworkPacket
    {

        /// <summary>
        /// Create a new PingPacket.
        /// </summary>
        /// <param name="destination">The endpoint destination for this particular packet.</param>
        public PingPacket(IPEndPoint destination) : base(PacketType.PING, destination)
        {
            FinalizeData();
            return;
        }

    }

    /// <summary>
    /// A packet used to synchronize the clients and servers.
    /// </summary>
    public class SyncPacket : NetworkPacket
    {

        /// <summary>
        /// Create a new SyncPacket.
        /// </summary>
        /// <param name="destination">The endpoint destination for this particular packet.</param>
        public SyncPacket(IPEndPoint destination) : base(PacketType.SYNC, destination)
        {
            this.Type = PacketType.SYNC;
            this.FinalizeData();
        }

    }

}
