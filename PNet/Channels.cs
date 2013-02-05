using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNet
{
    /// <summary>
    /// Channels for various rpcs
    /// </summary>
    public class Channels
    {
        /// <summary>
        /// unreliable streaming
        /// </summary>
        public const int UNRELIABLE_STREAM = 0;
        /// <summary>
        /// reliable streaming
        /// </summary>
        public const int RELIABLE_STREAM = 1;
        /// <summary>
        /// static rpcs (rooms)
        /// </summary>
        public const int STATIC_RPC = 3;
        /// <summary>
        /// utility rpcs
        /// </summary>
        public const int STATIC_UTILS = 5;
        /// <summary>
        /// beginning RPCMode channels
        /// </summary>
        public const int BEGIN_RPCMODES = 10;
        /// <summary>
        /// channel for owner rcpmode
        /// </summary>
        public const int OWNER_RPC = 17;
        /// <summary>
        /// channel for NetworkedSceneObject rpcs
        /// </summary>
        public const int OBJECT_RPC = 19;
        /// <summary>
        /// channel for synchronized fields
        /// </summary>
        public const int SYNCHED_FIELD = 21;
    }
}
