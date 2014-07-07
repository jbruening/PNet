using Lidgren.Network;
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
        /// Send to everyone but the caller(from a client)/owner(from server)
        /// </summary>
        Others = 1,
        /// <summary>
        /// Send to everyone
        /// </summary>
        All = 2,
        /// <summary>
        /// Send to everyone but the caller(from a client)/owner(from server), buffered
        /// </summary>
        OthersBuffered = 5,
        /// <summary>
        /// Send to everyone buffered
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

        /// <summary>
        /// send to everyone but the owner, in an unordered fashion. CURRENTLY IS ORDERED FOR NETWORKVIEWS, DUE TO LIDGREN ISSUES
        /// </summary>
        OthersUnordered = 11,
        /// <summary>
        /// send to everyone, in an unordered fashion. CURRENTLY IS ORDERED FOR NETWORKVIEWS, DUE TO LIDGREN ISSUES
        /// </summary>
        AllUnordered = 12,
    }

    internal static class RPCModeExtensions
    {
        public static NetDeliveryMethod GetDeliveryMethod(this RPCMode mode)
        {
            return NetDeliveryMethod.ReliableOrdered;
        }
    }
}