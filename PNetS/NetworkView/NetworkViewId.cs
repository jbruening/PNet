using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using PNet;

namespace PNetS
{
    /// <summary>
    /// The identifier for a network view
    /// </summary>
    public class NetworkViewId : INetSerializable
    {
        /// <summary>
        /// whether or not the server is the owner of the view
        /// </summary>
        public bool IsMine { get; internal set; }
        /// <summary>
        /// network identifier
        /// </summary>
        public ushort guid { get; internal set; }

        /// <summary>
        /// id of 0, ie, no id
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
        /// doesn't do anything for integrity
        /// </summary>
        /// <param name="message"></param>
        [Obsolete("Use NetworkView.Find(NetIncomingMessage, out NetworkView)")]
        public void OnDeserialize(NetIncomingMessage message){}

        internal static ushort Deserialize(NetIncomingMessage message)
        {
            return message.ReadUInt16();
        }

        /// <summary>
        /// size to allocate for bytes in the message
        /// </summary>
        public int AllocSize { get { return 2; } }
    }
}
