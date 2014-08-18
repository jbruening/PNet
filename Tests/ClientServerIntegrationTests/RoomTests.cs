using System;
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
    /// <summary>
    /// Summary description for RoomTests
    /// </summary>
    [TestClass]
    public class RoomTests
    {
        private Room _testRoom;
        private Room _testRoom2;
        private TestablePNet _client;

        public RoomTests()
        {
            //
            // TODO: Add constructor logic here
            //
        }

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext { get; set; }

        #region Additional test attributes
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

            _testRoom = Room.CreateRoom("test room");
            _testRoom2 = Room.CreateRoom("test room 2");
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
        public void TestRoomJoining()
        {
            _client.OnRoomChange += s => _client.FinishedRoomChange();
            PNetServer.OnPlayerConnected += OnPlayerConnected;

            

            _client.Connect(TestablePNet.GetTestConnectionConfig());

            Thread.Sleep(250);
            
            Assert.AreEqual(NetConnectionStatus.Connected, _client.Status, "Client did not connect in 250 ms");

            Thread.Sleep(100);

            var tPlayer = PNetServer.AllPlayers()[1];

            Assert.AreSame(_testRoom, tPlayer.CurrentRoom, "Client did not change to the room within 100 ms");

            GameState.InvokeIfRequired(() =>
            {
                tPlayer.ChangeRoom(_testRoom2); 
            });

            Thread.Sleep(100);

            Assert.AreSame(_testRoom2, tPlayer.CurrentRoom, "Client did not switch to room 2 within 100 ms");

            GameState.InvokeIfRequired(() =>
            {
                tPlayer.ChangeRoom(_testRoom);
            });

            Thread.Sleep(100);

            Assert.AreSame(_testRoom, tPlayer.CurrentRoom, "Client did not change back to room within 100 ms");
        }

        private void OnPlayerConnected(Player player)
        {
            player.ChangeRoom(_testRoom);
        }
    }
}
