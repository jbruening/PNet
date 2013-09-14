using System;
using System.Collections.Generic;
using Lidgren.Network;
using PNet;

namespace PNetC
{
    /// <summary>
    /// network synchronization
    /// </summary>
    public sealed class NetworkView
    {
        /// <summary>
        /// The object that this networkview is attached to.
        /// </summary>
        public object Container { get; internal set; }

        internal readonly NetworkViewManager Manager;

        #region NetworkViewID
        /// <summary>
        /// If i'm the owner
        /// </summary>
        public bool IsMine { get; private set; }
        /// <summary>
        /// identifier for the network view
        /// </summary>
        public NetworkViewId ViewID = NetworkViewId.Zero;

        private ushort _ownerId;
        /// <summary>
        /// ID of the owner. 0 is the server.
        /// </summary>
        public ushort OwnerId
        {
            get { return _ownerId; }
            internal set
            {
                _ownerId = value;
                IsMine = _ownerId == Manager.Net.PlayerId;
            }
        }

        #endregion

        /// <summary>
        /// stream size. Helps prevent array resizing
        /// </summary>
        public int DefaultStreamSize;

        private readonly Dictionary<byte, Action<NetIncomingMessage>> _rpcProcessors = new Dictionary<byte, Action<NetIncomingMessage>>();
        private readonly IntDictionary<Action<NetIncomingMessage>> _fieldProcessors = new IntDictionary<Action<NetIncomingMessage>>();
        private Action<NetOutgoingMessage> _onSerializeStream = delegate { };

        /// <summary>
        /// method of serialization
        /// </summary>
        public NetworkStateSynchronization StateSynchronization = NetworkStateSynchronization.Off;
        /// <summary>
        /// subscribe to this in order to deserialize streaming data
        /// </summary>
        public event Action<NetIncomingMessage> OnDeserializeStream;
        /// <summary>
        /// Subscribe to this to know when an object is being destroyed by the server.
        /// </summary>
        public event Action OnRemove = delegate { };
        /// <summary>
        /// run once we've finished setting up the networkview variables
        /// </summary>
        public event Action OnFinishedCreation = delegate { };

        internal NetworkView(NetworkViewManager manager)
        {
            Manager = manager;
        }

        /// <summary>
        /// Send an rpc
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, RPCMode mode, params INetSerializable[] args)
        {
            var size = 2;
            RPCUtils.AllocSize(ref size, args);

            var message = Manager.Net.Peer.CreateMessage(size);
            message.Write(ViewID.guid);
            message.Write(rpcID);
            RPCUtils.WriteParams(ref message, args);

            Manager.Net.Peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, (int)mode + Channels.BEGIN_RPCMODES);
        }

        /// <summary>
        /// Send an rpc to the owner of this object
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="args"></param>
        public void RPCToOwner(byte rpcID, params INetSerializable[] args)
        {
            var size = 3;
            RPCUtils.AllocSize(ref size, args);

            var message = Manager.Net.Peer.CreateMessage(size);
            message.Write(ViewID.guid);
            message.Write(rpcID);
            RPCUtils.WriteParams(ref message, args);

            Manager.Net.Peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
        }

        #region serialization
        /// <summary>
        /// set the method to be used during stream serialization
        /// </summary>
        /// <param name="newMethod"></param>
        /// <param name="defaultStreamSize"></param>
        public void SetSerializationMethod(Action<NetOutgoingMessage> newMethod, int defaultStreamSize = 16)
        {
            if (newMethod != null)
            {
                _onSerializeStream = newMethod;
                DefaultStreamSize = defaultStreamSize;
            }
        }

        internal void DoOnDeserializeStream(NetIncomingMessage msg)
        {
            if (OnDeserializeStream != null)
                OnDeserializeStream(msg);
        }

        /// <summary>
        /// Needs to be called by the implementing engine
        /// </summary>
        public void DoStreamSerialize()
        {
            if (Manager.Net.Status == NetConnectionStatus.Connected)
            {
                var nMessage = Manager.Net.Peer.CreateMessage(DefaultStreamSize);
                nMessage.Write(ViewID.guid);
                _onSerializeStream(nMessage);

                if (StateSynchronization == NetworkStateSynchronization.Unreliable)
                    Manager.Net.Peer.SendMessage(nMessage, NetDeliveryMethod.Unreliable, Channels.UNRELIABLE_STREAM);
                else if (StateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed)
                    Manager.Net.Peer.SendMessage(nMessage, NetDeliveryMethod.ReliableOrdered, Channels.RELIABLE_STREAM);
            }
        }

        #endregion

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
            
            //dont add the rpcProcessor if we already have one, because the caller has specified to not overwrite
            Action<NetIncomingMessage> checkExist;
            if (_rpcProcessors.TryGetValue(rpcID, out checkExist)) return false;
            
            _rpcProcessors.Add(rpcID, rpcProcessor);
            return true;
        }

        internal void SubscribeToSynchronizedField<T>(SynchronizedField<T> field)
        {
            int fieldId = _fieldProcessors.Add(field.OnReceiveValue);
            field.FieldId = (byte)fieldId;
        }

        internal void UnsubscribeSynchronizedField(int fieldId)
        {
            _fieldProcessors.Remove(fieldId);
        }

        /// <summary>
        /// Unsubscribe from an rpc
        /// </summary>
        /// <param name="rpcID"></param>
        public void UnsubscribeFromRPC(byte rpcID)
        {
            _rpcProcessors.Remove(rpcID);
        }

        internal void CallRPC(byte rpcID, NetIncomingMessage message)
        {
            Action<NetIncomingMessage> processor;
            if (_rpcProcessors.TryGetValue(rpcID, out processor))
            {
                if (processor != null)
                    processor(message);
                else
                {
                    //Debug.LogWarning("RPC processor for " + rpcID + " was null. Automatically cleaning up. Please be sure to clean up after yourself in the future.");
                    _rpcProcessors.Remove(rpcID);
                }
            }
            else
            {
                Debug.LogWarning("NetworkView " + ViewID.guid + ": unhandled RPC " + rpcID);
            }
        }

        internal void SetSynchronizedField(byte fieldID, NetIncomingMessage message)
        {
            Action<NetIncomingMessage> processor;
            if (_fieldProcessors.HasValue(fieldID))
            {
                processor = _fieldProcessors[fieldID];

                if (processor != null)
                    processor(message);
                else
                    _fieldProcessors.Remove(fieldID);
            }
            else
                Debug.LogWarning("Unhandled synchronized field " + fieldID);
        }
        
        internal void DoOnFinishedCreation()
        {
            try
            {
                if (OnFinishedCreation != null) OnFinishedCreation();
            }
            catch (Exception e)
            {
                Debug.LogError("[NetworkView.OnFinishedCreation] {0}", e);
            }
        }

        internal void DoOnRemove()
        {
            //do some cleanup
            _rpcProcessors.Clear();
            _fieldProcessors.Clear();
            _onSerializeStream = delegate { };

            try
            {
                if (OnRemove != null) OnRemove();
            }
            catch(Exception e)
            {
                Debug.LogError("[NetworkView.OnRemove] {0}", e);
            }

            Manager.RemoveView(this);
            Container = null;
        }

        /// <summary>
        /// viewid, ownerid, container
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return string.Format("NV {0}:{1}:{2}", ViewID.guid, OwnerId, Container);
        }
    }
}
