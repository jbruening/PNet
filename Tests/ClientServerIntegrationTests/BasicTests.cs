using System;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNet.Testing.Common;
using PNetS;

namespace ClientServerIntegrationTests
{
    [TestClass]
    public class BasicTests
    {

        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void TestCleanup()
        {
            ServerUtils.TeardownServer();
        }

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

        [TestMethod]
        public void RapidConnectionsTest()
        {
            const int ClientCount = 20;
            const int ClientConnectionTries = 5;
            const int ExpectedTotalConnections = ClientCount * ClientConnectionTries;

            ServerUtils.SetupDefaultServer();
            ServerUtils.StartServerOnNewThread();

            int cCount = 0;
            var cLock = new object();
            int dcCount = 0;
            var dcLock = new object();

            PNetServer.OnPlayerConnected += player => { lock (cLock) cCount++; };
            PNetServer.OnPlayerDisconnected += player => { lock (dcLock) dcCount++; };
            PNetServer.ApproveConnection += message =>
                                                {
                                                    Thread.Sleep(10);
                                                    message.SenderConnection.Approve();
                                                };

            var clients = new TestablePNet[ClientCount];
            for (int i = 0; i < clients.Length; i++)
            {
                clients[i] = new TestablePNet();
                clients[i].TestableHook.StartUpdateThread();
                int i1 = i;
                clients[i].OnFailedToConnect += s => Assert.Fail("{0} failed to connect: {1}", i1, s);
            }

            var connTasks = new List<Task>(clients.Length);
            Thread.Sleep(100); //wait for clients to start...
            for (int c = 0; c < clients.Length; c++)
            {
                var client = clients[c];
                int c1 = c;
                var task = new Task(() =>
                {
                    int connectCount = 0;
                    var config = TestablePNet.GetTestConnectionConfig();
                    client.OnFailedToConnect += s => Assert.Fail("{0} connect failed {1}", c1, s);
                    client.OnConnectedToServer += () =>
                    {
                        connectCount++;
                        Thread.Sleep(200);
                        client.Disconnect();
                    };
                    client.OnDisconnectedFromServer += () =>
                    {
                        if (connectCount < ClientConnectionTries)
                        {
                            client.Connect(config);
                        }
                    };
                    client.Connect(config);

                    while (connectCount < ClientConnectionTries)
                    {
                        Thread.Sleep(1);
                    }
                });
                connTasks.Add(task);
            }

            var aTasks = connTasks.ToArray();
            foreach (var task in aTasks)
            {
                task.Start();
            }

            Task.WaitAll(aTasks);

            Thread.Sleep(500);
            Assert.AreEqual(ExpectedTotalConnections, cCount);
            Assert.AreEqual(ExpectedTotalConnections, dcCount);
            var ap = PNetServer.AllPlayers();
            foreach (var player in ap)
            {
                Assert.IsNull(player);
            }
        }
    }
}
