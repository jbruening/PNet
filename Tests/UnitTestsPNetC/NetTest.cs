using PNetC;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace UnitTestsPNetC
{
    
    
    /// <summary>
    ///This is a test class for NetTest and is intended
    ///to contain all NetTest Unit Tests
    ///</summary>
    [TestClass()]
    public class NetTest
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


        /// <summary>
        /// Test that a single run of Update doesn't throw an error without being set up.
        ///</summary>
        [TestMethod()]
        [DeploymentItem("PNetC.dll")]
        public void UpdateDoesNotErrorTest()
        {
            var pnet = new PNetC.Net(new PNet.Testing.Common.TestEngineHook());
            var param0 = new PrivateObject(pnet);
            var target = new Net_Accessor(param0);
            target.Update();
        }
    }
}
