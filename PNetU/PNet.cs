using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Lidgren.Network;
using PNet;
using System.ComponentModel;

namespace PNetU
{
    /// <summary>
    /// Networking class
    /// </summary>
    public static class Net
    {
        /// <summary>
        /// When finished connecting to the server
        /// </summary>
        public static Action OnConnectedToServer = delegate { };
        /// <summary>
        /// When disconnected from the server
        /// </summary>
        public static Action OnDisconnectedFromServer = delegate { };
        /// <summary>
        /// When we've failed to connect
        /// </summary>
        public static Action<string> OnFailedToConnect = delegate { };
        /// <summary>
        /// When the room is changing
        /// </summary>
        public static Action<string> OnRoomChange = delegate { };
        /// <summary>
        /// subscribe to this in order to receive static RPC's from the server. you need to manually process them.
        /// </summary>
        public static Action<byte, NetIncomingMessage> ProcessRPC = delegate { };
        /// <summary>
        /// When a discovery response is received
        /// </summary>
        public static Action<NetIncomingMessage> OnDiscoveryResponse = delegate { };
        /// <summary>
        /// logging level. UNUSED
        /// </summary>
        public static NetworkLogLevel logLevel;
        internal static Dictionary<string, GameObject> ResourceCache;
        /// <summary>
        /// resource caching for instantiation
        /// </summary>
        public static bool resourceCaching;
        /// <summary>
        /// latest status
        /// </summary>
        public static NetConnectionStatus status { 
            get
            {
                return m_status;
            } 
            internal set
            {
                m_status = value;
                Debug.Log("[Net Status] " + m_status);
            }
        }
        private static NetConnectionStatus m_status = NetConnectionStatus.Disconnected;
        /// <summary>
        /// reason for the most latest status
        /// </summary>
        [DefaultValue("")]
        public static string statusReason { get; internal set; }
        /// <summary>
        /// pause the processing of the network queue
        /// </summary>
        public static bool isMessageQueueRunning = true;
        /// <summary>
        /// Not currently set
        /// </summary>
        public static double time { get; internal set; }
        /// <summary>
        /// The function to use for writing the connect data (username/password/etc)
        /// </summary>
        public static Action<NetOutgoingMessage> WriteHailMessage = delegate { };

        internal static NetClient peer;
        internal static ushort PlayerId;
        static NetPeerConfiguration config;
        static EngineUpdateHook singletonEngineHook;

