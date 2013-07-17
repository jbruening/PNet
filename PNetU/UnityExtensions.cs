using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PNetU
{
    /// <summary>
    /// Extensions for unity stuff
    /// </summary>
    public static class UnityExtensions
    {
        /// <summary>
        /// Serialize the quaternion to the message
        /// </summary>
        /// <param name="quaternion"></param>
        /// <param name="message"></param>
        public static void Serialize(this Quaternion quaternion, Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(quaternion.x);
            message.Write(quaternion.y);
            message.Write(quaternion.z);
            message.Write(quaternion.w);
        }

        /// <summary>
        /// serialize the vector3 to the message
        /// </summary>
        /// <param name="vector3"></param>
        /// <param name="message"></param>
        public static void Serialize(this Vector3 vector3, Lidgren.Network.NetOutgoingMessage message)
        {
            message.Write(vector3.x);
            message.Write(vector3.y);
            message.Write(vector3.z);
        }

        /// <summary>
        /// Get the 3 bytes
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static byte[] RGBBytes(this Color color)
        {
            Color32 col32 = color;

            byte[] bytes = new byte[3];
            bytes[0] = col32.r;
            bytes[1] = col32.g;
            bytes[2] = col32.b;

            return bytes;
        }

        /// <summary>
        /// Get the 4 bytes
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static byte[] ARGBBytes(this Color color)
        {
            Color32 col32 = color;

            byte[] bytes = new byte[4];
            bytes[0] = col32.a;
            bytes[1] = col32.r;
            bytes[2] = col32.g;
            bytes[3] = col32.b;

            return bytes;
        }

        /// <summary>
        /// Get the 3 bytes
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static byte[] RGBBytes(this Color32 color)
        {
            byte[] bytes = new byte[3];
            bytes[0] = color.r;
            bytes[1] = color.g;
            bytes[2] = color.b;

            return bytes;
        }

        /// <summary>
        /// Get the 4 bytes
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static byte[] ARGBBytes(this Color32 color)
        {
            byte[] bytes = new byte[4];
            bytes[0] = color.a;
            bytes[1] = color.r;
            bytes[2] = color.g;
            bytes[3] = color.b;

            return bytes;
        }

        internal static PNetC.RPCMode ToPNetCMode(this UnityEngine.RPCMode mode)
        {
            switch (mode)
            {
                case RPCMode.Others: return PNetC.RPCMode.Others;
                case RPCMode.Server: return PNetC.RPCMode.Server;
                case RPCMode.All: return PNetC.RPCMode.All;
                case RPCMode.OthersBuffered: return PNetC.RPCMode.OthersBuffered;
                case RPCMode.AllBuffered: return PNetC.RPCMode.AllBuffered;
                default: return PNetC.RPCMode.Server;
            }
        }
    }
}
