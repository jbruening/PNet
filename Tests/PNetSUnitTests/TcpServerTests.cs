using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNet.Testing.Common;
using PNetS;
using PNetS.Server;

namespace PNetSUnitTests
{
    /// <summary>
    /// Summary description for TcpServerTests
    /// </summary>
    [TestClass]
    public class TcpServerTests
    {
        public TcpServerTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        private TestContext testContextInstance;
        private TcpServer _server;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void ClassInitialize(TestContext testContext)
        {
            Debug.logger = new TestLogger();
        }
        
        // Use ClassCleanup to run code after all tests in a class have run
        // [ClassCleanup()]
        // public static void MyClassCleanup() { }
        
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void TestInitialize()
        {
            _server = new TcpServer();
            _server.Start(14000);
            Thread.Sleep(100);
        }
        
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            _server.Stop();
            Thread.Sleep(1100);
        }
        
        #endregion

        TcpClient GetConnectedClient()
        {
            var client = new TcpClient(new IPEndPoint(IPAddress.Any, 0));
            client.Connect(IPAddress.Loopback, 14000);
            Assert.IsTrue(client.Connected);
            return client;
        }

        [TestMethod]
        public void TestClientToServerMessage()
        {
            var client = GetConnectedClient();

            var sent = SendRandomMessage(client, 5000);
            
            Thread.Sleep(100);
            TcpServer.NetworkMessage state;
            if (_server.ReceiveQueue.TryDequeue(out state))
            {
                Assert.AreEqual(state.MessageSize, sent.Length);
                var msg = new byte[state.MessageSize];
                Buffer.BlockCopy(state.Message, 0, msg, 0, state.MessageSize);
                CollectionAssert.AreEqual(sent, msg);
            }
            else
            {
                Assert.Fail("No messages in queue");
            }
        }

        /// <summary>
        /// send a message from sendingClient to the server
        /// </summary>
        /// <param name="sendingClient"></param>
        /// <param name="messageSize"></param>
        /// <returns>the message bytes that were sent (not including the header)</returns>
        byte[] SendRandomMessage(TcpClient sendingClient, int messageSize)
        {
            var stream = sendingClient.GetStream();
            var rand = new Random();
            var msgBytes = new byte[messageSize];
            var msgSize = BitConverter.GetBytes(msgBytes.Length);
            rand.NextBytes(msgBytes);

            var bytes = new byte[msgSize.Length + msgBytes.Length];
            Buffer.BlockCopy(msgSize, 0, bytes, 0, msgSize.Length);
            Buffer.BlockCopy(msgBytes, 0, bytes, msgSize.Length, msgBytes.Length);
            stream.Write(bytes, 0, bytes.Length);
            stream.Flush();
            return msgBytes;
        }

        [TestMethod]
        public void DisconnectTest()
        {
            var client = GetConnectedClient();

            SendRandomMessage(client, 30);
            client.Client.Disconnect(true);

            Thread.Sleep(50);
            Assert.IsFalse(client.Connected);
        }

        public void MultipleClients()
        {
            
        }
    }
}
