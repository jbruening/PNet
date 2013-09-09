using Lidgren.Network;
using PNet;

namespace PNetS
{
    public partial class NetworkView
    {
        private const int DEFAULT_RPC_HEADER_SIZE = 3;

        /// <summary>
        /// Send a message to the specified recipients
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, RPCMode mode, params INetSerializable[] args)
        {
            if (_connections.Count == 0)
                return;

            var size = DEFAULT_RPC_HEADER_SIZE;
            RPCUtils.AllocSize(ref size, args);

            var message = CreateMessage(size, rpcID);

            RPCUtils.WriteParams(ref message, args);

            FinishRPCSend(mode, message);
        }

        /// <summary>
        /// Send a message to the specified recipients (prevents array allocation)
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="mode"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"> </param>
        /// <param name="arg4"> </param>
        public void RPC(byte rpcID, RPCMode mode, 
            INetSerializable arg0 = null, 
            INetSerializable arg1 = null,
            INetSerializable arg2 = null,
            INetSerializable arg3 = null,
            INetSerializable arg4 = null)
        {
            if (_connections.Count == 0)
                return;

            var size = DEFAULT_RPC_HEADER_SIZE;
            if (arg0 != null) size += arg0.AllocSize;
            if (arg1 != null) size += arg1.AllocSize;
            if (arg2 != null) size += arg2.AllocSize;
            if (arg3 != null) size += arg3.AllocSize;
            if (arg4 != null) size += arg4.AllocSize;
            var message = CreateMessage(size, rpcID);

            if (arg0 != null) arg0.OnSerialize(message);
            if (arg1 != null) arg1.OnSerialize(message);
            if (arg2 != null) arg2.OnSerialize(message);
            if (arg3 != null) arg3.OnSerialize(message);
            if (arg4 != null) arg4.OnSerialize(message);

            FinishRPCSend(mode, message);
        }

        /// <summary>
        /// Send an rpc, using a custom method to write to the NetOutgoingMessage
        /// DO NOT STORE THE NETOUTGOINGMESSAGE. LIDGREN WILL RECYCLE IT.
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="mode"></param>
        /// <param name="messageSerializer"></param>
        /// <param name="messageSize">the size to initialize the NetOutgoingMessage with. Better to have this be larger than necessary</param>
        public void RPC(byte rpcID, RPCMode mode, MessageSerializer messageSerializer, int messageSize = 0)
        {
            var size = DEFAULT_RPC_HEADER_SIZE + messageSize;
            var message = CreateMessage(size, rpcID);
            messageSerializer(ref message);

            FinishRPCSend(mode, message);
        }

        /// <summary>
        /// delegate type used to serialize messages with the RPC that takes one
        /// </summary>
        /// <param name="message"></param>
        public delegate void MessageSerializer(ref NetOutgoingMessage message);

        void FinishRPCSend(RPCMode mode, NetOutgoingMessage message)
        {
            if (mode == RPCMode.AllBuffered || mode == RPCMode.OthersBuffered)
            {
                Buffer(message);
            }

            SendMessage(message, mode);
        }

        NetOutgoingMessage CreateMessage(int byteSize, byte rpcID)
        {
            var message = PNetServer.peer.CreateMessage(byteSize);
            message.Write(viewID.guid);
            message.Write(rpcID);
            return message;
        }

        /// <summary>
        /// send a message to the specified player
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="player"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, Player player, params INetSerializable[] args)
        {
            if (_connections.Count == 0)
                return;

            var size = DEFAULT_RPC_HEADER_SIZE;
            RPCUtils.AllocSize(ref size, args);
            var message = CreateMessage(size, rpcID);

            RPCUtils.WriteParams(ref message, args);

            PNetServer.peer.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
        }
    }
}
