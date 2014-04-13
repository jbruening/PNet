using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        internal NetClient Peer;
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
            EngineHook = engineHook;
            NetworkViewManager = new NetworkViewManager(this);
        }

        /// <summary>
        /// Connect to the specified ip on the specified port
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="bindport">port to actually listen on. Default is just the first available port</param>
        [Obsolete("Use the overload that takes a ClientConfiguration")]
        public void Connect(string ip, int port, int bindport = 0)
        {
            Configuration = new ClientConfiguration(ip, port, bindport);
            Connect(Configuration);
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

            if (EngineHook == null) 
                throw new Exception("Cannot have a null EngineHook");
            
            EngineHook.EngineUpdate += Update;
            _shutdownQueued = false;
            _config = new NetPeerConfiguration(Configuration.AppIdentifier);
            _config.Port =  Configuration.BindPort; //so we can run client and server on the same machine..

            Peer = new NetClient(_config);

            Peer.Start();

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
        /// Run once the room changing has completed (tells the server you're actually ready to be in a room)
        /// </summary>
        public void FinishedRoomChange()
        {
            var message = Peer.CreateMessage(1);

            message.Write(RPCUtils.FinishedRoomChange);
            Peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
        }

        private void FinishedInstantiate(ushort netID)
        {
            NetOutgoingMessage msg = Peer.CreateMessage(3);
            msg.Write(RPCUtils.FinishedInstantiate);
            msg.Write(netID);

            Peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
            Debug.Log(this, "Finished instantiation, sending ack");
        }

        private void ProcessUtils(NetIncomingMessage msg)
        {
            var utilId = msg.ReadByte();

            if (utilId == RPCUtils.TimeUpdate)
            {

            }
            else if (utilId == RPCUtils.Instantiate)
            {
                //read the path...
                var resourcePath = msg.ReadString();
                var viewId = msg.ReadUInt16();
                var ownerId = msg.ReadUInt16();

                var position = new Vector3();
                position.OnDeserialize(msg);
                var rotation = new Quaternion();
                rotation.OnDeserialize(msg);

                var view = NetworkViewManager.Create(viewId, ownerId);

                

                object netviewContainer = null;

                try
                {
                     netviewContainer = EngineHook.Instantiate(resourcePath, view, position, rotation);
                }
                catch(Exception e)
                {
                    Debug.LogError(this, "[EngineHook.Instantiate] {0}", e);
                }
                view.Container = netviewContainer;

                Debug.Log(this, "Created {0}", view);

                view.DoOnFinishedCreation();
                
                FinishedInstantiate(viewId);
            }
            else if (utilId == RPCUtils.Remove)
            {
                var viewId = msg.ReadUInt16();
                byte reasonCode;
                if (!msg.ReadByte(out reasonCode))
                    reasonCode = 0;

                NetworkView find;
                if (NetworkViewManager.Find(viewId, out find))
                {
                    find.DoOnRemove(reasonCode);
                }
            }
            else if (utilId == RPCUtils.ChangeRoom)
            {
                var newRoom = msg.ReadString();

                Debug.Log(this, "Changing to room {0}", newRoom);

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
                var addToId = msg.ReadUInt16();
                var idToAdd = msg.ReadUInt16();
                string customFunction;
                msg.ReadString(out customFunction);


                NetworkView view;
                if (NetworkViewManager.Find(addToId, out view))
                {
                    var newView = NetworkViewManager.Create(idToAdd, view.OwnerId);

                    object container = null;
                    try
                    {
                        container = EngineHook.AddNetworkView(view, newView, customFunction);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(this, "[EngineHook.AddNetworkView] {0}", e);
                    }
                    if (container != null)
                    {
                        newView.Container = container;
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

        private bool _shutdownQueued = false;

        void Update()
        {
            if (!IsMessageQueueRunning) return;
            Time = NetTime.Now;
            if (Peer == null) return; //in case something is running update before we've even tried to connect
            var messages = new List<NetIncomingMessage>();
            int counter = Peer.ReadMessages(messages);

            if (_shutdownQueued)
            {
                if (Peer.Status == NetPeerStatus.NotRunning)
                    FinalizeDisconnect();
                else if (Peer.Status == NetPeerStatus.Running)
                {
                    //let's gracefully close things up.
                    Peer.Shutdown(StatusReason);
                }
            }

            //for loops are way faster with lists than foreach
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
                else if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    Debug.LogWarning(this, msg.ReadString());
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionLatencyUpdated)
                {
                    Latency = msg.ReadFloat();
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ErrorMessage)
                {
                    Debug.LogError(this, msg.ReadString());
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var lastStatus = _status;
                    Status = (NetConnectionStatus) msg.ReadByte();
                    StatusReason = msg.ReadString();
#if DEBUG
                    StatusReason = string.Format("{0} changed to {1}: {2}", lastStatus, Status, msg.RemainingBits > 0 ? msg.ReadString() : "");
#endif
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
        }

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
                    var actorId = msg.ReadUInt16();
                    NetworkView find;
                    if (NetworkViewManager.Find(actorId, out find))
                        find.DoOnDeserializeStream(msg);
                }
                else if (msg.SequenceChannel == Channels.RELIABLE_STREAM)
                {
                    var actorId = msg.ReadUInt16();
                    NetworkView find;
                    if (NetworkViewManager.Find(actorId, out find))
                        find.DoOnDeserializeStream(msg);
                }
                else if (msg.SequenceChannel >= Channels.BEGIN_RPCMODES && msg.SequenceChannel <= Channels.OWNER_RPC)
                {
                    //rpc...
                    var viewID = msg.ReadUInt16();
                    var rpcId = msg.ReadByte();
                    NetworkView find;
                    if (NetworkViewManager.Find(viewID, out find))
                        find.CallRPC(rpcId, msg);
                    else
                        Debug.LogWarning(this, "couldn't find view " + viewID + " to send rpc " + rpcId);
                }
                else if (msg.SequenceChannel == Channels.SYNCHED_FIELD)
                {
                    var viewId = msg.ReadUInt16();
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
                    var rpcId = msg.ReadByte();
                    if (ProcessRPC != null) ProcessRPC(rpcId, msg);
                }
                else if (msg.SequenceChannel == Channels.STATIC_UTILS)
                {
                    ProcessUtils(msg);
                }
                else
                {
                    Debug.LogWarning(this, "data received over unhandled channel " + msg.SequenceChannel);
                }
            }
            catch (Exception er)
            {
                Debug.LogError(this, "[Net.Consume] {0}", er);
            }
        }
    }
}
