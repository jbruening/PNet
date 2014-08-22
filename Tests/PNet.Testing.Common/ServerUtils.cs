using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lidgren.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNetS;

namespace PNet.Testing.Common
{
    public static class ServerUtils
    {
        private static Thread _serverThread;

        public static void StartServerOnNewThread()
        {
            _serverThread = new Thread(() => PNetServer.Start());
            _serverThread.Start();
        }

        public static void SetupDefaultServer()
        {
            PNetS.PNetServer.InitializeServer(GetDefaultServerConfig());

            Debug.logger = new DefaultConsoleLogger();
        }

        public static ServerConfiguration GetDefaultServerConfig()
        {
            return new ServerConfiguration(appIdentifier: "PNetTest");
        }

        public static void TeardownServer()
        {
            PNetServer.Disconnect();
            while(PNetServer.PeerStatus != NetPeerStatus.NotRunning)
            {Thread.Sleep(10);}
            PNetServer.Shutdown();
            Thread.Sleep(100);
        }
    }

    /// <summary>
    /// Outputs info and warning to console, but fails on Error
    /// </summary>
    public class TestLogger : ILogger
    {
        public void Info(string info, params object[] args)
        {
            Console.WriteLine(info, args);
        }

        public void Warning(string info, params object[] args)
        {
            Console.WriteLine(info, args);
        }

        public void Error(string info, params object[] args)
        {
            Assert.Fail(info, args);
        }
    }
}
