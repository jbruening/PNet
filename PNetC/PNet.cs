using System;
using System.Collections.Generic;
using System.Net;
using Lidgren.Network;
using PNet;
using System.ComponentModel;

namespace PNetC
{
    /// <summary>
    /// Networking class
    /// </summary>
    public class Net
    {
        /// <summary>
        /// When finished connecting to the server
        /// </summary>
        public event Action OnConnectedToServer;

        /// <summary>
        /// When disconnected from the server
        /// </summary>
        public event Action OnDisconnectedFromServer;

        /// <summary>
        /// When we've failed to connect
        /// </summary>
        public event Action<string> OnFailedToConnect;

        /// <summary>
        /// When the room is changing
        /// </summary>
        public event Action<string> OnRoomChange;

        /// <summary>
        /// subscribe to this in order to receive static RPC's from the server. you need to manually process them.
        /// </summary>
        public event Action<byte, NetIncomingMessage> ProcessRPC;

        /// <summary>
        /// When a discovery response is received
        /// </summary>
        public event Action<NetIncomingMessage> OnDiscoveryResponse;

        /// <summary>
        /// The function to use for writing the connect data (username/password/etc)
        /// </summary>
        public Action<NetOutgoingMessage> WriteHailMessage = delegate { };

        /// <summary>
        /// latest status
        /// </summary>
        public NetConnectionStatus Status { 
            get
            {
                return _status;
            } 
            internal set
            {
                _status = value;
                Debug.LogInfo(this, "[Net Status] " + _status);
            }
        }

        /// <summary>
        /// This will cause all the events to clear out. This is mainly for Unity to use. You probably shouldn't use it.
        /// </summary>
        public void CleanupEvents()
        {
            OnConnectedToServer = null;
            OnDisconnectedFromServer = null;
            OnFailedToConnect = null;
            OnRoomChange = null;
            ProcessRPC = null;
            OnDiscoveryResponse = null;
            WriteHailMessage = delegate { };
        }

        private NetConnectionStatus _status = NetConnectionStatus.Disconnected;
        /// <summary>
        /// reason for the most latest status
        /// </summary>
        [DefaultValue("")]
        public string StatusReason { get; private set; }
        /// <summary>
        /// pause the processing of the network queue
        /// </summary>
        public bool IsMessageQueueRunning = true;
        /// <summary>
        /// Network time of this frame
        /// </summary>
        public double Time { get; private set; }

        /// <summary>
        /// PNetServer.Time of the current frame.
        /// Returns 0 if there is no connection
        /// </summary>
        public double ServerTime
        {
            get
            {
                if (Peer.ServerConnection == null) return 0;
                return Peer.ServerConnection.GetRemoteTime(Time);
            }
        }

        internal NetClient Peer;
        internal NetClient RoomPeer;

        /// <summary>
        /// The Network ID of this client
        /// </summary>
        public ushort PlayerId { get; private set; }
        NetPeerConfiguration _config;
        /// <summary>
        /// The hook used to hook into various game engines that you might use
        /// </summary>
        public IEngineHook EngineHook { get; private set; }

