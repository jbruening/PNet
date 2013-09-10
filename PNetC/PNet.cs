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
        public static event Action OnConnectedToServer;

        /// <summary>
        /// When disconnected from the server
        /// </summary>
        public static event Action OnDisconnectedFromServer;

        /// <summary>
        /// When we've failed to connect
        /// </summary>
        public static event Action<string> OnFailedToConnect;

        /// <summary>
        /// When the room is changing
        /// </summary>
        public static event Action<string> OnRoomChange;

        /// <summary>
        /// subscribe to this in order to receive static RPC's from the server. you need to manually process them.
        /// </summary>
        public static event Action<byte, NetIncomingMessage> ProcessRPC;

        /// <summary>
        /// When a discovery response is received
        /// </summary>
        public static event Action<NetIncomingMessage> OnDiscoveryResponse;
        /// <summary>
        /// logging level. UNUSED
        /// </summary>
        public static NetworkLogLevel LogLevel;
        /// <summary>
        /// latest status
        /// </summary>
        public static NetConnectionStatus Status { 
            get
            {
                return _status;
            } 
            internal set
            {
                _status = value;
                Debug.Log("[Net Status] " + _status);
            }
        }
        private static NetConnectionStatus _status = NetConnectionStatus.Disconnected;
        /// <summary>
        /// reason for the most latest status
        /// </summary>
        [DefaultValue("")]
        public static string StatusReason { get; internal set; }
        /// <summary>
        /// pause the processing of the network queue
        /// </summary>
        public static bool IsMessageQueueRunning = true;
        /// <summary>
        /// Not currently set
        /// </summary>
        public static double Time { get; internal set; }
        /// <summary>
        /// The function to use for writing the connect data (username/password/etc)
        /// </summary>
        public static Action<NetOutgoingMessage> WriteHailMessage = delegate { };

        internal static NetClient Peer;
        protected static NetClient NetClient{get { return Peer; }}
        internal static ushort PlayerId;
        static NetPeerConfiguration _config;
        protected static IEngineHook SingletonEngineHook;

        static Net()
        {
            Status = NetConnectionStatus.Disconnected;
        }

        /// <summary>
        /// Connect to the specified ip on the specified port
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="port"></param>
        /// <param name="bindport">port to actually listen on. Default is just the first available port</param>
        [Obsolete("Use the overload that takes a ClientConfiguration")]
        public static void Connect(string ip, int port, int bindport = 0)
        {
            Configuration = new ClientConfiguration(ip, port, bindport);
            Connect(Configuration);
        }
        /// <summary>
        /// the current configuration
        /// </summary>
        public static ClientConfiguration Configuration { get; protected set; }

        /// <summary>
        /// last received latency value from the lidgren's calculations
        /// </summary>
        public static float Latency { get; private set; }

        /// <summary>
        /// Connect with the specified configuration
        /// </summary>
        /// <param name="configuration"></param>
        public static void Connect(ClientConfiguration configuration)
        {
            Configuration = configuration;
            if (Peer != null && Peer.Status != NetPeerStatus.NotRunning)
            {
                Debug.LogError("cannot connect while connected");
                return;
            }

            SingletonEngineHook = EngineHookFactory.DoCreateEngineHook();
            if (SingletonEngineHook == null) throw new Exception("The EngineHookFactory returned null. It should return an instance of IEngineHook. Make sure you've subscribed a function to EngineHookFactory.CreateEngineHook");
            SingletonEngineHook.EngineUpdate += Update;

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
        public static void Disconnect()
        {
            if (Peer == null)
                return;

            Peer.Shutdown("disconnecting");

            Status = NetConnectionStatus.Disconnected;
            StatusReason = "disconnecting";

            //OnDisconnectedFromServer();
        }

        /// <summary>
        /// Send an rpc to the server
        /// </summary>
        /// <param name="rpcId"></param>
        /// <param name="args"></param>
        public static void RPC(byte rpcId, params INetSerializable[] args)
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
        public static void FinishedRoomChange()
        {
            var message = Peer.CreateMessage(1);

            message.Write(RPCUtils.FinishedRoomChange);
            Peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
        }

        private static void FinishedInstantiate(ushort netID)
        {
            NetOutgoingMessage msg = Peer.CreateMessage(3);
            msg.Write(RPCUtils.FinishedInstantiate);
            msg.Write(netID);

            Peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
            Debug.Log("Finished instantiation, sending ack");
        }

        private static void ProcessUtils(NetIncomingMessage msg)
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

                var view = new NetworkView();
                NetworkView.RegisterView(view, viewId);
                view.ViewID = new NetworkViewId(){guid = viewId, IsMine = PlayerId == ownerId};
                view.OwnerId = ownerId;

                object netviewContainer = null;

                try
                {
                     netviewContainer = SingletonEngineHook.Instantiate(resourcePath, view, position, rotation);
                }
                catch(Exception e)
                {
                    Debug.LogError("[SingletonEngineHook.Instantiate] {0}", e);
                }
                view.Container = netviewContainer;

                view.DoOnFinishedCreation();
                
                FinishedInstantiate(viewId);
            }
            else if (utilId == RPCUtils.Remove)
            {
                var viewId = msg.ReadUInt16();

                NetworkView find;
                if (NetworkView.Find(viewId, out find))
                {
                    find.DoOnRemove();
                }
            }
            else if (utilId == RPCUtils.ChangeRoom)
            {
                var newRoom = msg.ReadString();

                if (OnRoomChange != null)
                {
                    try
                    {
                        OnRoomChange(newRoom);
                    }
                    catch(Exception e)
                    {
                        Debug.LogError("[OnChangeRoom] {0}", e);
                    }
                }

                if (Configuration.DeleteNetworkInstantiatesOnRoomChange)
                {
                    NetworkView.DestroyAllViews();
                }
            }
            else if (utilId == RPCUtils.AddView)
            {
                var addToId = msg.ReadUInt16();
                var idToAdd = msg.ReadUInt16();
                string customFunction;
                var runCustomFunction = msg.ReadString(out customFunction);


                NetworkView view;
                if (NetworkView.Find(addToId, out view))
                {
                    var newView = new NetworkView();
                    NetworkView.RegisterView(newView, idToAdd);
                    newView.ViewID = new NetworkViewId() { guid = idToAdd, IsMine = view.IsMine };
                    newView.IsMine = view.IsMine;
                    newView.OwnerId = view.OwnerId;

                    object container = null;
                    try
                    {
                        container = SingletonEngineHook.AddNetworkView(view, newView, customFunction);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError("[SingletonEngineHook.AddNetworkView] {0}", e);
                    }
                    if (container != null)
                    {
                        newView.Container = container;
                    }
                }
                else
                {
                    Debug.LogError("Attempted to add a network view to id {0}, but it could not be found");
                }
            }
            else if (utilId == RPCUtils.SetPlayerId)
            {
                var playerId = msg.ReadUInt16();
                PlayerId = playerId;
                Debug.Log("Setting player id to " + playerId);
            }
        }

        static void Update()
        {
            if (!IsMessageQueueRunning) return;
            var messages = new List<NetIncomingMessage>();
            int counter = Peer.ReadMessages(messages);

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
                    Debug.LogWarning(msg.ReadString());
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionLatencyUpdated)
                {
                    Latency = msg.ReadFloat();
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ErrorMessage)
                {
                    Debug.LogError(msg.ReadString());
                    Peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var lastStatus = _status;
                    _status = (NetConnectionStatus) msg.ReadByte();
                    StatusReason = msg.ReadString();
                    Peer.Recycle(msg);

                    try
                    {
                        if (_status == NetConnectionStatus.Disconnected)
                        {
                            if (lastStatus != NetConnectionStatus.Disconnected)
                            {
                                if (OnDisconnectedFromServer != null)
                                    OnDisconnectedFromServer();

                                if (Configuration.DeleteNetworkInstantiatesOnDisconnect)
                                {
                                    NetworkView.DestroyAllViews();
                                }
                            }
                            else
                            {
                                if (OnFailedToConnect != null)
                                    OnFailedToConnect(StatusReason);
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
                        Debug.LogError("[Net.Update.StatusChanged] {0}", e);
                    }
                }
                else if (msg.MessageType == NetIncomingMessageType.Error)
                {
                    Debug.LogError(msg.ReadString()); //this should really never happen...
                    Peer.Recycle(msg);
                }
                else
                    Peer.Recycle(msg);
            }
        }

        internal static NetOutgoingMessage CreateMessage(int initialCapacity)
        {
            return Peer.CreateMessage(initialCapacity);
        }


        private static void Consume(NetIncomingMessage msg)
        {
            try
            {
                //faster than switch, as this is in most to least common order
                if (msg.SequenceChannel == Channels.UNRELIABLE_STREAM)
                {
                    var actorId = msg.ReadUInt16();
                    NetworkView find;
                    if (NetworkView.Find(actorId, out find))
                        find.OnDeserializeStream(msg);
                }
                else if (msg.SequenceChannel == Channels.RELIABLE_STREAM)
                {
                    var actorId = msg.ReadUInt16();
                    NetworkView find;
                    if (NetworkView.Find(actorId, out find))
                        find.OnDeserializeStream(msg);
                }
                else if (msg.SequenceChannel >= Channels.BEGIN_RPCMODES && msg.SequenceChannel <= Channels.OWNER_RPC)
                {
                    //rpc...
                    var viewID = msg.ReadUInt16();
                    var rpcId = msg.ReadByte();
                    NetworkView find;
                    if (NetworkView.Find(viewID, out find))
                        find.CallRPC(rpcId, msg);
                    else
                        Debug.LogWarning("couldn't find view " + viewID + " to send rpc " + rpcId);
                }
                else if (msg.SequenceChannel == Channels.SYNCHED_FIELD)
                {
                    var viewId = msg.ReadUInt16();
                    var fieldId = msg.ReadByte();
                    NetworkView find;
                    if (NetworkView.Find(viewId, out find))
                        find.SetSynchronizedField(fieldId, msg);
                    else
                        Debug.LogWarning("couldn't find view " + viewId + " to set field " + fieldId);
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
                    Debug.LogWarning("data received over unhandled channel " + msg.SequenceChannel);
                }
            }
            catch (Exception er)
            {
                Debug.LogError("[Net.Consume] {0}", er);
            }
        }
    }
}
