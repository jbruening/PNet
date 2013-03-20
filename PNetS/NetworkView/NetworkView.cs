using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using PNet;
using System.Collections;
using PNetS;
using System.Threading;
using System.Reflection;
using PNetS.Utils;

namespace PNetS
{
    public sealed partial class NetworkView : Component
    {
        /// <summary>
        /// The owner
        /// </summary>
        public Player owner { get; internal set; }
        /// <summary>
        /// The room this is in
        /// </summary>
        public Room room { get { return gameObject.Room; } }
        /// <summary>
        /// The phase this is in
        /// </summary>
        public int phase { get; private set; }

        /// <summary>
        /// the identifier for the network view
        /// </summary>
        public NetworkViewId viewID = NetworkViewId.Zero;
        
        private List<NetConnection> connections = new List<NetConnection>();
        private List<NetConnection> allButOwner = new List<NetConnection>();

        /// <summary>
        /// move this to a new phase
        /// </summary>
        /// <param name="phase"></param>
        public void MoveToPhase(int phase)
        {
            //TODO: actually change the phase this is in for the room

            this.phase = phase;
            if (room == null)
            {
                Debug.LogError("Network view {0} is not in any room, but it attempted to change phases.", viewID.guid);
            }
            else
            {
                UpdateConnections();
            }
        }

        /// <summary>
        /// update the connections this should send to when rpc or serializing. usually you shouldn't use this
        /// </summary>
        public void UpdateConnections()
        {
            connections = room.GetConnectionsInPhase(phase);
            allButOwner = connections.Where(c => c != owner.connection).ToList();
        }

        private void Destroy()
        {
            //TODO: destroy this
        }
        /// <summary>
        /// cleaning up
        /// </summary>
        protected override void Disposing()
        {
            _rpcProcessors.Clear();
        }

        #region RPC Subscriptions

        void OnComponentAdded(Component component)
        {
            SubscribeMarkedRPCsOnComponent(component);
        }

        /// <summary>
        /// Subscribe all the marked rpcs on the supplied component
        /// </summary>
        /// <param name="component"></param>
        public void SubscribeMarkedRPCsOnComponent(Component component)
        {
            if (component == this) return;
            if (component == null) return;

            var thisType = component.GetType();

            if (thisType == typeof(NetworkView)) //speedup
                return;
            //get all the methods of the derived type
            MethodInfo[] methods = thisType.GetMethods(
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy
                );

            foreach (MethodInfo method in methods)
            {
                var tokens = Attribute.GetCustomAttributes(method, typeof(RpcAttribute), false) as RpcAttribute[];

                foreach (var token in tokens)
                {

                    if (token == null)
                        continue;

                    Action<NetIncomingMessage, NetMessageInfo> del = Delegate.CreateDelegate(typeof(Action<NetIncomingMessage, NetMessageInfo>), component, method, false) as Action<NetIncomingMessage, NetMessageInfo>;

                    if (del != null)
                        SubscribeToRPC(token.rpcId, del, defaultContinueForwarding: token.defaultContinueForwarding);
                    else
                        Debug.LogWarning("The method {0} for type {1} does not match the RPC delegate of Action<NetIncomingMessage, NetMessageInfo>, but is marked to process RPC's. Please either fix this method, or remove the attribute",
                            method.Name,
                            method.DeclaringType.Name
                            );
                }
            }
        }

        /// <summary>
        /// Subscribe all the methods marked with the RPC attribute attached to the same gameobject as this network view
        /// </summary>
        public void SubscribeMarkedRPCs()
        {
            var components = gameObject.GetComponents<Component>();

            foreach (var component in components)
            {
                SubscribeMarkedRPCsOnComponent(component);
            }
        }

        #endregion

        #region RPC Processing
        private readonly Dictionary<byte, RPCProcessor> _rpcProcessors = new Dictionary<byte, RPCProcessor>();

