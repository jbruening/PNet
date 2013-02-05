using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/// <summary>
/// Identifier for a NetworkView
/// </summary>
public class NetworkViewId
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
}