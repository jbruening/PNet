using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetU
{
    /// <summary>
    /// Configuration for the client
    /// </summary>
    public class ClientConfiguration
    {
        /// <summary>
        /// the ip or domain name of the server
        /// </summary>
        public readonly string Ip;

        /// <summary>
        /// the port to connect to on the server
        /// </summary>
        public readonly int Port;

        /// <summary>
        /// this should be unique per game, and should be the same on the client and server
        /// </summary>
        public readonly string AppIdentifier;

        /// <summary>
        /// the port to bind the game to. 0 will cause it to bind to the first available port
        /// </summary>
        public readonly int BindPort;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="port"> </param>
        /// <param name="bindPort"></param>
        /// <param name="appIdentifier"></param>
        /// <param name="ip"> </param>
        public ClientConfiguration(string ip, int port, int bindPort = 0, string appIdentifier = "PNet")
        {
            BindPort = bindPort;
            AppIdentifier = appIdentifier;
            Ip = ip;
            Port = port;
        }
    }
}
