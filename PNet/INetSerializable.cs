using System;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace PNet
{
    /// <summary>
    /// Interface used for objects that can read and write from network streams
    /// </summary>
    public interface INetSerializable
    {
        /// <summary>
        /// write to the message
        /// </summary>
        /// <param name="message">message to write to</param>
        void OnSerialize(NetOutgoingMessage message);
        /// <summary>
        /// read the message
        /// </summary>
        /// <param name="message">message to read from</param>
        void OnDeserialize(NetIncomingMessage message);

        /// <summary>
        /// size to allocate for bytes in the message.  if you're under, it'll result in array resizing.
        /// </summary>
        int AllocSize { get; }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class ASerializable<TSerialize, TValue> : INetSerializable where TSerialize : ASerializable<TSerialize, TValue>, new()
    {
        /// <summary>
        /// value of the class
        /// </summary>
        public TValue Value;

        public abstract void OnSerialize(NetOutgoingMessage message);

        public abstract void OnDeserialize(NetIncomingMessage message);

        public abstract int AllocSize { get; }

        /// <summary>
        /// a static, single instance of the class. Can be used to reduce garbage collection
        /// </summary>
        public static readonly TSerialize Instance = new TSerialize();
        /// <summary>
        /// update Value with the newValue
        /// </summary>
        /// <param name="newValue"></param>
        /// <returns>this</returns>
        public ASerializable<TSerialize, TValue> Update(TValue newValue)
        {
            Value = newValue;
            return this;
        }
    }

    /// <summary>
    /// serializer for strings
    /// </summary>
    public class StringSerializer : ASerializable<StringSerializer, string>
    {
        public StringSerializer(string value)
        {
            Value = value;
        }
        public StringSerializer() { Value = ""; }
        /// <summary>
        /// serialize
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value);
        }
        /// <summary>
        /// deserialize
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(NetIncomingMessage message)
        {
            message.ReadString(out Value);
        }
        /// <summary>
        /// get the size of the string in bytes
        /// </summary>
        public override int AllocSize
        {
            get { return Value.Length * 2; }
        }
    }

    /// <summary>
    /// serializer for integers
    /// </summary>
    public class IntSerializer : ASerializable<IntSerializer, int>
    {
        public IntSerializer(){}
        public IntSerializer(int value)
        {
            Value = value;
        }

        /// <summary>
        /// serialize
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value);
        }

        /// <summary>
        /// deserialize
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(NetIncomingMessage message)
        {
            Value = message.ReadInt32();
        }

        /// <summary>
        /// size of the integer in bytes
        /// </summary>
        public override int AllocSize
        {
            get { return 4; }
        }
    }

    /// <summary>
    /// serializer for floats
    /// </summary>
    public class FloatSerializer : ASerializable<FloatSerializer, float>
    {
        /// <summary>
        /// create a new serializer for a float
        /// </summary>
        /// <param name="value"></param>
        public FloatSerializer(float value) { Value = value; }

        /// <summary>
        /// Create a new serializer with a value of 0
        /// </summary>
        public FloatSerializer() { }

        /// <summary>
        /// serialize
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value);
        }

        /// <summary>
        /// deserialize
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(NetIncomingMessage message)
        {
            Value = message.ReadFloat();
        }

        /// <summary>
        /// get the size of this serializable
        /// </summary>
        public override int AllocSize
        {
            get { return 4; }
        }
    }

    /// <summary>
    /// serializer for a short
    /// </summary>
    public class ShortSerializer : ASerializable<ShortSerializer, short>
    {
        /// <summary>
        /// 
        /// </summary>
        public ShortSerializer(){}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ShortSerializer(short value)
        {
            Value = value;
        }
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value);
        }

        public override void OnDeserialize(NetIncomingMessage message)
        {
            Value = message.ReadInt16();
        }

        public override int AllocSize
        {
            get { return 2; }
        }
    }

    /// <summary>
    /// serializer for a ushort
    /// </summary>
    public class UShortSerializer : ASerializable<UShortSerializer, ushort>
    {
        /// <summary>
        /// 
        /// </summary>
        public UShortSerializer(){}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public UShortSerializer(ushort value)
        {
            Value = value;
        }
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value);
        }

        public override void OnDeserialize(NetIncomingMessage message)
        {
            Value = message.ReadUInt16();
        }

        public override int AllocSize
        {
            get { return 2; }
        }
    }

    /// <summary>
    /// class to serialize single bytes
    /// </summary>
    public class ByteSerializer : ASerializable<ByteSerializer, byte>
    {
        /// <summary>
        /// 
        /// </summary>
        public ByteSerializer(){}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public ByteSerializer(byte value)
        {
            Value = value;
        }
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value);
        }

        public override void OnDeserialize(NetIncomingMessage message)
        {
            Value = message.ReadByte();
        }

        public override int AllocSize
        {
            get { return 1; }
        }
    }

    /// <summary>
    /// class to serialize single booleans
    /// </summary>
    public class BoolSerializer : ASerializable<BoolSerializer, bool>
    {
        /// <summary>
        /// 
        /// </summary>
        public BoolSerializer(){}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public BoolSerializer(bool value)
        {
            Value = value;
        }
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value);
        }

        public override void OnDeserialize(NetIncomingMessage message)
        {
            Value = message.ReadBoolean();
        }

        public override int AllocSize
        {
            get { return 1; }
        }
    }

    /// <summary>
    /// class to serialize byte arrays
    /// </summary>
    public class ByteArraySerializer : ASerializable<ByteArraySerializer, byte[]>
    {
        /// <summary>
        /// serialize to the stream
        /// </summary>
        /// <param name="message"></param>
        public override void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Value.Length);
            message.Write(Value);
        }

        /// <summary>
        /// deserialize from the stream
        /// </summary>
        /// <param name="message"></param>
        public override void OnDeserialize(NetIncomingMessage message)
        {
            var size = message.ReadInt32();
            message.ReadBytes(size, out Value);
        }

        /// <summary />
        public override int AllocSize { get { return Value.Length + 4; } }
    }
}
