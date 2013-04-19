using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using PNet;

/// <summary>
/// Identifier for a NetworkView
/// </summary>
public class NetworkViewId : INetSerializable
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
    /// doesn't do anything for integrity
    /// </summary>
    /// <param name="message">message to read from</param>
    [Obsolete("Use NetworkView.Find(NetIncomingMessage, out NetworkView)")]
    public void OnDeserialize(NetIncomingMessage message){}

    internal static ushort Deserialize(NetIncomingMessage message)
    {
        return message.ReadUInt16();
    }

    public int AllocSize { get { return 2; } }
}