using System;
using Lidgren.Network;
using PNet;

namespace PNetC
{
    /// <summary>
    /// Identifier for a NetworkView
    /// </summary>
    public struct NetworkViewId : INetSerializable
    {
        /// <summary>
        /// Whether or not I own the object
        /// </summary>
        public bool IsMine { get; internal set; }

        /// <summary>
        /// network id
        /// </summary>
        public ushort guid { get; internal set; }

        /// <summary>
        /// Network ID of nothing
        /// </summary>
        public static NetworkViewId Zero 
        {
            get
            {
                return new NetworkViewId() { guid = 0, IsMine = false };
            }
        }

        /// <summary>
        /// write to the message
        /// </summary>
        /// <param name="message">message to write to</param>
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(guid);
        }

        /// <summary>
        /// Be careful! It is not recommended to be directly serializing networkviewids.
        /// </summary>
        /// <param name="message">message to read from</param>
        public void OnDeserialize(NetIncomingMessage message)
        {
            guid = message.ReadUInt16();
        }

        /// <summary>
        /// Deserialize a networkviewid from the networkmessage
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        internal static NetworkViewId Deserialize(NetIncomingMessage message)
        {
            var id = new NetworkViewId();
            id.OnDeserialize(message);
            return id;
        }

        /// <summary>
        /// size when serializing to stream
        /// </summary>
        public int AllocSize { get { return 2; } }
    }
}