using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Lidgren.Network;
using PNet;

namespace PNetS
{
    /// <summary>
    /// a player with external data and network identification
    /// </summary>
    public class Player
    {
        /// <summary>
        /// the network id of the player
        /// </summary>
        [System.ComponentModel.DefaultValue(0)]
        public ushort Id { get; internal set; }

        /// <summary>
        /// number of times the player has incurred an internal error
        /// </summary>
        public int InternalErrorCount
        {
            get { return _internalErrorCount; }
            internal set
            {
                _internalErrorCount = value;
                if (_internalErrorCount > MaxInternalErrorCount)
                {
                    Debug.LogWarning("Player {0} disconnected for reaching maximum internal error count", UserData);
                    Disconnect("Maximum allowable network errors reached");
                }
            }
        }

        private int _internalErrorCount;

        /// <summary>
        /// number of times a player can incur internal errors before being automatically disconnected
        /// </summary>
        public static int MaxInternalErrorCount = 100;

        /// <summary>
        /// custom object to associate with the player. not synched over the network.
        /// </summary>
        public object UserData;

        /// <summary>
        /// user defined tag. please use UserData
        /// </summary>
        [Obsolete("use UserData")]
        public object Tag { get { return UserData; } set { UserData = value; } }

        /// <summary>
        /// 
        /// </summary>
        public NetConnectionStatus Status { get { return connection.Status; } }

        /// <summary>
        /// remote player's network time (PNetC.Time), at the current PNetServer.Time
        /// </summary>
        public double RemoteTime
        {
            get
            {
                return connection.GetRemoteTime(GameState.NetFrameTime);
            }
        }

        internal NetConnection connection;
        /// <summary>
        /// current room the player is in. can be null
        /// </summary>
        public Room CurrentRoom { get; internal set; }
        
        internal Player()
        {
            CurrentRoom = null;
            
        }

        private Room newRoom;

        /// <summary>
        /// change the player to the specified room
        /// </summary>
        /// <exception cref="ThreadStateException">if GameState.InvokeRequired</exception>
        /// <param name="room"></param>
        public void ChangeRoom(Room room)
        {
            if (GameState.InvokeRequired)
                throw new ThreadStateException("Cannot change rooms on unless on gamestate's update thread");

            newRoom = room;

            if (CurrentRoom != null)
                CurrentRoom.RemovePlayer(this);
            CurrentRoom = null;

            var message = PNetServer.peer.CreateMessage();
            message.Write(RPCUtils.ChangeRoom);
            message.Write(room.Name);

            connection.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
        }

        /// <summary>
        /// Fired as a player is about to leave a room.  Once it returns, the player will be removed from the room.
        /// </summary>
        public event Action<Room> LeavingRoom;

        internal void FireLeaveRoom(Room roomLeaving)
        {
            if (LeavingRoom != null)
                LeavingRoom(roomLeaving);
        }

        internal Room GetRoomSwitchingTo()
        {
            return newRoom;
        }

        /// <summary>
        /// disconnect the player with the specified reason sent to the player
        /// </summary>
        /// <param name="reason"></param>
        public void Disconnect(string reason)
        {
            connection.Disconnect(reason);
        }

        /// <summary>
        /// the server's player
        /// </summary>
        public static Player Server { get; internal set; }

        #region RPC
        /// <summary>
        /// Send a static RPC to the player.
        /// </summary>
        /// <param name="rpcId"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcId, params INetSerializable[] args)
        {
            var size = 1;
            RPCUtils.AllocSize(ref size, args);

            var message = CreateMessage(rpcId, size);
            RPCUtils.WriteParams(ref message, args);

            SendMessage(message);
        }

        /// <summary>
        /// Send a static RPC to the player. (prevents array allocation)
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="ordered"></param>
        public void RPC(byte rpcID, bool ordered = true)
        {
            var size = 1;
            var message = CreateMessage(rpcID, size);
            if (ordered)
                SendMessage(message);
            else
                SendUnorderedMessage(message);
        }

        /// <summary>
        /// Send a static RPC to the player. (prevents array allocation)
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="arg0"></param>
        /// <param name="ordered"></param>
        public void RPC(byte rpcID, INetSerializable arg0, bool ordered = true)
        {
            var size = 1;
            size += arg0.AllocSize;

            var message = CreateMessage(rpcID, size);            

            arg0.OnSerialize(message);

            if (ordered)
                SendMessage(message);
            else
                SendUnorderedMessage(message);
        }

