using PNet.Testing.Common;
using PNetC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Debug = System.Diagnostics.Debug;

namespace UnitTestsPNetC
{
    
    
    /// <summary>
    ///This is a test class for NetworkViewManagerTest and is intended
    ///to contain all NetworkViewManagerTest Unit Tests
    ///</summary>
    [TestClass()]
    public class NetworkViewManagerTest
    {
        private TestablePNet _net;

        private TestContext testContextInstance;
        private NetworkViewManager _target;

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
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            
        }
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        [TestInitialize()]
        public void MyTestInitialize()
        {
            _net = new TestablePNet();
            _target = new NetworkViewManager(_net);
        }
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        /// <summary>
        ///A test for Create
        ///</summary>
        [TestMethod()]
        public void CreateTest()
        {
            var viewId = new NetworkViewId{guid =15};
            ushort ownerId = 14;
            
            _net.TestablePlayerID = 12;
            NetworkView actual = _target.Create(viewId, ownerId);
            
            
            //test regular creation with an empty pool
            Assert.AreEqual(viewId, actual.ViewID);
            Assert.AreEqual(ownerId, actual.OwnerId);
            Assert.IsFalse(actual.IsMine);
        }

        [TestMethod()]
        public void IsMineTest()
        {
            ushort playerID = 15;
            _net.TestablePlayerID = playerID;

            var actual = _target.Create(new NetworkViewId{guid =16}, playerID);
            
            Assert.IsTrue(actual.IsMine, "The networkview should be mine");

            actual = _target.Create(new NetworkViewId{guid = 16}, 17);

            Assert.IsFalse(actual.IsMine, "The networkview should not be mine");
        }

        [TestMethod()]
        public void RecycleTest()
        {
            var vid1 = new NetworkViewId{guid =15};
            var vid2 = new NetworkViewId{guid =16};
            var vid3 = new NetworkViewId{guid =17};

            ushort owner1 = 1;
            ushort owner2 = 2;
            ushort owner3 = 3;

            var first = _target.Create(vid1, owner1);
            var second = _target.Create(vid2, owner2);

            _target.RemoveView(first);
            first = null;

            var third = _target.Create(vid3, owner3);

            Assert.AreNotEqual(vid1, third.ViewID.guid);
            Assert.AreNotEqual(owner1, third.OwnerId);
        }
    }
}
