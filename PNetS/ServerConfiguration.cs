using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// configuration for the server
    /// </summary>
    public class ServerConfiguration
    {
        /// <summary>
        /// 
        /// </summary>
        public readonly int MaximumConnections;
        /// <summary>
        /// 
        /// </summary>
        public readonly int ListenPort;
        /// <summary>
        /// 
        /// </summary>
        public readonly int TickRate;
        /// <summary>
        /// this should be unique per game, and should be the same on the client and server
        /// </summary>
        public readonly string AppIdentifier;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maximumConnections"></param>
        /// <param name="listenPort"></param>
        /// <param name="tickRate"></param>
        /// <param name="appIdentifier"></param>
        public ServerConfiguration(int maximumConnections = 32, int listenPort = 14000, int tickRate = 66, string appIdentifier = "PNet")
        {
            MaximumConnections = maximumConnections;
            ListenPort = listenPort;
            TickRate = tickRate;
            AppIdentifier = appIdentifier;
        }
    }
}
