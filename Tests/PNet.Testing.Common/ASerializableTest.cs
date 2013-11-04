using System.Threading;
using Lidgren.Network;
using PNet;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace PNet.Testing.Common
{
    
    
    /// <summary>
    ///This is a test class for ASerializableTest and is intended
    ///to contain all ASerializableTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ASerializableTest
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
        public void InstanceTestIsThreadSafeTest()
        {
            var rand = new Random();
            int t1Value = rand.Next(1, 100);
            int t2Value = rand.Next(1, 100);
            
            if (t1Value == t2Value) 
                t2Value++;

            var oValue = t2Value + 1;

            var t1Handle = new ManualResetEvent(false);
            var t2Handle = new ManualResetEvent(false);

            //first, make sure our test won't be faulty. Ensure that generic instance is the same object
            Assert.AreSame(HookableSerializer<int>.Instance, HookableSerializer<int>.Instance);

            HookableSerializer<int>.Instance.Value = oValue;

            Exception threadExceptions = null;

            var t1 = new Thread(() =>
                                    {
                                        try
                                        {
                                            HookableSerializer<int>.Instance.Value = t1Value;
                                            t1Handle.WaitOne();
                                            Assert.AreEqual(t1Value, HookableSerializer<int>.Instance.Value);
                                            t1Handle.WaitOne();
                                            //we should be equal to t1 value, and definitely not t2 value
                                            Assert.AreEqual(t1Value, HookableSerializer<int>.Instance.Value);
                                            Assert.AreNotEqual(t2Value, HookableSerializer<int>.Instance.Value);
                                        }
                                        catch (Exception e)
                                        {
                                            threadExceptions = e;
                                        }
                                    });
            
            var t2 = new Thread(() =>
                                    {
                                        try
                                        {
                                            //not running by default.
                                            t2Handle.WaitOne();
                                            //we've been released by t1. set and then wait again
                                            HookableSerializer<int>.Instance.Value = t2Value;
                                            t2Handle.WaitOne();
                                            //t1 has checked. let's check ourselves too
                                            Assert.AreEqual(t2Value, HookableSerializer<int>.Instance.Value);
                                            Assert.AreNotEqual(t1Value, HookableSerializer<int>.Instance.Value);
                                        }
                                        catch (Exception e)
                                        {
                                            threadExceptions = e;
                                        }
                                    });

            t1.Start();
            t2.Start();

            Assert.AreEqual(oValue, HookableSerializer<int>.Instance.Value);
            
            Thread.Sleep(20);
            //t1 has already set its value, but not checked. let t2 set
            t2Handle.Set();
            Thread.Sleep(20);
            //now let t1 check.
            t1Handle.Set();
            Thread.Sleep(20);
            //t2 waiting to check, t1 did just check.
            t2Handle.Set();
            Thread.Sleep(20);
            //t2 finished
            t1Handle.Set();
            Thread.Sleep(20);
            //t1 finished

            //apparently if a different thread throws an exception, the test doesn't fail.
            if (threadExceptions != null)
            {
                Assert.Fail("One of the threads failed: " + threadExceptions);
            }
        }

        class HookableSerializer<T> : ASerializable<HookableSerializer<T>, T>
        {

            public event Action<T> OnSerializing;
            public event Action<T> OnDeserializing;
 
            public override void OnSerialize(NetOutgoingMessage message)
            {
                if (OnSerializing != null) OnSerializing(Value);
            }

            public override void OnDeserialize(NetIncomingMessage message)
            {
                if (OnDeserializing != null) OnDeserializing(Value);
            }

            public override int AllocSize
            {
                get { return 4; }
            }
        }
    }
}
