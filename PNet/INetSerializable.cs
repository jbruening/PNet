using System;
using System.Collections.Generic;
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
    /// serializer for strings
    /// </summary>
    public class StringSerializer : INetSerializable
    {
        /// <summary>
        /// string to be serialized
        /// </summary>
        public string str;
        /// <summary>
        /// create a new serializer for the specified string
        /// </summary>
        /// <param name="str"></param>
        public StringSerializer(string str)
        {
            this.str = str;
        }
        /// <summary>
        /// Create a new serializer, with nothing for the value
        /// </summary>
        public StringSerializer() { str = ""; }
        /// <summary>
        /// serialize
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(str);
        }
        /// <summary>
        /// deserialize
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(NetIncomingMessage message)
        {
            message.ReadString(out str);
        }
        /// <summary>
        /// get the size of the string in bytes
        /// </summary>
        public int AllocSize
        {
            get { return str.Length * 2; }
        }
    }

    /// <summary>
    /// serializer for integers
    /// </summary>
    public class IntSerializer : INetSerializable
    {
        /// <summary>
        /// integer to be serialized
        /// </summary>
        public int inte;
        /// <summary>
        /// create a new serializer for the specified integer
        /// </summary>
        /// <param name="inte"></param>
        public IntSerializer(int inte) { this.inte = inte; }

        /// <summary>
        /// Create a new serializer with a value of 0
        /// </summary>
        public IntSerializer() { }

        /// <summary>
        /// serialize
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(inte);
        }

        /// <summary>
        /// deserialize
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(NetIncomingMessage message)
        {
            inte = message.ReadInt32();
        }

        /// <summary>
        /// size of the integer in bytes
        /// </summary>
        public int AllocSize
        {
            get { return 4; }
        }
    }

    /// <summary>
    /// serializer for floats
    /// </summary>
    public class FloatSerializer : INetSerializable
    {
        /// <summary>
        /// float used for serialization
        /// </summary>
        public float floa;
        /// <summary>
        /// create a new serializer for a float
        /// </summary>
        /// <param name="inte"></param>
        public FloatSerializer(float inte) { this.floa = inte; }

        /// <summary>
        /// Create a new serializer with a value of 0
        /// </summary>
        public FloatSerializer() { }

        /// <summary>
        /// serialize
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(floa);
        }

        /// <summary>
        /// deserialize
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(NetIncomingMessage message)
        {
            floa = message.ReadFloat();
        }

        /// <summary>
        /// get the size of this serializable
        /// </summary>
        public int AllocSize
        {
            get { return 4; }
        }
    }

    /// <summary>
    /// class to serialize byte arrays
    /// </summary>
    public class ByteArraySerializer : INetSerializable
    {
        /// <summary>
        /// the data
        /// </summary>
        public byte[] Bytes;

        /// <summary>
        /// serialize to the stream
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(NetOutgoingMessage message)
        {
            message.Write(Bytes.Length);
            message.Write(Bytes);
        }

        /// <summary>
        /// deserialize from the stream
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(NetIncomingMessage message)
        {
            var size = message.ReadInt32();
            message.ReadBytes(size, out Bytes);
        }

        public int AllocSize { get { return Bytes.Length + 4; } }
    }

    /// <summary>
    /// A serializer for an array of INetSerializable
    /// WARNING: DOES NOT SAVE INDICES IF THERE ARE NULL VALUES
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ObjectArraySerializer<T> : INetSerializable
        where T : INetSerializable, new()
    {
        /// <summary>
        /// items
        /// </summary>
        public T[] items;

        /// <summary>
        /// Create a new serializer
        /// </summary>
        public ObjectArraySerializer()
        {
        }

        /// <summary>
        /// Create a new serializer from the specified array
        /// </summary>
        /// <param name="newItems"></param>
        public ObjectArraySerializer(T[] newItems)
        {
            items = newItems;
        }

        /// <summary>
        /// Create a new serializer from the specified list
        /// </summary>
        /// <param name="newItems"></param>
        public ObjectArraySerializer(List<T> newItems)
        {
            items = newItems.ToArray();
        }

        /// <summary>
        /// Size when writing to the stream
        /// </summary>
        public int AllocSize
        {
            get
            {
                if (items != null && items.Length >= 1)
                    return 4 + items[0].AllocSize * items.Length;
                return 0;
            }
        }

        /// <summary>
        /// Deserialize the array from the message
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            var length = message.ReadInt32();
            items = new T[length];

            for (int i = 0; i < length; i++)
            {
                var t = new T();
                t.OnDeserialize(message);
                items[i] = t;
            }
        }

        /// <summary>
        /// Serialize the array to the message
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            if (items == null || items.Length == 0)
                return;

            message.Write(items.Length);
            foreach (var item in items)
                item.OnSerialize(message);
        }
    }
}