        /// <summary>
        /// Subscribe to an rpc
        /// </summary>
        /// <param name="rpcID">id of the rpc</param>
        /// <param name="rpcProcessor">action to process the rpc with</param>
        /// <param name="overwriteExisting">overwrite the existing processor if one exists.</param>
        /// <param name="defaultContinueForwarding">default value for info.continueForwarding</param>
        /// <returns>Whether or not the rpc was subscribed to. Will return false if an existing rpc was attempted to be subscribed to, and overwriteexisting was set to false</returns>
        public bool SubscribeToRPC(byte rpcID, Action<NetIncomingMessage, NetMessageInfo> rpcProcessor, bool overwriteExisting = true, bool defaultContinueForwarding = true)
        {
            if (rpcProcessor == null)
                throw new ArgumentNullException("rpcProcessor", "the processor delegate cannot be null");
            if (overwriteExisting)
            {
                _rpcProcessors[rpcID] = new RPCProcessor(rpcProcessor, defaultContinueForwarding);
                return true;
            }
            else
            {
                if (_rpcProcessors.ContainsKey(rpcID))
                {
                    return false;
                }
                else
                {
                    _rpcProcessors.Add(rpcID, new RPCProcessor(rpcProcessor, defaultContinueForwarding));
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

        internal void CallRPC(byte rpcID, NetIncomingMessage message, NetMessageInfo info)
        {
            RPCProcessor processor;
            if (_rpcProcessors.TryGetValue(rpcID, out processor))
            {
                info.continueForwarding = processor.DefaultContinueForwarding;
                if (processor.Method != null)
                    processor.Method(message, info);
                else
                {
                    //Debug.LogWarning("RPC processor for {0} was null. Automatically cleaning up. Please be sure to clean up after yourself in the future.", rpcID);
                    _rpcProcessors.Remove(rpcID);
                }
            }
            else
            {
                Debug.LogWarning("Networkview on {1}: unhandled RPC {0}", rpcID, gameObject.Name);
                info.continueForwarding = false;
            }
        }

        #endregion

        #region RPC sending
        /// <summary>
        /// Send a message to the specified recipients
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, RPCMode mode, params INetSerializable[] args)
        {
            if (connections.Count == 0)
                return;

            var size = 3;
            RPCUtils.AllocSize(ref size, args);

            var message = PNetServer.peer.CreateMessage(size);
            message.Write(viewID.guid);
            message.Write(rpcID);
            RPCUtils.WriteParams(ref message, args);

            if (mode == RPCMode.AllBuffered || mode == RPCMode.OthersBuffered)
            {
                Buffer(message);
            }

            SendMessage(message, mode);
        }

        /// <summary>
        /// send a message to the specified player
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="player"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, Player player, params INetSerializable[] args)
        {
            if (connections.Count == 0)
                return;

            var size = 2;
            RPCUtils.AllocSize(ref size, args);

            var message = PNetServer.peer.CreateMessage(size);
            message.Write(viewID.guid);
            message.Write(rpcID);
            RPCUtils.WriteParams(ref message, args);

            PNetServer.peer.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
        }

        #endregion

        #region Serialization
        /// <summary>
        /// Time between each stream serialization
        /// Minimum time is 0.01 (1/100 second)
        /// </summary>
        public float SerializationTime = 0.05f;

        /// <summary>
        /// subscribe to this in order to deserialize streaming data. Player is the sender
        /// </summary>
        public Action<NetIncomingMessage, Player> OnDeserializeStream = delegate { };

        

        /// <summary>
        /// Set the method to be used when serializing to the stream
        /// </summary>
        /// <param name="newMethod"></param>
        /// <param name="defaultSize">size of the stream. Helps prevent array resizing when writing</param>
        /// <param name="skipOwner">whether or not to skip sending to the owner</param>
        public void SetSerializationMethod(Action<NetOutgoingMessage> newMethod, int defaultSize = 16, bool skipOwner = true)
        {
            if (newMethod != null)
            {
                OnSerializeStream = newMethod;
                DefaultStreamSize = defaultSize;
                this.skipSerializationToOwner = skipOwner;
            }
        }
        Action<NetOutgoingMessage> OnSerializeStream = delegate { };
        bool skipSerializationToOwner = true;

        /// <summary>
        /// size of the stream. helps prevent array resizing when writing
        /// </summary>
        public int DefaultStreamSize = 16;

        private NetworkStateSynchronization m_StateSynchronization = NetworkStateSynchronization.Off;
        private bool m_IsSerializing = false;
        /// <summary>
        /// The method of synchronization. This should be the same on the client and server
        /// </summary>
        public NetworkStateSynchronization StateSynchronization
        {
            get
            {
                return m_StateSynchronization;
            }
            set
            {
                m_StateSynchronization = value;
                if (m_StateSynchronization != NetworkStateSynchronization.Off && !m_IsSerializing)
                {
                    m_IsSerializing = true;
                    StartCoroutine(Serialize(), true);
                }
                else if (m_StateSynchronization == NetworkStateSynchronization.Off && m_IsSerializing)
                {
                    m_IsSerializing = false;
                }
            }
        }

        /// <summary>
        /// custom processing of which connections Serialization should send to
        /// This gets run every serialize tick. Make sure its efficient.
        /// </summary>
        public Func<List<NetConnection>, List<NetConnection>> ProcessSerializationConnections = null;

        IEnumerator<YieldInstruction> Serialize()
        {
            while (m_IsSerializing)
            {
                if (SerializationTime < 0.01f)
                    SerializationTime = 0.01f;

                if (owner == Player.Server || owner.connection.Status == NetConnectionStatus.Connected)
                {
                    var nMessage = PNetServer.peer.CreateMessage(DefaultStreamSize);
                    nMessage.Write(viewID.guid);
                    OnSerializeStream(nMessage);

                    //TODO: figure out connections to send to, then send them

                    List<NetConnection> conns;

                    if (ProcessSerializationConnections != null)
                        conns = ProcessSerializationConnections(skipSerializationToOwner ? allButOwner : connections);
                    else
                        conns = skipSerializationToOwner ? allButOwner : connections;

                    if (StateSynchronization == NetworkStateSynchronization.Unreliable)
                    {
                        if (conns.Count > 0)
                            PNetServer.peer.SendMessage(nMessage, conns, NetDeliveryMethod.Unreliable, Channels.UNRELIABLE_STREAM);
                    }
                    else if (StateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed)
                    {
                        if (conns.Count > 0)
                            PNetServer.peer.SendMessage(nMessage, conns, NetDeliveryMethod.ReliableOrdered, Channels.RELIABLE_STREAM);
                    }
                }
                yield return new WaitForSeconds(SerializationTime);
            }
        }

        #endregion

        #region RPC messages and buffers
        List<NetBuffer> buffer = new List<NetBuffer>(4);

        //don't need to keep track of the mode, because the clients don't care about it, 
        //and technically, neither do we (everyone new needs it anyway, so 'all' and 'other' are identical)
        internal void Buffer(NetBuffer msg)
        {
            var toBuffer = new NetBuffer();
            msg.Clone(toBuffer);
            buffer.Add(toBuffer);
        }

        internal void SendBuffer(Player player)
        {
            foreach (var b in buffer)
            {
                var message = PNetServer.peer.CreateMessage();
                b.Clone(message);
                Debug.Log("Sending buffered message to player " + player.Id);
                PNetServer.peer.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
            }

            foreach (var b in fieldBuffer.Values)
            {
                var message = PNetServer.peer.CreateMessage();
                b.Clone(message);

                PNetServer.peer.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, Channels.SYNCHED_FIELD);
            }
        }

