using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimMath;
using PNet;

namespace PNetS
{
    /// <summary>
    /// Serializer for vectors
    /// </summary>
    public class Vector3Serializer : INetSerializable
    {
        /// <summary>
        /// vector3 used for serialization/deserialization
        /// </summary>
        public Vector3 vector3;

        /// <summary>
        /// create a new serializer from the specified vector3
        /// </summary>
        /// <param name="vector3"></param>
        public Vector3Serializer(Vector3 vector3)
        {
            this.vector3 = vector3;
        }

        /// <summary>
        /// New serializer, value is Zero
        /// </summary>
        public Vector3Serializer()
        {
            this.vector3 = Vector3.Zero;
        }
        /// <summary>
        /// serialize to the stream
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(vector3.X);
            message.Write(vector3.Y);
            message.Write(vector3.Z);
        }

        /// <summary>
        /// deserialize from the stream
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            vector3.X = message.ReadFloat();
            vector3.Y = message.ReadFloat();
            vector3.Z = message.ReadFloat();
        }

        /// <summary>
        /// size of vector3 in bytes
        /// </summary>
        public int AllocSize
        {
            get { return 12; }
        }
    }

    /// <summary>
    /// Serializer for quaternions
    /// </summary>
    public class QuaternionSerializer : INetSerializable
    {
        /// <summary>
        /// quaternion used for serialization/deserialization
        /// </summary>
        public Quaternion quaternion;

        /// <summary>
        /// create a new serializer from the specified quaternion
        /// </summary>
        /// <param name="quaternion"></param>
        public QuaternionSerializer(Quaternion quaternion)
        {
            this.quaternion = quaternion;
        }

        /// <summary>
        /// new serializer, quaternion is identity
        /// </summary>
        public QuaternionSerializer()
        {
            this.quaternion = Quaternion.Identity;
        }

        /// <summary>
        /// serialize to the stream
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(quaternion.X);
            message.Write(quaternion.Y);
            message.Write(quaternion.Z);
            message.Write(quaternion.W);
        }

        /// <summary>
        /// deserialize from the stream
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            quaternion.X = message.ReadFloat();
            quaternion.Y = message.ReadFloat();
            quaternion.Z = message.ReadFloat();
            quaternion.W = message.ReadFloat();
        }

        /// <summary>
        /// size of quaternion in bytes
        /// </summary>
        public int AllocSize
        {
            get { return 16; }
        }
    }

}
