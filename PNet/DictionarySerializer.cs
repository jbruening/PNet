using System;
using System.Collections.Generic;
using System.Linq;
using Lidgren.Network;
using System.Runtime.InteropServices;

namespace PNet
{
    /// <summary>
    /// THIS ONLY WORKS WITH BUILT IN TYPES AND STRING. USER DEFINED STRUCTS WILL BREAK
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="U"></typeparam>
    public class DictionarySerializer<T, U> : INetSerializable
        where T : struct
        where U : struct
    {
        /// <summary>
        /// serialized value
        /// </summary>
        public Dictionary<T, U> dictionary;
        /// <summary>
        /// create a new serializer from the dictionary
        /// </summary>
        /// <param name="dictionary"></param>
        public DictionarySerializer(Dictionary<T, U> dictionary)
        {
            this.dictionary = dictionary;
        }
        /// <summary>
        /// create a new serializer with no values
        /// </summary>
        public DictionarySerializer()
        {
            dictionary = new Dictionary<T, U>(0);
        }
        /// <summary>
        /// size in bytes
        /// </summary>
        public int AllocSize
        {
            get { return dictionary.Count * (Marshal.SizeOf(new T()) + Marshal.SizeOf(new U())); }
        }
        /// <summary>
        /// deserialize from the message
        /// </summary>
        /// <param name="message"></param>
        public void OnDeserialize(Lidgren.Network.NetIncomingMessage message)
        {
            var size = message.ReadInt32();
            dictionary = new Dictionary<T, U>(size);

            for (int i = 0; i < size; ++i)
            {
                var key = message.Read<T>();
                var value = message.Read<U>();
                
                dictionary[key] = value;
            }
        }
        /// <summary>
        /// serialize to the message
        /// </summary>
        /// <param name="message"></param>
        public void OnSerialize(Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(dictionary.Count);

            foreach (var kvp in dictionary)
            {
                message.Write(kvp.Key);
                message.Write(kvp.Value);
            }
        }
    }
}
