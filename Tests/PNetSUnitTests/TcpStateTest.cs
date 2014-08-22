using System;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNetS.Server;

namespace PNetSUnitTests
{
    [TestClass]
    public class TcpStateTest
    {
        /// <summary>
        /// test to ensure that the message can be received even if broken into parts
        /// </summary>
        [TestMethod]
        public void DividedTest()
        {
            var state = new TcpServer.NetworkMessage();

            var strmsg = "Hello world";
            var message = Encoding.UTF8.GetBytes(strmsg);
            var msgSize = message.Length;
            
            var sizeBytes = BitConverter.GetBytes(msgSize);

            var firstPart = new byte[2];
            Buffer.BlockCopy(sizeBytes, 0, firstPart, 0, 2);
            var secondSize = message.Length/2;
            var secondPart = new byte[secondSize + 2];
            Buffer.BlockCopy(sizeBytes, 2, secondPart, 0, 2);
            Buffer.BlockCopy(message, 0, secondPart, 2, secondSize);
            var lastSize = message.Length - secondSize;
            var lastPart = new byte[lastSize];
            Buffer.BlockCopy(message, secondSize, lastPart, 0, lastSize);

            state.Reset();

            Buffer.BlockCopy(firstPart, 0, state.Buffer, 0, firstPart.Length);
            state.ConsumeBuffer(firstPart.Length);
            Buffer.BlockCopy(secondPart, 0, state.Buffer, 0, secondPart.Length);
            state.ConsumeBuffer(secondPart.Length);
            Buffer.BlockCopy(lastPart, 0, state.Buffer, 0, lastPart.Length);
            state.ConsumeBuffer(lastPart.Length);

            
            Assert.IsTrue(state.IsValidMessage);
            Assert.AreEqual(state.MessageSize, msgSize);
            var result = Encoding.UTF8.GetString(state.Message, 0, state.MessageSize);
            Assert.AreEqual(strmsg, result);
        }

        /// <summary>
        /// Test to ensure that states are capable of resizing their message array to fit messages larger than their inital size
        /// </summary>
        [TestMethod]
        public void LargeMessageTest()
        {
            var state = new TcpServer.NetworkMessage();

            Random rand = new Random();
            var message = new byte[2000];
            rand.NextBytes(message);
            var msgSize = message.Length;

            var sizeBytes = BitConverter.GetBytes(msgSize);

            var firstPart = new byte[1000];
            firstPart[0] = sizeBytes[0];
            firstPart[1] = sizeBytes[1];
            firstPart[2] = sizeBytes[2];
            firstPart[3] = sizeBytes[3];

            Buffer.BlockCopy(message, 0, firstPart, 4, firstPart.Length - 4);

            var secondPart = new byte[message.Length - firstPart.Length + 4];
            Buffer.BlockCopy(message, firstPart.Length - 4, secondPart, 0, secondPart.Length);

            state.Reset();
            Buffer.BlockCopy(firstPart, 0, state.Buffer, 0, firstPart.Length);
            state.ConsumeBuffer(firstPart.Length);
            Buffer.BlockCopy(secondPart, 0, state.Buffer, 0, secondPart.Length);
            state.ConsumeBuffer(secondPart.Length);

            Assert.IsTrue(state.IsValidMessage);
            Assert.AreEqual(msgSize, state.MessageSize);
            var stateMessage = new byte[state.MessageSize];
            Buffer.BlockCopy(state.Message, 0, stateMessage, 0, state.MessageSize);
            CollectionAssert.AreEqual(message, stateMessage);
        }
    }
}
