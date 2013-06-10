using System.Collections.Generic;

namespace PNet
{
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
            {
                message.Write(0);
                return;
            }

            message.Write(items.Length);
            foreach (var item in items)
                item.OnSerialize(message);
        }
    }
}