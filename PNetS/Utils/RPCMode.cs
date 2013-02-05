using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// Ways of sending messages
    /// </summary>
    public enum RPCMode : byte
    {
        /// <summary>
        /// send to the server (not used)
        /// </summary>
        Server = 0,
        /// <summary>
        /// Send to everyone but the caller
        /// </summary>
        Others = 1,
        /// <summary>
        /// Send to everyone
        /// </summary>
        All = 2,
        /// <summary>
        /// Send to everyone but the caller, buffered
        /// </summary>
        OthersBuffered = 5,
        /// <summary>
        /// Send to everyon buffered
        /// </summary>
        AllBuffered = 6,
        /// <summary>
        /// send to the owner of the networkview
        /// </summary>
        Owner = 7,

        /// <summary>
        /// send to no one?
        /// </summary>
        None = 10,
    }
}