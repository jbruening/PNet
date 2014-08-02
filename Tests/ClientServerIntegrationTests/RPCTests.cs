using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Lidgren.Network;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNet;
using PNet.Testing.Common;
using PNetS;
using SlimMath;

namespace ClientServerIntegrationTests
{
    /// <summary>
    /// Summary description for RoomTests
    /// </summary>
    [TestClass]
    public class RPCTests
    {
        private Room _testRoom;
        private TestablePNet _client;
        private Player _player;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region setup/cleanup
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            ServerUtils.SetupDefaultServer();
            ServerUtils.StartServerOnNewThread();
        }
        //
        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            ServerUtils.TeardownServer();
        }
        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            _client = new TestablePNet();
            _client.TestableHook.StartUpdateThread();

            _client.OnRoomChange += s => _client.FinishedRoomChange();
            PNetServer.OnPlayerConnected += player => player.ChangeRoom(_testRoom);

            _testRoom = Room.CreateRoom("test room");

            _client.Connect(TestablePNet.GetTestConnectionConfig());
            Thread.Sleep(250);
            _player = PNetServer.AllPlayers().First(f => f != null && f != Player.Server);
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            _client.Disconnect();
            while(_client.Status != NetConnectionStatus.Disconnected)
            {Thread.Sleep(10);}
            _client.TestableHook.StopUpdateThread();
        }
        //
        #endregion

        [TestMethod]
        public void TestInstantiateRPC()
        {
            const string expectedInstantiate = "joi129348ujij&&I(";
            const string expectedRPCValue = "(**Ylkafjsiu&^&*";
            const byte rpcID = 36;

            var gobj = _testRoom.NetworkInstantiate(expectedInstantiate, Vector3.Zero, Quaternion.Identity, _player);
            var netView = gobj.GetComponent<NetworkView>();

            Thread.Sleep(50);

            Assert.IsTrue(_client.TestableHook.Instantiates.ContainsKey(expectedInstantiate), 
                "Client did not have an object named {0} instantiated", expectedInstantiate);

            var rpcWasCalled = false;

            _client.TestableHook.Instantiates[expectedInstantiate].SubscribeToRPC(rpcID, message =>
            {
                StringSerializer.Instance.OnDeserialize(message);
                Assert.AreEqual(expectedRPCValue, StringSerializer.Instance.Value, 
                    "Client received {0} but expected {1}", StringSerializer.Instance.Value, expectedRPCValue);
                rpcWasCalled = true;
            });

            //of note: the rpc method that takes a few serializers, instead of the params[], should be called
            netView.RPC(rpcID, RPCMode.All, StringSerializer.Instance.Update(expectedRPCValue));

            Thread.Sleep(100);
            Assert.IsTrue(rpcWasCalled, "Message was not received within 100ms");
        }

        [TestMethod]
        public void StaticASerializableTest()
        {
            var rpcWasCalled = false;

            string rpcStr = "foobar";

            _client.ProcessRPC += (id, msg) =>
            {
                if (id == 1)
                {
                    var str = StringSerializer.Deserialize(msg);

                    Assert.AreEqual(rpcStr, str);
                    rpcWasCalled = true;
                }
            };

            _player.RPC(1, StringSerializer.Instance.Update(rpcStr));

            Thread.Sleep(50);
            Assert.IsTrue(rpcWasCalled);
        }

        [TestMethod]
        public void RoomRPCTest()
        {
            var rpcWasCalled = false;

            const string expected = "roomRpcTest";
            const int msgId = 7;

            _client.ProcessRPC += (b, message) =>
            {
                if (b == msgId)
                {
                    var str = StringSerializer.Deserialize(message);
                    Assert.AreEqual(expected, str);
                    rpcWasCalled = true;
                }
            };

            _testRoom.RPC(msgId, RPCMode.All, StringSerializer.Instance.Update(expected));

            Thread.Sleep(50);
            Assert.IsTrue(rpcWasCalled);
        }
    }
}
