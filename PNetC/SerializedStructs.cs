using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using PNet;
using System.Globalization;

namespace PNetC
{
    /// <summary>
    /// rotation struct
    /// </summary>
    public struct Quaternion : INetSerializable
    {
        /// <summary>
        /// delta of the 4d rotation
        /// </summary>
        public float X, Y, Z, W;

        /// <summary>
        /// write to the message
        /// </summary>
        /// <param name="message">message to write to</param>
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(X);
            message.Write(Y);
            message.Write(Z);
            message.Write(W);
        }

        /// <summary>
        /// read the message
        /// </summary>
        /// <param name="message">message to read from</param>
        public void OnDeserialize(NetIncomingMessage message)
        {
            X = message.ReadFloat();
            Y = message.ReadFloat();
            Z = message.ReadFloat();
            W = message.ReadFloat();
        }

        /// <summary>
        /// size in bytes
        /// </summary>
        public int AllocSize
        {
            get { return 16; }
        }

        /// <summary>
        /// invariant culture
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", 
                X.ToString(CultureInfo.InvariantCulture),
                Y.ToString(CultureInfo.InvariantCulture),
                Z.ToString(CultureInfo.InvariantCulture),
                W.ToString(CultureInfo.InvariantCulture));
        }
    }

    /// <summary>
    /// position/direction struct
    /// </summary>
    public struct Vector3 : INetSerializable
    {
        /// <summary>
        /// delta of the three axis
        /// </summary>
        public float X, Y, Z;

        /// <summary>
        /// write to the message
        /// </summary>
        /// <param name="message">message to write to</param>
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(X);
            message.Write(Y);
            message.Write(Z);
        }

        /// <summary>
        /// read the message
        /// </summary>
        /// <param name="message">message to read from</param>
        public void OnDeserialize(NetIncomingMessage message)
        {
            X = message.ReadFloat();
            Y = message.ReadFloat();
            Z = message.ReadFloat();
        }

        /// <summary>
        /// size in bytes
        /// </summary>
        public int AllocSize
        {
            get { return 12; }
        }

        /// <summary>
        /// invariant culture
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", 
                X.ToString(CultureInfo.InvariantCulture), 
                Y.ToString(CultureInfo.InvariantCulture), 
                Z.ToString(CultureInfo.InvariantCulture));
        }
    }
}
