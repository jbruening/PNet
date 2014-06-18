using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Yaml.Serialization;
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
        [YamlSerialize(YamlSerializeMethod.Never)]
        public Player owner
        {
            get { return _owner; }
            internal set
            {
                _owner = value;

                if (_isVisibleToAll)
                {
                    foreach (var player in room.players)
                    {
                        _connections.Add(player.connection);
                        if (player != _owner)
                            _allButOwner.Add(player.connection);
                    }
                }
                else
                {
                    if (_owner != Player.Server)
                        _connections.Add(_owner.connection);
                }
            }
        }
        private Player _owner;

        /// <summary>
        /// The room this is in
        /// </summary>
        [YamlSerialize(YamlSerializeMethod.Never)]
        public Room room { get { return gameObject.Room; } }

        /// <summary>
        /// the identifier for the network view
        /// </summary>
        [YamlSerialize(YamlSerializeMethod.Never)]
        public NetworkViewId viewID = NetworkViewId.Zero;
        
        private readonly List<NetConnection> _connections = new List<NetConnection>();
        [YamlSerialize(YamlSerializeMethod.Never)]
        internal List<NetConnection> Connections { get { return _connections; } }
        private readonly List<NetConnection> _allButOwner = new List<NetConnection>();
        [YamlSerialize(YamlSerializeMethod.Never)]
        internal List<NetConnection> AllButOwner { get { return _allButOwner; } }

        /// <summary>
        /// Whether or not this object is visible to all players
        /// if you set this to false, from being true, it will merely prevent it from showing up on any new players
        /// use ClearSubscriptions if you want to remove it from all players
        /// </summary>
        [YamlSerialize(YamlSerializeMethod.Never)]
        public bool IsVisibleToAll
        {
            get { return _isVisibleToAll; }
            set
            {
                if (!_isVisibleToAll && value)
                {
                    var playersToAdd = new List<NetConnection>(room.players.Count);
                    for (var i = 0; i < room.players.Count; i++)
                    {
                        if (!_connections.Contains(room.players[i].connection))
                            playersToAdd.Add(room.players[i].connection);
                    }

                    room.SendNetworkInstantiate(playersToAdd, gameObject);
                    _connections.AddRange(playersToAdd);
                }
            }
        }
        [YamlSerialize(YamlSerializeMethod.Assign)]
        private bool _isVisibleToAll = true;
        
        /// <summary>
        /// Subscribe a player to this
        /// </summary>
        /// <param name="player"></param>
        /// <param name="isSubscribed"></param>
        public void SetPlayerSubscription(Player player, bool isSubscribed)
        {
            if (_isVisibleToAll)
                return; //the object is already visible to the specified player.

            if (player == owner)
            {
                Debug.LogWarning("[NetworkView.SetPlayerSubscription] Players are always subscribed to NetworkViews they own. Do not subscribe them to it.");
                return;
            }

            if (player.CurrentRoom != room)
            {
                Debug.LogWarning("[NetworkView.SetPlayerSubscription] Players cannot be subscribed to objects not in the same room as them.");
                return;
            }

            if (isSubscribed)
            {
                if (_connections.Contains(player.connection))
                    return; //player is already subscribed
                
                _connections.Add(player.connection);
                _allButOwner.Add(player.connection);

                room.SendNetworkInstantiate(new List<NetConnection> {player.connection}, gameObject);
            }
            else
            {
                OnPlayerLeftRoom(player);
                DestroyOnPlayer(player);
            }
        }

        /// <summary>
        /// removes the object from all current players, except the owner, and clears the subscriptions
        /// </summary>
        public void ClearSubscriptions()
        {
            var message = PNetServer.peer.CreateMessage(3);
            message.Write(PNet.RPCUtils.Remove);
            message.Write(viewID.guid);

            if (_allButOwner.Count > 0)
            {
                PNetServer.peer.SendMessage(message, _allButOwner, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
            }

            _allButOwner.Clear();
            _connections.Clear();
            if (owner != Player.Server)
                _connections.Add(owner.connection);
        }

        void DestroyOnPlayer(Player player)
        {
            player.RPC(PNet.RPCUtils.Remove, viewID);
        }

        private void Destroy()
        {
            _connections.Clear();
            _allButOwner.Clear();
        }

        void OnPlayerEnteredRoom(Player player)
        {
            if (_isVisibleToAll)
            {
                //we already have the player added to the connections list, as it was set when we set the owner.
                if (owner == player)
                    return;

                _connections.Add(player.connection);
                _allButOwner.Add(player.connection);
            }
        }
        void OnPlayerLeftRoom(Player player)
        {
            _connections.Remove(player.connection);
            
            if (player != owner)
                _allButOwner.Remove(player.connection);
        }

        private void OnInstantiationFinished(Player player)
        {
            //get the player up to speed
            SendBuffer(player);


            //get all the secondary views and send them
            if (IsSecondaryView) return;
            var conn = new List<NetConnection> {player.connection};

            foreach (var nview in gameObject.GetComponents<NetworkView>().Where(n => n != this))
            {
                SendSecondaryView(nview, conn);
            }
        }
        /// <summary>
        /// cleaning up
        /// </summary>
        protected override void Disposing()
        {
            _rpcProcessors.Clear();

            RemoveView(this);
        }

        /// <summary>
        /// Add another network view to the same gameobject on the server and all clients
        /// </summary>
        /// <param name="customFunctionOnClient">function to run using SendMessage on the gameobject this is added to on the client. The function should have NetworkView as its parameter</param>
        /// <returns></returns>
        public NetworkView AddNetworkedNetworkView(string customFunctionOnClient = null)
        {
            var nView = gameObject.AddComponent<NetworkView>();
            nView.IsSecondaryView = true;
            nView._customSecondaryFunction = customFunctionOnClient;
            RegisterNewView(ref nView);

            SendSecondaryView(nView, _connections);
            return nView;
        }

        /// <summary>
        /// whether or not this is a network view added secondarily
        /// </summary>
        public bool IsSecondaryView { get; private set; }

        private string _customSecondaryFunction;

        void SendSecondaryView(NetworkView newView, List<NetConnection> conns)
        {
            var message = PNetServer.peer.CreateMessage();

            message.Write(RPCUtils.AddView);
            message.Write(viewID.guid);
            message.Write(newView.viewID.guid);
            if (newView._customSecondaryFunction != null)
                message.Write(newView._customSecondaryFunction);

            if (conns.Count > 0)
                PNetServer.peer.SendMessage(message, conns, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
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
                    StartCoroutine(Serialize());
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
                    List<NetConnection> conns;

                    if (ProcessSerializationConnections != null)
                        conns = ProcessSerializationConnections(skipSerializationToOwner ? _allButOwner : _connections);
                    else
                        conns = skipSerializationToOwner ? _allButOwner : _connections;

                    if (conns.Count > 0)
                    {
                        var nMessage = PNetServer.peer.CreateMessage(DefaultStreamSize);
                        nMessage.Write(viewID.guid);
                        OnSerializeStream(nMessage);

                        if (StateSynchronization == NetworkStateSynchronization.Unreliable)
                        {
                            PNetServer.peer.SendMessage(nMessage, conns, NetDeliveryMethod.Unreliable, Channels.UNRELIABLE_STREAM);
                        }
                        else if (StateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed)
                        {
                            PNetServer.peer.SendMessage(nMessage, conns, NetDeliveryMethod.ReliableOrdered, Channels.RELIABLE_STREAM);
                        }
                        else
                        {
                            Debug.LogError("A networkview is set to the serialization type {0}. This is not a valid serialization type.", StateSynchronization);
                        }
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
            for(int i = 0; i < buffer.Count; i++)
            {
                var message = PNetServer.peer.CreateMessage();
                buffer[i].Clone(message);
                Debug.Log("Sending buffered message to player " + player.Id);
                PNetServer.peer.SendMessage(message, player.connection, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
            }

            foreach (var b in fieldBuffer)
            {
                var message = PNetServer.peer.CreateMessage();
                b.Value.Clone(message);

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
                //all and other are identical if originalsender is null.
                if ((mode == RPCMode.All || mode == RPCMode.AllBuffered || originalSender == null) && _connections.Count > 0)
                    PNetServer.peer.SendMessage(msg, _connections, mode.GetDeliveryMethod(), Channels.OWNER_RPC);
                else
                {
                    if (_allButOwner.Count != 0)
                        PNetServer.peer.SendMessage(msg, _allButOwner, mode.GetDeliveryMethod(), Channels.OWNER_RPC);
                }
            }
            else
            {
                PNetServer.peer.SendMessage(msg, owner.connection, mode.GetDeliveryMethod(), Channels.OWNER_RPC);
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

            var conns = _connections.Where(c => c != originalSender).ToList();
            if (conns.Count != 0)
                PNetServer.peer.SendMessage(message, conns, NetDeliveryMethod.ReliableOrdered, Channels.SYNCHED_FIELD);
        }
        #endregion
    }
}