        /// <summary>
        /// The container of all the network views
        /// </summary>
        public NetworkViewManager NetworkViewManager { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public Net(IEngineHook engineHook)
        {
            if (engineHook == null)
                throw new ArgumentNullException("engineHook");
            EngineHook = engineHook;
            NetworkViewManager = new NetworkViewManager(this);
        }
        /// <summary>
        /// the current configuration
        /// </summary>
        public ClientConfiguration Configuration { get; private set; }

        /// <summary>
        /// last received latency value from the lidgren's calculations
        /// </summary>
        public float Latency { get; private set; }

        /// <summary>
        /// Connect with the specified configuration
        /// </summary>
        /// <param name="configuration"></param>
        public void Connect(ClientConfiguration configuration)
        {
            Configuration = configuration;
            if (Peer != null && Peer.Status != NetPeerStatus.NotRunning)
            {
                Debug.LogError(this, "cannot connect while peer is running");
                return;
            }
            
            EngineHook.EngineUpdate += Update;
            _shutdownQueued = false;
            _config = new NetPeerConfiguration(Configuration.AppIdentifier);
            _config.Port =  Configuration.BindPort; //so we can run client and server on the same machine..

            var roomConfig = _config.Clone();

            Peer = new NetClient(_config);
            RoomPeer = new NetClient(roomConfig);

            Peer.Start();
            RoomPeer.Start();

            var hailMessage = Peer.CreateMessage();
            WriteHailMessage(hailMessage);
            Peer.Connect(Configuration.Ip, Configuration.Port, hailMessage);
        }

        
        /// <summary>
        /// Disconnect if connected
        /// </summary>
        public void Disconnect()
        {
            if (Peer == null)
                return;

            Peer.Shutdown("disconnecting");
            RoomPeer.Shutdown("CDC");
        }

        /// <summary>
        /// Send an rpc to the server
        /// </summary>
        /// <param name="rpcId"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcId, params INetSerializable[] args)
        {
            var size = 1;
            RPCUtils.AllocSize(ref size, args);

            var message = Peer.CreateMessage(size);
            message.Write(rpcId);
            RPCUtils.WriteParams(ref message, args);

            Peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
        }

        /// <summary>
        /// Send an rpc to the server, in any order
        /// </summary>
        /// <param name="rpcId"></param>
        /// <param name="args"></param>
        public void RPCUnordered(byte rpcId, params INetSerializable[] args)
        {
            var size = 1;
            RPCUtils.AllocSize(ref size, args);

            var message = Peer.CreateMessage(size);
            message.Write(rpcId);
            RPCUtils.WriteParams(ref message, args);

            Peer.SendMessage(message, NetDeliveryMethod.ReliableUnordered, Channels.STATIC_RPC_UNORDERED);
        }

        private int _roomPort;
        private readonly byte[] _roomKey = new byte[16];
        private bool _roomChangeQueued = false;
        private bool _roomChangeCompleteQueued = false;
        /// <summary>
        /// Run once the room changing has completed (tells the server you're actually ready to be in a room)
        /// </summary>
        public void FinishedRoomChange()
        {
            if (RoomPeer == null)
                throw new InvalidOperationException("Cannot switch rooms before connecting to a server");
            if (RoomPeer.Status != NetPeerStatus.Running)
                throw new InvalidOperationException("Cannot switch rooms while not running");
            if (Peer.ConnectionStatus != NetConnectionStatus.Connected)
                throw new InvalidOperationException("Cannot switch rooms when not connected to a server");

            if (!_roomChangeQueued)
                throw new InvalidOperationException("Cannot switch rooms when no room change is queued");
            _roomChangeQueued = false;
            _roomChangeCompleteQueued = true;
        }

        private void FinishedInstantiate(NetworkViewId netId)
        {
            var msg = Peer.CreateMessage(3);
            msg.Write(RPCUtils.FinishedInstantiate);
            netId.OnSerialize(msg);

            Peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
            Debug.Log(this, "Finished instantiation, sending ack");
        }

        private void ProcessUtils(NetIncomingMessage msg)
        {
            var utilId = msg.ReadByte();

            if (utilId == RPCUtils.TimeUpdate)
            {
                throw new NotImplementedException("RPCUtils.TimeUpdate");
            }
            else if (utilId == RPCUtils.Instantiate)
            {
                //read the path...
                var resourcePath = msg.ReadString();
                var viewId = NetworkViewId.Deserialize(msg);
                var ownerId = msg.ReadUInt16();

                var position = new Vector3();
                position.OnDeserialize(msg);
                var rotation = new Quaternion();
                rotation.OnDeserialize(msg);

                var view = NetworkViewManager.Create(viewId, ownerId);

                try
                {
                    EngineHook.Instantiate(resourcePath, view, position, rotation);
                }
                catch(Exception e)
                {
                    Debug.LogError(this, "[EngineHook.Instantiate] {0}", e);
                }

                Debug.Log(this, "Created {0}", view);

                view.DoOnFinishedCreation();
                
                FinishedInstantiate(viewId);
            }
            else if (utilId == RPCUtils.Remove)
            {

                var viewId = new NetworkViewId();
                viewId.OnDeserialize(msg);
                byte reasonCode;
                if (!msg.ReadByte(out reasonCode))
                    reasonCode = 0;

                NetworkView find;
                if (NetworkViewManager.Find(viewId, out find))
                {
                    find.DoOnRemove(reasonCode);
                }
                else
                {
                    Debug.LogError(this, "Attempted to remove {0}, but it could not be found", viewId);
                }
            }
            else if (utilId == RPCUtils.ChangeRoom)
            {
                var newRoom = msg.ReadString();
                _roomPort = msg.ReadInt32();
                _roomChangeQueued = true;
                msg.ReadBytes(_roomKey, 0, 16);

                Debug.LogInfo(this, "Changing to room {0}", newRoom);
                //and disconnect the room peer.
                RoomPeer.Disconnect("SRS");

                if (OnRoomChange != null)
                {
                    try
                    {
                        OnRoomChange(newRoom);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError(this, "[OnChangeRoom] {0}", e);
                    }
                }

                if (Configuration.DeleteNetworkInstantiatesOnRoomChange)
                {
                    NetworkViewManager.DestroyAllViews();
                }
            }
            else if (utilId == RPCUtils.AddView)
            {
                var addToId = NetworkViewId.Deserialize(msg);
                var idToAdd = NetworkViewId.Deserialize(msg);
                string customFunction;
                msg.ReadString(out customFunction);


                NetworkView view;
                if (NetworkViewManager.Find(addToId, out view))
                {
                    var newView = NetworkViewManager.Create(idToAdd, view.OwnerId);

                    try
                    {
                        EngineHook.AddNetworkView(view, newView, customFunction);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(this, "[EngineHook.AddNetworkView] {0}", e);
                    }
                }
                else
                {
                    Debug.LogError(this, "Attempted to add a network view to id {0}, but it could not be found");
                }
            }
            else if (utilId == RPCUtils.SetPlayerId)
            {
                var playerId = msg.ReadUInt16();
                PlayerId = playerId;
                Debug.LogInfo(this, "Setting player id to " + playerId);
            }
        }

        private bool _shutdownQueued;

        private int _lastFrameCount = 16;
        void Update()
        {
            if (!IsMessageQueueRunning) return;
            Time = NetTime.Now;
            if (Peer == null) return; //in case something is running update before we've even tried to connect
            var messages = new List<NetIncomingMessage>(_lastFrameCount * 2);
            _lastFrameCount = Peer.ReadMessages(messages);

            if (_shutdownQueued)
            {
                if (Peer.Status == NetPeerStatus.NotRunning)
                    FinalizeDisconnect();
                else if (Peer.Status == NetPeerStatus.Running)
                {
                    //let's gracefully close things up.
                    Peer.Shutdown(StatusReason);
                    if (RoomPeer != null)
                        RoomPeer.Shutdown("CCC");
                }
            }

            //for loops are way faster with lists than foreach
// ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];
                //faster than switch, as most will be Data messages.
                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    Consume(msg);
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.DiscoveryResponse)
                {
                    if (OnDiscoveryResponse != null) OnDiscoveryResponse(msg);
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionLatencyUpdated)
                {
                    Latency = msg.ReadFloat();
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.DebugMessage)
                {
                    Debug.LogInfo(this, msg.ReadString());
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    Debug.LogWarning(this, msg.ReadString());
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var lastStatus = _status;
                    Status = (NetConnectionStatus) msg.ReadByte();
                    StatusReason = msg.ReadString();

                    Debug.LogInfo(this, "Status changed from {0} to {1}: {2}", lastStatus, Status, StatusReason);
                    Peer.Recycle(msg);

                    try
                    {
                        if (_status == NetConnectionStatus.Disconnected)
                        {
                            switch (lastStatus)
                            {
                                case NetConnectionStatus.Disconnecting:
                                case NetConnectionStatus.Connected:
                                    _shutdownQueued = true;
                                    break;
                                default:
                                    if (OnFailedToConnect != null)
                                        OnFailedToConnect(StatusReason);
                                    break;
                            }
                        }
                        else if (_status == NetConnectionStatus.Connected)
                        {
                            if (OnConnectedToServer != null)
                                OnConnectedToServer();
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(this, "[Net.Update.StatusChanged] {0}", e);
                    }
                }
                else if (msg.MessageType == NetIncomingMessageType.Error)
                {
                    Debug.LogError(this, msg.ReadString()); //this should really never happen...
                    Peer.Recycle(msg);
                }
                else
                    Peer.Recycle(msg);
            }

            UpdateRoom();
        }