        /// <summary>
        /// Remove all the buffered RPC's, so new players will no longer receive them
        /// </summary>
        public void ClearBuffer()
        {
            buffer.Clear();
        }

        internal void Send(NetBuffer msg, RPCMode mode, NetConnection originalSender = null)
        {
            var message = PNetServer.peer.CreateMessage();
            msg.Clone(message);

            SendMessage(message, mode, originalSender);
        }

        private void SendMessage(NetOutgoingMessage msg, RPCMode mode, NetConnection originalSender = null)
        {
            if (mode != RPCMode.Owner)
            {
                if (mode == RPCMode.All || mode == RPCMode.AllBuffered)
                    PNetServer.peer.SendMessage(msg, connections, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
                else
                {
                    var conns = connections.Where(c => c != originalSender).ToList();
                    if (conns.Count != 0)
                        PNetServer.peer.SendMessage(msg, conns, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
                }
            }
            else
            {
                PNetServer.peer.SendMessage(msg, owner.connection, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
            }
        }

        #endregion

        #region Synchronized fields
        Dictionary<byte, NetBuffer> fieldBuffer = new Dictionary<byte, NetBuffer>();

        internal void BufferField(NetIncomingMessage msg, byte fieldId)
        {
            var toBuffer = new NetBuffer();
            msg.Clone(toBuffer);
            fieldBuffer[fieldId] = toBuffer;
        }

        //fields are basically treated like OthersBuffered
        internal void SendField(NetBuffer msg, NetConnection originalSender = null)
        {
            var message = PNetServer.peer.CreateMessage();
            msg.Clone(message);

            var conns = connections.Where(c => c != originalSender).ToList();
            if (conns.Count != 0)
                PNetServer.peer.SendMessage(message, conns, NetDeliveryMethod.ReliableOrdered, Channels.SYNCHED_FIELD);
        }
        #endregion
    }
}
