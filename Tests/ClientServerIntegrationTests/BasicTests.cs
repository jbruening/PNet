using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lidgren.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNet.Testing.Common;
using PNetS;

namespace ClientServerIntegrationTests
{
    [TestClass]
    public class BasicTests
    {        
        /// <summary>
        /// Test that the client connects and disconnects in 1 second
        /// </summary>
        [TestMethod]
        public void TestConnecting()
        {
            //setup a default server
            ServerUtils.SetupDefaultServer();
            ServerUtils.StartServerOnNewThread();

            var client = new TestablePNet();
            client.TestableHook.StartUpdateThread();
            client.OnFailedToConnect += s => Assert.Fail("Failed to connect: {0}", s);

            //the client should connect and disconnect quickly
            client.Connect(TestablePNet.GetTestConnectionConfig());
            Thread.Sleep(200);
            Assert.AreEqual(client.Status, NetConnectionStatus.Connected, "The client took longer than 200 ms to connect");
            
            client.Disconnect();
            Thread.Sleep(200);
            Assert.AreEqual(client.Status, NetConnectionStatus.Disconnected, "The client took longer than 200 ms to disconnect");

            //some cleanup
            ServerUtils.TeardownServer();
        }
    }
}
