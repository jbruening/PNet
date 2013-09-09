using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using PNet;
using System.Globalization;

namespace PNetC
{
    public struct Quaternion : INetSerializable
    {
        public float X, Y, Z, W;
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(X);
            message.Write(Y);
            message.Write(Z);
            message.Write(W);
        }

        public void OnDeserialize(NetIncomingMessage message)
        {
            X = message.ReadFloat();
            Y = message.ReadFloat();
            Z = message.ReadFloat();
            W = message.ReadFloat();
        }

        public int AllocSize
        {
            get { return 16; }
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}, {3}", 
                X.ToString(CultureInfo.InvariantCulture),
                Y.ToString(CultureInfo.InvariantCulture),
                Z.ToString(CultureInfo.InvariantCulture),
                W.ToString(CultureInfo.InvariantCulture));
        }
    }

    public struct Vector3 : INetSerializable
    {
        public float X, Y, Z;
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(X);
            message.Write(Y);
            message.Write(Z);
        }

        public void OnDeserialize(NetIncomingMessage message)
        {
            X = message.ReadFloat();
            Y = message.ReadFloat();
            Z = message.ReadFloat();
        }

        public int AllocSize
        {
            get { return 12; }
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}, {2}", 
                X.ToString(CultureInfo.InvariantCulture), 
                Y.ToString(CultureInfo.InvariantCulture), 
                Z.ToString(CultureInfo.InvariantCulture));
        }
    }
}
