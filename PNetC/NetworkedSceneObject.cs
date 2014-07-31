using System;
using System.Collections.Generic;
using System.Text;
using PNet;
using Lidgren.Network;

namespace PNetC
{
    /// <summary>
    /// Objects that exist in a scene with pre-synchronized network id's
    /// </summary>
    public class NetworkedSceneObject
    {
        int _networkID;
        private readonly Net _net;

        /// <summary>
        /// The scene/room Network ID of this item. Should be unique per object
        /// </summary>
        public int NetworkID
        {
            get
            {
                return _networkID;
            }
            set { _networkID = value; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="networkID"></param>
        /// <param name="net"></param>
        public NetworkedSceneObject(int networkID, Net net)
        {
            _networkID = networkID;
            _net = net;
            SceneObjects[_networkID] = this;
        }

        /// <summary>
        /// only used for serialization if you really need to. Does not actually make it accessible to the network
        /// </summary>
        /// <param name="networkID"></param>
        public NetworkedSceneObject(int networkID)
        {
            _networkID = networkID;
        }
        
        /// <summary>
        /// Should be called by implementing engine upon a scene change, if relevent
        /// </summary>
        public static void ClearSceneIDs()
        {
            SceneObjects.Clear();
        }

        #region RPC Processing

        readonly Dictionary<byte, Action<NetIncomingMessage>> _rpcProcessors = new Dictionary<byte, Action<NetIncomingMessage>>();

        /// <summary>
        /// Subscribe to an rpc
        /// </summary>
        /// <param name="rpcID">id of the rpc</param>
        /// <param name="rpcProcessor">action to process the rpc with</param>
        /// <param name="overwriteExisting">overwrite the existing processor if one exists.</param>
        /// <returns>Whether or not the rpc was subscribed to. Will return false if an existing rpc was attempted to be subscribed to, and overwriteexisting was set to false</returns>
        public bool SubscribeToRPC(byte rpcID, Action<NetIncomingMessage> rpcProcessor, bool overwriteExisting = true)
        {
            if (rpcProcessor == null)
                throw new ArgumentNullException("rpcProcessor", "the processor delegate cannot be null");
            if (overwriteExisting)
            {
                _rpcProcessors[rpcID] = rpcProcessor;
                return true;
            }
            else
            {
                Action<NetIncomingMessage> checkExist;
                if (_rpcProcessors.TryGetValue(rpcID, out checkExist))
                {
                    return false;
                }
                else
                {
                    _rpcProcessors.Add(rpcID, rpcProcessor);
                    return true;
                }
            }
        }

        /// <summary>
        /// Unsubscribe from an rpc
        /// </summary>
        /// <param name="rpcID"></param>
        public void UnsubscribeFromRPC(byte rpcID)
        {
            _rpcProcessors.Remove(rpcID);
        }

        internal static void CallRPC(int id, byte rpcID, NetIncomingMessage message)
        {
            NetworkedSceneObject sceneObject;
            if (SceneObjects.TryGetValue(id, out sceneObject))
            {
                Action<NetIncomingMessage> processor;
                if (sceneObject._rpcProcessors.TryGetValue(rpcID, out processor))
                {
                    if (processor != null)
                        processor(message);
                    else
                    {
                        Debug.LogWarning(sceneObject._net, "RPC processor for {0} was null. Automatically cleaning up. Please be sure to clean up after yourself in the future.", rpcID);
                        sceneObject._rpcProcessors.Remove(rpcID);
                    }
                }
                else
                {
                    Debug.LogWarning(sceneObject._net, "NetworkedSceneObject on {0}: unhandled RPC {1}", id, rpcID);
                }
            }
            
        }

        #endregion

        /// <summary>
        /// Send an rpc to the server
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, params INetSerializable[] args)
        {
            var size = 3;
            RPCUtils.AllocSize(ref size, args);

            var message = _net.RoomPeer.CreateMessage(size);
            message.Write((ushort)NetworkID);
            message.Write(rpcID);
            RPCUtils.WriteParams(ref message, args);

            _net.RoomPeer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.OBJECT_RPC);
        }

        /// <summary>
        /// serialize this into a string
        /// </summary>
        /// <returns></returns>
        public string Serialize()
        {
            var sb = new StringBuilder();
            sb.AppendLine("-NetworkedSceneObject-");
            sb.Append("id: ").Append(NetworkID).AppendLine(";");

            return sb.ToString();
        }

        private static readonly Dictionary<int, NetworkedSceneObject> SceneObjects = new Dictionary<int, NetworkedSceneObject>();
    }
}