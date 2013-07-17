using PNet;
using UnityEngine;

namespace PNetU
{
    /// <summary>
    /// Serializer for vectors
    /// </summary>
    public class Vector3Serializer : INetSerializable
    {
        /// <summary>
        /// the Vector3 used for serializing
        /// </summary>
        public Vector3 vector3;

        /// <summary>
        /// create a new serializer from the Vector3
        /// </summary>
        /// <param name="vector3"></param>
        public Vector3Serializer(Vector3 vector3)
        {
            this.vector3 = vector3;
        }

        /// <summary>
        /// New serializer, value at zero
        /// </summary>
        public Vector3Serializer()
        {
            this.vector3 = Vector3.zero;
        }

        /// <summary>
        /// serialize vector3 into message
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(vector3.x);
            message.Write(vector3.y);
            message.Write(vector3.z);
        }

        /// <summary>
        /// deserialize into vector3
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            vector3.x = message.ReadFloat();
            vector3.y = message.ReadFloat();
            vector3.z = message.ReadFloat();
        }

        /// <summary>
        /// get a Vector3 from the message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Vector3 Deserialize(Lidgren.Network.NetIncomingMessage message)
        {
            return new Vector3(message.ReadFloat(), message.ReadFloat(), message.ReadFloat());
        }

        /// <summary>
        /// 12 bytes
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
        /// the quaternion that is serialized with this instance
        /// </summary>
        public Quaternion quaternion;

        /// <summary>
        /// create a new serializer from the quaternion
        /// </summary>
        /// <param name="quaternion"></param>
        public QuaternionSerializer(Quaternion quaternion)
        {
            this.quaternion = quaternion;
        }

        /// <summary>
        /// new serializer, value is identity
        /// </summary>
        public QuaternionSerializer()
        {
            this.quaternion = Quaternion.identity;
        }
        /// <summary>
        /// serialize quaternion into the stream
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(quaternion.x);
            message.Write(quaternion.y);
            message.Write(quaternion.z);
            message.Write(quaternion.w);
        }

        /// <summary>
        /// deserialize into quaternion
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            quaternion.x = message.ReadFloat();
            quaternion.y = message.ReadFloat();
            quaternion.z = message.ReadFloat();
            quaternion.w = message.ReadFloat();
        }

        /// <summary>
        /// deserialize a quaternion from the specified message
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Quaternion Deserialize(Lidgren.Network.NetIncomingMessage message)
        {
            return new Quaternion(message.ReadFloat(), message.ReadFloat(), message.ReadFloat(), message.ReadFloat());
        }
        
        /// <summary>
        /// 16 bytes
        /// </summary>
        public int AllocSize
        {
            get { return 16; }
        }
    }
}