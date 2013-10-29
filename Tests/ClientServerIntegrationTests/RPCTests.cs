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
            var player = PNetServer.AllPlayers().First(f => f != null && f != Player.Server);
            var rand = new Random();
            var expectedInstantiate = (rand.NextDouble()*50).ToString();
            
            byte expectedRPCValue = (byte) rand.Next(byte.MinValue, byte.MaxValue);
            if (expectedRPCValue == default(byte))
                expectedRPCValue += 1; //don't allow defaults
            
            byte rpcID = (byte) rand.Next(byte.MinValue, byte.MaxValue);
            if (rpcID == default(byte))
                rpcID += 1;

            var gobj = _testRoom.NetworkInstantiate(expectedInstantiate, Vector3.Zero, Quaternion.Identity, player);
            var netView = gobj.GetComponent<NetworkView>();

            Thread.Sleep(50);

            Assert.IsTrue(_client.TestableHook.Instantiates.ContainsKey(expectedInstantiate));

            var rpcWasCalled = false;

            _client.TestableHook.Instantiates[expectedInstantiate].SubscribeToRPC(rpcID, message =>
            {
                ByteSerializer.Instance.OnDeserialize(message);
                Assert.AreEqual(expectedRPCValue, ByteSerializer.Instance.Value);
                rpcWasCalled = true;
            });

            //of note: the rpc method that takes a few serializers, instead of the params[], should be called
            netView.RPC(rpcID, RPCMode.All, ByteSerializer.Instance.Update(expectedRPCValue));

            Thread.Sleep(50);
            Assert.IsTrue(rpcWasCalled);
        }
    }
}
