using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace PNet
{
    /// <summary>
    /// class for extensions to lidgren
    /// </summary>
    public static class LidgrenExtensions
    {
        static Dictionary<System.Type, Func<NetIncomingMessage, object>> messageReadMethods = new Dictionary<Type, Func<NetIncomingMessage, object>>()
        {
            { typeof(byte), (msg) => {return msg.ReadByte();}}
            , { typeof(sbyte), (msg) => {return msg.ReadSByte();}}
            , { typeof(short), (msg) => {return msg.ReadInt16();}}
            , { typeof(ushort), (msg) => {return msg.ReadUInt16();}}
            , { typeof(int), (msg) => {return msg.ReadInt32();}}
            , { typeof(uint), (msg) => {return msg.ReadUInt32();}}
            , { typeof(long), (msg) => {return msg.ReadInt64();}}
            , { typeof(ulong), (msg) => {return msg.ReadUInt64();}}
            , { typeof(float), (msg) => {return msg.ReadFloat();}}
            , { typeof(double), (msg) => {return msg.ReadDouble();}}
            , { typeof(string), (msg) => {return msg.ReadString();}}
        };
        static Dictionary<System.Type, Action<NetOutgoingMessage, object>> messageWriteMethods = new Dictionary<Type, Action<NetOutgoingMessage, object>>()
        {
            { typeof(byte), (msg, obj) => { msg.Write((byte)obj);}}
            , { typeof(sbyte), (msg, obj) => { msg.Write((sbyte)obj);}}
            , { typeof(short), (msg, obj) => { msg.Write((short)obj);}}
            , { typeof(ushort), (msg, obj) => { msg.Write((ushort)obj);}}
            , { typeof(int), (msg, obj) => { msg.Write((int)obj);}}
            , { typeof(uint), (msg, obj) => { msg.Write((uint)obj);}}
            , { typeof(long), (msg, obj) => { msg.Write((long)obj);}}
            , { typeof(ulong), (msg, obj) => { msg.Write((ulong)obj);}}
            , { typeof(float), (msg, obj) => { msg.Write((float)obj);}}
            , { typeof(double), (msg, obj) => { msg.Write((double)obj);}}
            , { typeof(string), (msg, obj) => {msg.Write((string)obj);}}
        };
        /// <summary>
        /// ONLY WORKS FOR VALUE TYPES AND STRING. NO ENUMS.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static T Read<T>(this NetIncomingMessage msg)
            where T : struct
        {
            var type = typeof(T);

            return (T)messageReadMethods[type](msg);
        }

        /// <summary>
        /// Read an INetSerializable out of the NetIncomingMessage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <returns></returns>
        public static T ReadINet<T>(this NetIncomingMessage msg)
            where T : INetSerializable, new()
        {
            var ret = new T();
            ret.OnDeserialize(msg);
            return ret;
        }
        /// <summary>
        /// ONLY WORKS FOR VALUE TYPES AND STRING. NO ENUMS.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="msg"></param>
        /// <param name="val"></param>
        public static void Write<T>(this NetOutgoingMessage msg, T val)
            where T : struct
        {
            var type = typeof(T);

            messageWriteMethods[type](msg, val);
        }
    }
}
