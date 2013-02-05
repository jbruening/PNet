using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// The identifier for a network view
    /// </summary>
    public class NetworkViewId
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
    }
}