        private int _lastRoomFrameCount;
        void UpdateRoom()
        {
            if (RoomPeer == null) return;

            var messages = new List<NetIncomingMessage>(_lastRoomFrameCount * 2);
            _lastRoomFrameCount = RoomPeer.ReadMessages(messages);

            //for loops are way faster with lists than foreach
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];
                //faster than switch, as most will be Data messages.
                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    Consume(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.DebugMessage)
                {
                    Debug.LogInfo(this, "[Room] {0}", msg.ReadString());
                }
                else if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    Debug.LogWarning(this, "[Room] {0}", msg.ReadString());
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var lastStatus = RoomStatus;
                    RoomStatus = (NetConnectionStatus)msg.ReadByte();
                    RoomStatusReason = msg.ReadString();

                    Debug.LogInfo(this, "Room status changed from {0} to {1}: {2}", lastStatus, RoomStatus, RoomStatusReason);

                }
                else if (msg.MessageType == NetIncomingMessageType.Error)
                {
                    Debug.LogError(this, "[Room] {0}", msg.ReadString()); //this should really never happen...
                }

                RoomPeer.Recycle(msg);
            }

            //actually switch to the room on the server.
            if (_roomChangeCompleteQueued && RoomPeer.ConnectionStatus == NetConnectionStatus.Disconnected)
            {
                _roomChangeCompleteQueued = false;
                
                var message = RoomPeer.CreateMessage(18);
                message.Write(PlayerId);
                message.Write(_roomKey);
                //faster to use existing information about connection rather than from config.
                var endPoint = new IPEndPoint(Peer.ServerConnection.RemoteEndPoint.Address, _roomPort);
                RoomPeer.Connect(endPoint, message);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public string RoomStatusReason { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        public NetConnectionStatus RoomStatus { get; private set; }

        void FinalizeDisconnect()
        {
            _shutdownQueued = false;

            if (OnDisconnectedFromServer != null)
                OnDisconnectedFromServer();

            if (Configuration.DeleteNetworkInstantiatesOnDisconnect)
            {
                NetworkViewManager.DestroyAllViews();
            }
        }

        internal NetOutgoingMessage CreateMessage(int initialCapacity)
        {
            return Peer.CreateMessage(initialCapacity);
        }

        private void Consume(NetIncomingMessage msg)
        {
            try
            {
                //faster than switch, as this is in most to least common order
                if (msg.SequenceChannel == Channels.UNRELIABLE_STREAM)
                {
                    if (msg.DeliveryMethod == NetDeliveryMethod.ReliableUnordered)
                    {
                        HandleStaticRpc(msg);
                    }
                    else
                    {
                        var actorId = NetworkViewId.Deserialize(msg);
                        NetworkView find;
                        if (NetworkViewManager.Find(actorId, out find))
                            find.DoOnDeserializeStream(msg);
                    }
                }
                else if (msg.SequenceChannel == Channels.RELIABLE_STREAM)
                {
                    var actorId = NetworkViewId.Deserialize(msg);
                    NetworkView find;
                    if (NetworkViewManager.Find(actorId, out find))
                        find.DoOnDeserializeStream(msg);
                }
                else if (msg.SequenceChannel >= Channels.BEGIN_RPCMODES && msg.SequenceChannel <= Channels.OWNER_RPC)
                {
                    //rpc...
                    var viewId = NetworkViewId.Deserialize(msg);
                    var rpcId = msg.ReadByte();
                    NetworkView find;
                    if (NetworkViewManager.Find(viewId, out find))
                        find.CallRPC(rpcId, msg);
                    else
                        Debug.LogWarning(this, "couldn't find view {0} to send rpc {1}", viewId, rpcId);
                }
                else if (msg.SequenceChannel == Channels.SYNCHED_FIELD)
                {
                    var viewId = NetworkViewId.Deserialize(msg);
                    var fieldId = msg.ReadByte();
                    NetworkView find;
                    if (NetworkViewManager.Find(viewId, out find))
                        find.SetSynchronizedField(fieldId, msg);
                    else
                        Debug.LogWarning(this, "couldn't find view " + viewId + " to set field " + fieldId);
                }
                else if (msg.SequenceChannel == Channels.OBJECT_RPC)
                {
                    var viewId = msg.ReadUInt16();
                    var rpcId = msg.ReadByte();
                    NetworkedSceneObject.CallRPC(viewId, rpcId, msg);
                }
                else if (msg.SequenceChannel == Channels.STATIC_RPC)
                {
                    HandleStaticRpc(msg);
                }
                else if (msg.SequenceChannel == Channels.STATIC_UTILS)
                {
                    ProcessUtils(msg);
                }
                else
                {
                    Debug.LogWarning(this, "{1} bytes received over unhandled channel {0}, delivery {2}", msg.SequenceChannel, msg.LengthBytes, msg.DeliveryMethod);
                }
            }
            catch (Exception er)
            {
                Debug.LogError(this, "[Net.Consume] {0}", er);
            }
        }

        void HandleStaticRpc(NetIncomingMessage msg)
        {
            var rpcId = msg.ReadByte();
            if (ProcessRPC != null) ProcessRPC(rpcId, msg);
        }
    }
}
