using System;
using Lidgren.Network;

namespace PNet
{
    /// <summary>
    /// Helpers for RPCs
    /// </summary>
    public static class RPCUtils
    {
        /// <summary>
        /// get the allocation size from the specified serializing objects
        /// </summary>
        /// <param name="prealloc"></param>
        /// <param name="towrite"></param>
        public static void AllocSize(ref int prealloc, INetSerializable[] towrite)
        {
            foreach (var arg in towrite)
            {
                prealloc += arg.AllocSize;
            }
        }

        /// <summary>
        /// write all the serializing objects to the stream
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="towrite"></param>
        public static void WriteParams(ref NetOutgoingMessage msg, INetSerializable[] towrite)
        {
            foreach (var arg in towrite)
            {
                arg.OnSerialize(msg);
            }
        }

        /// <summary>
        /// Serialize to an IntSerializer. This will have issues if the enum isn't an int type.
        /// </summary>
        /// <param name="enumeration"></param>
        /// <returns></returns>
        public static IntSerializer Serialize(this Enum enumeration)
        {
                return new IntSerializer(Convert.ToInt32(enumeration));
        }


        /// <summary>
        /// string resourcepath
        /// ushort viewid
        /// ushort ownerid
        /// </summary>
        public const byte Instantiate = 1;
        /// <summary>
        /// Network destroy
        /// </summary>
        public const byte Remove = 2;
        /// <summary>
        /// new time
        /// </summary>
        public const byte TimeUpdate = 3;
        /// <summary>
        /// set the id of this client
        /// </summary>
        public const byte SetPlayerId = 4;
        /// <summary>
        /// change to the specified room
        /// </summary>
        public const byte ChangeRoom = 5;
        /// <summary>
        /// sent to the server when the client has finished changing rooms
        /// </summary>
        public const byte FinishedRoomChange = 6;
        /// <summary>
        /// sent to the server when a networkview has been created
        /// </summary>
        public const byte FinishedInstantiate = 8;
        /// <summary>
        /// sent to the clients when a view is added to an already existing gameobject with a network view
        /// </summary>
        public const byte AddView = 10;
    }

    /// <summary>
    /// Attribute for marking rpc methods
    /// </summary>
    /// <remarks>
    /// Only one rpc attribute is valid per rpc id per receiving object (room, networkview, etc). If there are multiple, they are overwritten
    /// </remarks>
    [JetBrains.Annotations.MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method, AllowMultiple=true, Inherited=true)]
    public class RpcAttribute : Attribute
    {
        /// <summary>
        /// id of the rpc
        /// </summary>
        public byte rpcId = 0;

        /// <summary>
        /// Server only. what the default value for continue forwarding is set to
        /// </summary>
        public bool defaultContinueForwarding = true;

        /// <summary>
        /// mark the specified method with this rpc id
        /// </summary>
        /// <param name="rpcId"></param>
        public RpcAttribute(byte rpcId)
        {
            this.rpcId = rpcId;
        }

        /// <summary>
        /// Server only
        /// </summary>
        /// <param name="rpcId"></param>
        /// <param name="defaultContinueForwarding">what the default value for continue forwarding is set to</param>
        public RpcAttribute(byte rpcId, bool defaultContinueForwarding)
        {
            this.rpcId = rpcId;
            this.defaultContinueForwarding = defaultContinueForwarding;
        }
    }
}
