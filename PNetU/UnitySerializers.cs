using PNet;
using UnityEngine;

namespace PNetU
{
    /// <summary>
    /// Serializer for vectors
    /// </summary>
    public class Vector3Serializer : ASerializable<Vector3Serializer, Vector3>
    {
        /// <summary>
        /// create a new serializer from the Vector3
        /// </summary>
        /// <param name="vector3"></param>
        public Vector3Serializer(Vector3 vector3)
        {
            this.Value = vector3;
        }

        /// <summary>
        /// New serializer, value at zero
        /// </summary>
        public Vector3Serializer()
        {
            this.Value = Vector3.zero;
        }

        /// <summary>
        /// serialize vector3 into message
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(Value.x);
            message.Write(Value.y);
            message.Write(Value.z);
        }

        /// <summary>
        /// deserialize into vector3
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            Value.x = message.ReadFloat();
            Value.y = message.ReadFloat();
            Value.z = message.ReadFloat();
        }

        /// <summary>
        /// 12 bytes
        /// </summary>
        public override int AllocSize
        {
            get { return 12; }
        }
    }

    /// <summary>
    /// Serializer for quaternions
    /// </summary>
    public class QuaternionSerializer : ASerializable<QuaternionSerializer, Quaternion>
    {
        /// <summary>
        /// create a new serializer from the quaternion
        /// </summary>
        /// <param name="quaternion"></param>
        public QuaternionSerializer(Quaternion quaternion)
        {
            this.Value = quaternion;
        }

        /// <summary>
        /// new serializer, value is identity
        /// </summary>
        public QuaternionSerializer()
        {
            this.Value = Quaternion.identity;
        }
        /// <summary>
        /// serialize quaternion into the stream
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(Value.x);
            message.Write(Value.y);
            message.Write(Value.z);
            message.Write(Value.w);
        }

        /// <summary>
        /// deserialize into quaternion
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            Value.x = message.ReadFloat();
            Value.y = message.ReadFloat();
            Value.z = message.ReadFloat();
            Value.w = message.ReadFloat();
        }
        
        /// <summary>
        /// 16 bytes
        /// </summary>
        public override int AllocSize
        {
            get { return 16; }
        }
    }
}