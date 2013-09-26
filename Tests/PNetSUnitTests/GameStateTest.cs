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
            typeof(GameState).GetField("_createdThread").SetValue(null, Thread.CurrentThread);

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
    }
}
