using System.Collections;
using System.Reflection;
using System.Threading;
using PNet.Testing.Common;
using PNetS;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PNetSUnitTests
{
    
    
    /// <summary>
    ///This is a test class for GameStateTest and is intended
    ///to contain all GameStateTest Unit Tests
    ///</summary>
    [TestClass()]
    public class GameStateTest
    {


        private TestContext testContextInstance;

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
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        [TestMethod()]
        public void InvokeIfRequiredCatchTest()
        {
            var stateType = typeof (GameState);
            var threadField = stateType.GetField("_createdThread", BindingFlags.Static | BindingFlags.NonPublic);
            threadField.SetValue(null, Thread.CurrentThread);

            bool didCatchFail = false;
            try
            {
                GameState.InvokeIfRequired(delegate { throw new Exception("InvokeIfRequiredCatchTest"); });
            }
            catch (Exception e)
            {
                if (e.Message == "InvokeIfRequiredCatchTest")
                    didCatchFail = true;
                else
                    throw;
            }

            Assert.IsTrue(didCatchFail, "Should have caught the failure");
        }

        [TestMethod()]
        public void CoroutineTest()
        {
            var gobj = new GameObject();
            var test = gobj.AddComponent<TestCoroutine>();

            test.StartCoroutine(test.DoThing());
            //one extra Run is required as it's assumed that this would be the same frame that the coroutine would be started in.
            test.RunCoroutines();
            Assert.AreEqual(test.OuterCount, 1);
            Assert.AreEqual(test.InnerCount, 0);
            test.RunCoroutines();
            Assert.AreEqual(test.OuterCount, 1);
            Assert.AreEqual(test.InnerCount, 1);
            test.RunCoroutines();
            Assert.AreEqual(test.OuterCount, 1);
            Assert.AreEqual(test.InnerCount, 2);
            test.RunCoroutines();
            Assert.AreEqual(test.OuterCount, 2);
            Assert.AreEqual(test.InnerCount, 2);

            test.StartCoroutine(test.MoreEnumerator());
            test.RunCoroutines();
            Assert.AreEqual(test.MoreEnumCount, 0);
            test.RunCoroutines();
            Assert.AreEqual(test.MoreEnumCount, 1);
            test.RunCoroutines();
            Assert.AreEqual(test.MoreEnumCount, 2);
            test.RunCoroutines();
            Assert.AreEqual(test.MoreEnumCount, 3);
            test.RunCoroutines();
            Assert.AreEqual(test.MoreEnumCount, 4);
            test.RunCoroutines();
            Assert.AreEqual(test.MoreEnumCount, 5);
            test.RunCoroutines();
            Assert.AreEqual(test.MoreEnumCount, 5);
        }

        class TestCoroutine : Component
        {
            public int OuterCount;
            public int InnerCount;
            public int MoreEnumCount;

            public IEnumerator DoThing()
            {
                OuterCount++;
                yield return null;
                yield return StartCoroutine(EmbeddedThing());
                OuterCount++;
            }

            IEnumerator EmbeddedThing()
            {
                InnerCount++;
                yield return null;
                InnerCount++;
            }

            public IEnumerator MoreEnumerator()
            {
                yield return null;
                MoreEnumCount++;
                if (MoreEnumCount < 5)
                    StartCoroutine(MoreEnumerator());
            }

            public bool HasWaitedForFrames { get; private set; }
            public IEnumerator FrameWaiter()
            {
                yield return new WaitForFrames(3);
                HasWaitedForFrames = true;
            }
        }

        [TestMethod]
        public void TestWaitFrames()
        {
            var gobj = new GameObject();
            var test = gobj.AddComponent<TestCoroutine>();

            test.StartCoroutine(test.FrameWaiter());
            Assert.IsFalse(test.HasWaitedForFrames);
            //one extra Run is required as it's assumed that this would be the same frame that the coroutine would be started in.
            test.RunCoroutines();
            Assert.IsFalse(test.HasWaitedForFrames);
            
            //now these are real frames.
            test.RunCoroutines();
            Assert.IsFalse(test.HasWaitedForFrames);
            test.RunCoroutines();
            Assert.IsFalse(test.HasWaitedForFrames);
            test.RunCoroutines();
            Assert.IsTrue(test.HasWaitedForFrames);
        }
    }
}
