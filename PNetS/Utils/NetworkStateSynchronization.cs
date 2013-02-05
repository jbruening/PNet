using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// Type of synchronization used for stream
    /// </summary>
    public enum NetworkStateSynchronization
    {
        /// <summary>
        /// no streaming
        /// </summary>
        Off = 0,
        /// <summary>
        /// reliably, ordered, but only when there's a chnage
        /// </summary>
        ReliableDeltaCompressed = 1,
        /// <summary>
        /// unreliable, unordered, and all the time
        /// </summary>
        Unreliable = 2,
    }
}