        static Net()
        {
            status = NetConnectionStatus.Disconnected;
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
        public static ClientConfiguration Configuration { get; private set; }
        /// <summary>
        /// Connect with the specified configuration
        /// </summary>
        /// <param name="configuration"></param>
        public static void Connect(ClientConfiguration configuration)
        {
            Configuration = configuration;
            if (peer != null && peer.Status != NetPeerStatus.NotRunning)
            {
                Debug.LogError("cannot connect while connected");
                return;
            }

            //do we have an engine hook?
            if (!singletonEngineHook)
            {
                var gobj = new GameObject("UNet Singleton");
                singletonEngineHook = gobj.AddComponent<EngineUpdateHook>();
                GameObject.DontDestroyOnLoad(gobj);
                //gobj.hideFlags = HideFlags.DontSave;


            }
            //set up netclient...

            ResourceCache = new Dictionary<string, GameObject>();

            singletonEngineHook.UpdateSubscription += Update;

            config = new NetPeerConfiguration(Configuration.AppIdentifier);
            config.Port =  Configuration.BindPort; //so we can run client and server on the same machine..

            peer = new NetClient(config);

            peer.Start();

            var hailMessage = peer.CreateMessage();
            WriteHailMessage(hailMessage);
            peer.Connect(Configuration.Ip, Configuration.Port, hailMessage);
        }

        
        /// <summary>
        /// Disconnect if connected
        /// </summary>
        public static void Disconnect()
        {
            if (peer == null)
                return;

            peer.Shutdown("disconnecting");

            status = NetConnectionStatus.Disconnected;
            statusReason = "disconnecting";
            
            singletonEngineHook.UpdateSubscription -= Update;

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

            var message = peer.CreateMessage(size);
            message.Write(rpcId);
            RPCUtils.WriteParams(ref message, args);

            peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
        }

        /// <summary>
        /// Run once the room changing has completed (tells the server you're actually ready to be in a room)
        /// </summary>
        public static void FinishedRoomChange()
        {
            var message = peer.CreateMessage(1);

            message.Write(RPCUtils.FinishedRoomChange);
            peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
        }

        private static void FinishedInstantiate(ushort netID)
        {
            NetOutgoingMessage msg = peer.CreateMessage(3);
            msg.Write(RPCUtils.FinishedInstantiate);
            msg.Write(netID);

            peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
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
                if (Debug.isDebugBuild)
                    Debug.Log("network instantiate of " + resourcePath);
                var viewId = msg.ReadUInt16();
                var ownerId = msg.ReadUInt16();

                GameObject gobj;
                bool isCached = false;
                if (resourceCaching && (isCached = ResourceCache.ContainsKey(resourcePath)))
                    gobj = ResourceCache[resourcePath];
                else
                    gobj = Resources.Load(resourcePath) as GameObject;

                if (resourceCaching && !isCached)
                    ResourceCache.Add(resourcePath, gobj);

                var instance = (GameObject)GameObject.Instantiate(gobj);

                if (instance == null)
                {
                    Debug.LogWarning("could not find prefab " + resourcePath + " to instantiate");
                    return;
                }

                var trans = instance.transform;

                trans.position = Vector3Serializer.Deserialize(msg);
                trans.rotation = QuaternionSerializer.Deserialize(msg);
                if (Debug.isDebugBuild)
                {
                    Debug.Log(string.Format("network instantiate of {0}. Loc: {1} Rot: {2}", resourcePath, trans.position, trans.rotation));
                }

                //look for a networkview..

                var view = instance.GetComponent<NetworkView>();

                if (view)
                {
                    NetworkView.RegisterView(view, viewId);
                    view.viewID = new NetworkViewId() { guid = viewId, IsMine = PlayerId == ownerId};
                    view.IsMine = PlayerId == ownerId;
                    view.OwnerId = ownerId;

                    var nBehaviours = instance.GetComponents<NetBehaviour>();

                    foreach (var behave in nBehaviours)
                    {
                        behave.netView = view;

                        view.OnFinishedCreation += behave.CallFinished;
                    }

                    view.OnFinishedCreation();

                    FinishedInstantiate(view.viewID.guid);
                }
            }
            else if (utilId == RPCUtils.Remove)
            {
                var viewId = msg.ReadUInt16();

                NetworkView find;
                if (NetworkView.Find(viewId, out find))
                {
                    find.OnRemove();
                }

                NetworkView.RemoveView(viewId);
            }
            else if (utilId == RPCUtils.ChangeRoom)
            {
                var newRoom = msg.ReadString();

                NetworkedSceneObject.sceneObjects = new Dictionary<int, NetworkedSceneObject>();
                OnRoomChange(newRoom);
            }
            else if (utilId == RPCUtils.AddView)
            {
                var addToId = msg.ReadUInt16();
                var idToAdd = msg.ReadUInt16();

                NetworkView view;
                if (NetworkView.Find(addToId, out view))
                {
                    NetworkView.RegisterView(view, idToAdd);
                    var newView = view.gameObject.AddComponent<NetworkView>();
                    newView.viewID = new NetworkViewId() { guid = idToAdd, IsMine = view.IsMine };
                    newView.IsMine = view.IsMine;
                    newView.OwnerId = view.OwnerId;
                }
            }
            else if (utilId == RPCUtils.SetPlayerId)
            {
                var playerId = msg.ReadUInt16();
                PlayerId = playerId;
                if (Debug.isDebugBuild)
                    Debug.Log("Setting player id to " + playerId);
            }
        }

        static void Update()
        {
            if (!isMessageQueueRunning) return;
            List<NetIncomingMessage> messages = new List<NetIncomingMessage>();
            int counter = peer.ReadMessages(messages);

            foreach (var msg in messages)
            {
                //faster than switch, as most will be Data messages.

                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    Consume(msg);
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.DiscoveryResponse)
                {
                    OnDiscoveryResponse(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    Debug.LogWarning(msg.ReadString());
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionLatencyUpdated)
                {
                    var latency = msg.ReadFloat();
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ErrorMessage)
                {
                    Debug.LogError(msg.ReadString());
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    status = (NetConnectionStatus)msg.ReadByte();
                    statusReason = msg.ReadString();
                    peer.Recycle(msg);

                    try
                    {
                        if (status == NetConnectionStatus.Disconnected)
                            OnDisconnectedFromServer();
                        else if (status == NetConnectionStatus.Connected)
                            OnConnectedToServer();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                    }
                }
                else if (msg.MessageType == NetIncomingMessageType.Error)
                {
                    Debug.LogError(msg.ReadString()); //this should really never happen...
                    peer.Recycle(msg);
                }
                else
                    peer.Recycle(msg);
            }
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
                    NetworkedSceneObject find;
                    if (NetworkedSceneObject.sceneObjects.TryGetValue(viewId, out find))
                        find.CallRPC(rpcId, msg);
                }
                else if (msg.SequenceChannel == Channels.STATIC_RPC)
                {
                    var rpcId = msg.ReadByte();
                    ProcessRPC(rpcId, msg);
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
                Debug.LogError(er.Message + er.StackTrace);
            }
        }
    }
}
