using PNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Lidgren.Network;

namespace PNet.Testing.Common
{
    
    
    /// <summary>
    ///This is a test class for SerializableTest and is intended
    ///to contain all SerializableTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SerializableTest
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

        [TestMethod]
        public void DifferentSerializerInstanceValuesTest()
        {
            var v1 = StringSerializer.Instance;
            var v2 = IntSerializer.Instance;

            Assert.AreNotSame(v1, v2);
        }

        [TestMethod]
        public void InstanceDifferentFromConstructedTest()
        {
            Assert.AreNotSame(StringSerializer.Instance, new StringSerializer());
        }

        /// <summary>
        /// Expects not to throw an error
        /// </summary>
        [TestMethod]
        public void NonDefinedConstructorInheritableTest()
        {
            var test = new ByteArraySerializer();
        }
    }
}
