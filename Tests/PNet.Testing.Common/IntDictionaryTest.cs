using PNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PNet.Testing.Common
{
    
    
    /// <summary>
    ///This is a test class for IntDictionaryTest and is intended
    ///to contain all IntDictionaryTest Unit Tests
    ///</summary>
    [TestClass()]
    public class IntDictionaryTest
    {
        private TestContext testContextInstance;
        private IntDictionary<MockObject> _target;

        class MockObject
        {
            public int Id;
        }

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
        [TestInitialize()]
        public void MyTestInitialize()
        {
            _target = new IntDictionary<object>();
        }
        //
        //Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            _target = null;
        }
        //
        #endregion

        [TestMethod]
        public void AddTest()
        {
            var add = new MockObject();

            for (int i = 0; i < 10; i++)
            {
                _target.Add(add);
            }

            int expected = 10;
            int actual;
            actual = _target.Add(add);
            Assert.AreEqual(expected, actual);
        }

        [TestMethod]
        public void RemoveAddTest()
        {
            var add = new MockObject();

            for (int i = 0; i < 10; i++)
            {
                _target.Add(add);
            }

            _target.Remove(3);

            int expected = 3;
            int actual = _target.Add(add);
            Assert.AreEqual(expected, actual);

            _target.Remove(0);
            expected = 0;
            actual = _target.Add(add);
            Assert.AreEqual(expected, actual);


            //make sure out of bounds doesn't throw
            _target.Remove(int.MaxValue);
        }

        [TestMethod]
        public void InflationTest()
        {
            const int InflationTestSize = 300;
            for (int i = 0; i < InflationTestSize; i++)
            {
                var add = new MockObject();
                add.Id = _target.Add(add);
            }

            foreach (var add in _target)
            {
                _target.Remove(add.Id);
            }

            for (int i = 0; i < InflationTestSize; i++)
            {
                var add = new MockObject();
                add.Id = _target.Add(add);
                Assert.IsTrue(add.Id < InflationTestSize);
            }

            Assert.AreEqual(_target.Capacity, InflationTestSize);
        }
    }
}
