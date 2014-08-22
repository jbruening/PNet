using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

[assembly:InternalsVisibleTo("PNetSUnitTests")]
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
        public int TcpListenPort { get { return ListenPort + 1; } }
        /// <summary>
        /// 
        /// </summary>
        public readonly int TickRate;
        /// <summary>
        /// this should be unique per game, and should be the same on the client and server
        /// </summary>
        public readonly string AppIdentifier;
        /// <summary>
        /// size of outgoing buffer. 131071 bytes
        /// </summary>
        public readonly int SendBuffer;
        /// <summary>
        /// size of incoming buffer. 131071 bytes
        /// </summary>
        public readonly int ReceiveBuffer;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="maximumConnections"></param>
        /// <param name="listenPort"></param>
        /// <param name="tickRate"></param>
        /// <param name="appIdentifier"></param>
        /// <param name="sendBuffer"></param>
        /// <param name="receiveBuffer"></param>
        public ServerConfiguration(
            int maximumConnections = 32, 
            int listenPort = 14000, 
            int tickRate = 66, 
            string appIdentifier = "PNet", 
            int sendBuffer = 131071, 
            int receiveBuffer = 131071)
        {
            MaximumConnections = maximumConnections;
            ListenPort = listenPort;
            TickRate = tickRate;
            AppIdentifier = appIdentifier;
            SendBuffer = sendBuffer;
            ReceiveBuffer = receiveBuffer;
        }
    }
}