        /// <summary>
        /// Send a static RPC to the player. (prevents array allocation)
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="ordered"></param>
        public void RPC(byte rpcID, 
            INetSerializable arg0, 
            INetSerializable arg1, 
            bool ordered = true)
        {
            var size = 1;
            size += arg0.AllocSize;
            size += arg1.AllocSize;

            var message = CreateMessage(rpcID, size);

            arg0.OnSerialize(message);
            arg1.OnSerialize(message);

            if (ordered)
                SendMessage(message);
            else
                SendUnorderedMessage(message);
        }
        
        /// <summary>
        /// Send a static RPC to the player. (prevents array allocation)
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="ordered"></param>
        public void RPC(byte rpcID, 
            INetSerializable arg0, 
            INetSerializable arg1, 
            INetSerializable arg2, 
            bool ordered = true)
        {
            var size = 1;
            size += arg0.AllocSize;
            size += arg1.AllocSize;
            size += arg2.AllocSize;

            var message = CreateMessage(rpcID, size);

            arg0.OnSerialize(message);
            arg1.OnSerialize(message);
            arg2.OnSerialize(message);

            if (ordered)
                SendMessage(message);
            else
                SendUnorderedMessage(message);
        }

        /// <summary>
        /// Send a static RPC to the player. (prevents array allocation)
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="ordered"></param>
        public void RPC(byte rpcID, 
            INetSerializable arg0, 
            INetSerializable arg1, 
            INetSerializable arg2, 
            INetSerializable arg3, 
            bool ordered = true)
        {
            var size = 1;
            size += arg0.AllocSize;
            size += arg1.AllocSize;
            size += arg2.AllocSize;
            size += arg3.AllocSize;

            var message = CreateMessage(rpcID, size);

            arg0.OnSerialize(message);
            arg1.OnSerialize(message);
            arg2.OnSerialize(message);
            arg3.OnSerialize(message);

            if (ordered)
                SendMessage(message);
            else
                SendUnorderedMessage(message);
        }
        /// <summary>
        /// Send a static RPC to the player. (prevents array allocation)
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="arg0"></param>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <param name="arg3"></param>
        /// <param name="arg4"></param>
        /// <param name="ordered"></param>
        public void RPC(byte rpcID, 
            INetSerializable arg0, 
            INetSerializable arg1, 
            INetSerializable arg2, 
            INetSerializable arg3, 
            INetSerializable arg4, 
            bool ordered = true)
        {
            var size = 1;
            size += arg0.AllocSize;
            size += arg1.AllocSize;
            size += arg2.AllocSize;
            size += arg3.AllocSize;
            size += arg4.AllocSize;

            var message = CreateMessage(rpcID, size);

            arg0.OnSerialize(message);
            arg1.OnSerialize(message);
            arg2.OnSerialize(message);
            arg3.OnSerialize(message);
            arg4.OnSerialize(message);

            if (ordered)
                SendMessage(message);
            else
                SendUnorderedMessage(message);
        }

        #region nonordered
        /// <summary>
        /// Send a static RPC to the player, in any order
        /// </summary>
        /// <param name="rpcId"></param>
        /// <param name="args"></param>
        public void RPCUnordered(byte rpcId, params INetSerializable[] args)
        {
            var size = 1;
            RPCUtils.AllocSize(ref size, args);

            var message = CreateMessage(rpcId, size);
            RPCUtils.WriteParams(ref message, args);

            SendMessage(message);
        }

        void SendUnorderedMessage(NetOutgoingMessage msg)
        {
            connection.SendMessage(msg, NetDeliveryMethod.ReliableUnordered, Channels.STATIC_RPC);
        }
        #endregion

        NetOutgoingMessage CreateMessage(byte rpcID, int size)
        {
            var message = PNetServer.peer.CreateMessage(size);
            message.Write(rpcID);
            return message;
        }

        void SendMessage(NetOutgoingMessage msg)
        {
            connection.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
        }

        #endregion

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>
        /// A string that represents the current object.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            if (UserData != null)
                return "(Player) " + UserData;
            return base.ToString();
        }
    }
}
