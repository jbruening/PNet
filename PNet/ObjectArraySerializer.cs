using System.Collections.Generic;

namespace PNet
{
    /// <summary>
    /// A serializer for an array of INetSerializable
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
        /// Whether or not the index is preserved with an array with nulls
        /// Takes an additional bit per index
        /// only makes sense for nullable types
        /// </summary>
        public bool PreserveIndex = false;

// ReSharper disable StaticFieldInGenericType
        private static readonly bool IsValueType = typeof (T).IsValueType;
// ReSharper restore StaticFieldInGenericType

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
                    return (4 + items[0].AllocSize + (PreserveIndex ? 1 : 0))  * items.Length;
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
                var hasValue = true;
                if (PreserveIndex && !IsValueType)
                {
                    hasValue = message.ReadBoolean();
                }
                if (!hasValue) continue;

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
            {
                message.Write(0);
                return;
            }

            message.Write(items.Length);
            foreach (var item in items)
            {
                if (!IsValueType)
                {
                    if (PreserveIndex)
                    {
                        message.Write(item != null);
                    }
                    if (item != null)
                        item.OnSerialize(message);
                }
                else
                {
                    item.OnSerialize(message);
                }
            }
        }
    }
}