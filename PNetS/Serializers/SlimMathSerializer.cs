using SlimMath;
using PNet;

namespace PNetS
{
    /// <summary>
    /// Serializer for vectors
    /// </summary>
    public class Vector3Serializer : ASerializable<Vector3Serializer, Vector3>
    {
        /// <summary>
        /// create a new serializer from the specified vector3
        /// </summary>
        /// <param name="vector3"></param>
        public Vector3Serializer(Vector3 vector3)
        {
            Value = vector3;
        }

        /// <summary>
        /// New serializer, value is Zero
        /// </summary>
        public Vector3Serializer()
        {
            Value = Vector3.Zero;
        }
        /// <summary>
        /// serialize to the stream
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(Value.X);
            message.Write(Value.Y);
            message.Write(Value.Z);
        }

        /// <summary>
        /// deserialize from the stream
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            Value.X = message.ReadFloat();
            Value.Y = message.ReadFloat();
            Value.Z = message.ReadFloat();
        }

        /// <summary>
        /// size of vector3 in bytes
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
        /// create a new serializer from the specified quaternion
        /// </summary>
        /// <param name="quaternion"></param>
        public QuaternionSerializer(Quaternion quaternion)
        {
            Value = quaternion;
        }

        /// <summary>
        /// new serializer, quaternion is identity
        /// </summary>
        public QuaternionSerializer()
        {
            Value = Quaternion.Identity;
        }

        /// <summary>
        /// serialize to the stream
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(Value.X);
            message.Write(Value.Y);
            message.Write(Value.Z);
            message.Write(Value.W);
        }

        /// <summary>
        /// deserialize from the stream
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            Value.X = message.ReadFloat();
            Value.Y = message.ReadFloat();
            Value.Z = message.ReadFloat();
            Value.W = message.ReadFloat();
        }

        /// <summary>
        /// size of quaternion in bytes
        /// </summary>
        public override int AllocSize
        {
            get { return 16; }
        }
    }

}
