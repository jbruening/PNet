using System;
using System.Collections.Generic;
using System.ComponentModel;
using Lidgren.Network;
using PNet;

namespace PNetS
{
    /// <summary>
    /// The server
    /// </summary>
    public static partial class PNetServer
    {
        /// <summary>
        /// When a new player has finished connecting
        /// </summary>
        public static Action<Player> OnPlayerConnected = delegate { };
        /// <summary>
        /// When the server begins initialization
        /// </summary>
        public static Action OnServerInitialized = delegate { };
        /// <summary>
        /// When a player disconnects or times out
        /// </summary>
        public static Action<Player> OnPlayerDisconnected = delegate { };

        /// <summary>
        /// Approval method for new connections
        /// </summary>
        public static Action<NetIncomingMessage> ApproveConnection;
        /// <summary>
        /// When the server receives a discovery request
        /// </summary>
        public static Action<NetOutgoingMessage> OnDiscoveryRequest;
        /// <summary>
        /// NOT USED
        /// </summary>
        public static NetworkLogLevel logLevel;
        /// <summary>
        /// last reason for the status change
        /// </summary>
        [DefaultValue("")]
        public static string statusReason { get; internal set; }

        /// <summary>
        /// 
        /// </summary>
        public static NetPeerStatus PeerStatus { get { return peer.Status; } }
        /// <summary>
        /// NOT USED CURRENTLY
        /// </summary>
        public static bool isMessageQueueRunning = true;
        /// <summary>
        /// Current network time. Can be used with NetMessage.Read/Write time
        /// </summary>
        public static double Time { get { return GameState.NetFrameTime; } }

        internal static NetServer peer;
        /// <summary>
        /// Get a new outgoing message
        /// </summary>
        public static NetOutgoingMessage GetMessage { get { return peer.CreateMessage(); } }
        
        static NetPeerConfiguration _netPeerConfiguration;

        /// <summary>
        /// the server configuration
        /// </summary>
        public static ServerConfiguration Configuration { get; private set; }

        /// <summary>
        /// Set up the server, and bind to a socket. Use Start to actually fully start the server
        /// </summary>
        /// <param name="maxConnections"></param>
        /// <param name="listenPort"></param>
        /// <param name="tickRate"></param>
        [Obsolete("Use the overload that takes a ServerConfiguration")]
        public static void InitializeServer(int maxConnections, int listenPort, int tickRate = 66)
        {
            Configuration = new ServerConfiguration(maxConnections, listenPort, tickRate);
        }

        /// <summary>
        /// Set up the server, bind to a socket. Use Start to fully start the server after running this
        /// </summary>
        /// <param name="configuration"></param>
        public static void InitializeServer(ServerConfiguration configuration)
        {
            Configuration = configuration;

            if (peer != null && peer.Status != NetPeerStatus.NotRunning)
            {
                Debug.LogError("cannot start server while already running");
                return;
            }

            _netPeerConfiguration = new NetPeerConfiguration(Configuration.AppIdentifier);
            _netPeerConfiguration.Port = Configuration.ListenPort;
            _netPeerConfiguration.MaximumConnections = Configuration.MaximumConnections;
            connections = new IntDictionary<NetConnection>(Configuration.MaximumConnections);

            _netPeerConfiguration.SetMessageTypeEnabled(NetIncomingMessageType.ConnectionApproval, true);

            peer = new NetServer(_netPeerConfiguration);

            peer.Start();

            var serverId = connections.Add(null);
            var serverPlayer = new Player();
            serverPlayer.Id = (ushort)serverId;
            Player.Server = serverPlayer;

            GameState.update += Update;
        }

        /// <summary>
        /// Actually start the server
        /// </summary>
        /// <param name="frameTime"></param>
        public static void Start(double frameTime = 0.02d)
        {
            GameState.Start(frameTime);
        }

        /// <summary>
        /// shut down the server game machine. The network, however, will still be connected.
        /// </summary>
        public static void Shutdown()
        {
            GameState.Quit();
        }
        
        /// <summary>
        /// shut down the networking
        /// </summary>
        public static void Disconnect()
        {
            if (peer == null)
                return;
            peer.Shutdown("shutting down");
        }

        static void AddPlayer(NetConnection connection)
        {
            var playerId = connections.Add(connection);
            var tag = connection.Tag as Player;

            tag.Id = (ushort)playerId;

            var idMessage = peer.CreateMessage(3);
            idMessage.Write(RPCUtils.SetPlayerId);
            idMessage.Write(tag.Id);

            connection.SendMessage(idMessage, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);

            Debug.Log("sent id to player {0}", tag.Id);

            OnPlayerConnected(tag);
        }
        static void RemovePlayer(NetConnection connection)
        {
            var player = connection.Tag as Player;
            if (player == null)
            {
                Debug.LogError("could not find the player for connection {0} to remove", connection.RemoteUniqueIdentifier);
                return;
            }
            var playerId = player.Id;
            if (playerId == 0)
            {
                Debug.LogWarning("A player of id 0 disconnected, meaning they probably dc'd during Approval.");
            }
            else
            {
                connections.Remove(playerId);
            }

            if (player.CurrentRoom != null)
                player.CurrentRoom.RemovePlayer(player);

            OnPlayerDisconnected(player);
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
                    {
                        Player player = GetPlayer(msg.SenderConnection);
                        find.OnDeserializeStream(msg, player);
                    }
                    else
                    {
                        Debug.LogWarning("[PNetS.Consume] Player {0} attempted to send unreliable stream data for view {1}, but it does not exist",
                            msg.SenderConnection.Tag, actorId);
                        (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                    }
                }
                else if (msg.SequenceChannel == Channels.RELIABLE_STREAM)
                {
                    var actorId = msg.ReadUInt16();
                    NetworkView find;
                    if (NetworkView.Find(actorId, out find))
                    {
                        Player player = GetPlayer(msg.SenderConnection);
                        find.OnDeserializeStream(msg, player);
                    }
                    else
                    {
                        Debug.LogWarning("[PNetS.Consume] Player {0} attempted to send reliable stream data for view {1}, but it does not exist",
                            msg.SenderConnection.Tag, actorId);
                        (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                    }
                }
                else if (msg.SequenceChannel >= Channels.BEGIN_RPCMODES && msg.SequenceChannel <= Channels.OWNER_RPC)
                {
                    //rpc...
                    var viewID = msg.ReadUInt16();
                    var rpcId = msg.ReadByte();
                    Player player = GetPlayer(msg.SenderConnection);
                    NetworkView find;
                    var info = new NetMessageInfo((RPCMode)(msg.SequenceChannel - Channels.BEGIN_RPCMODES), player);
                    if (NetworkView.Find(viewID, out find))
                    {
                        find.CallRPC(rpcId, msg, info);


                        //Do we need to forward this still?
                        if (info.mode != RPCMode.Server && info.continueForwarding)
                        {
                            //need to forward...
                            if (info.mode == RPCMode.Others || info.mode == RPCMode.All) { }
                            else
                            {
                                find.Buffer(msg);
                            }
                            find.Send(msg, info.mode, msg.SenderConnection);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PNetS.Consume] Player {0} attempted RPC {1} on view {2}, but the view does not exist",
                            player, rpcId, viewID);
                        player.InternalErrorCount++;
                    }
                }
                else if (msg.SequenceChannel == Channels.SYNCHED_FIELD)
                {
                    var viewId = msg.ReadUInt16();
                    var fieldId = msg.ReadByte();
                    NetworkView find;
                    if (NetworkView.Find(viewId, out find))
                    {
                        find.BufferField(msg, fieldId);
                        find.SendField(msg, msg.SenderConnection);
                    }
                }
                else if (msg.SequenceChannel == Channels.OBJECT_RPC)
                {
                    Player player = GetPlayer(msg.SenderConnection);
                    if (player.CurrentRoom != null)
                        player.CurrentRoom.IncomingObjectRPC(msg);
                }
                else if (msg.SequenceChannel == Channels.STATIC_RPC)
                {
                    var rpcId = msg.ReadByte();

                    var player = GetPlayer(msg.SenderConnection);
                    var currentRoom = player.CurrentRoom;
                    if (currentRoom != null)
                    {
                        NetMessageInfo info = new NetMessageInfo(RPCMode.None, player);
                        currentRoom.CallRPC(rpcId, msg, info);

                        if (info.continueForwarding)
                        {
                            var newMessage = peer.CreateMessage();
                            msg.Clone(newMessage);
                            currentRoom.SendMessage(newMessage);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[PNetS.Consume] Player {0} attempted static rpc {1}, but they are not in a room to do so",
                            player, rpcId);
                        (player).InternalErrorCount++;
                    }

                }
                else if (msg.SequenceChannel == Channels.STATIC_UTILS)
                {
                    ProcessUtils(msg);
                }
                else
                {
                    Debug.LogWarning("data received over unhandled channel " + msg.SequenceChannel);
                    (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[Consumption] {0} : {1}", ex.Message, ex.StackTrace);
            }
        }

        internal static void ProcessUtils(NetIncomingMessage msg)
        {
            //
            var utilId = msg.ReadByte();

            if (utilId == RPCUtils.TimeUpdate)
            {
                //Players shouldn't be doing this either..
                Debug.LogWarning("[PNetS.ProcessUtils] Player {0} attempted to set time", msg.SenderConnection.Tag);
                (msg.SenderConnection.Tag as Player).InternalErrorCount++;
            }
            else if (utilId == RPCUtils.Instantiate)
            {
                //Players shouldn't be instantiating things
                Debug.LogWarning("[PNetS.ProcessUtils] Player {0} attempted to instantiate something", msg.SenderConnection.Tag);
                (msg.SenderConnection.Tag as Player).InternalErrorCount++;
            }
            else if (utilId == RPCUtils.FinishedRoomChange)
            {
                //the player has finished loading into the new room. now actually put their player in the new room.
                var player = msg.SenderConnection.Tag as Player;

                var newRoom = player.GetRoomSwitchingTo();

                if (newRoom != null)
                {
                    newRoom.AddPlayer(player);
                }
                else
                {
                    Debug.LogWarning("[PNetS.ProcessUtils] Player {0} attempted to finish joining a room, but it no longer exists", msg.SenderConnection.Tag);
                    (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                }
            }
            else if (utilId == RPCUtils.Remove)
            {
                //Players shouldn't be removing view ids
                Debug.LogWarning("[PNetS.ProcessUtils] Player {0} attempted to remove a network view", msg.SenderConnection.Tag);
                (msg.SenderConnection.Tag as Player).InternalErrorCount++;
            }
            else if (utilId == RPCUtils.FinishedInstantiate)
            {
                Player player = msg.SenderConnection.Tag as Player;
                ushort viewId = msg.ReadUInt16();

                NetworkView find;
                if (NetworkView.Find(viewId, out find))
                {
                    find.gameObject.OnFinishedInstantiate(player);
                }
                else
                {
                    Debug.LogWarning("[PNetS.ProcessUtils] Player {0} attempted to finish instantiation for {1}, but that view ID doesn't exist", msg.SenderConnection.Tag, viewId);
                    (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                }
            }
        }

        private static IntDictionary<NetConnection> connections;

        internal static Player GetPlayer(NetConnection connection)
        {
            return connection.Tag as Player;
        }

        /// <summary>
        /// Get all the currently connected players
        /// Warning: Some of the values will be null, due to the way the internal lists are handled. 
        /// Just check for null first, before doing any operations
        /// </summary>
        /// <returns></returns>
        public static List<Player> AllPlayers()
        {
            var conns = connections.Values;
            var ret = new List<Player>(conns.Length);
            
// ReSharper disable ForCanBeConvertedToForeach we're going for speed
            for (int i = 0; i < conns.Length; i++)
// ReSharper restore ForCanBeConvertedToForeach
            {
                var c = conns[i];
                ret.Add(c == null ? null : c.Tag as Player);
            }
            return ret;
        }

        //rooms

        //public static List<Room> rooms { get; internal set; }

        //public static void AddRoom(Room room)
        //{
        //    rooms.Add(room);
        //}

        /// <summary>
        /// Send a message to all players on the server
        /// </summary>
        /// <param name="rpcId">id of the RPC</param>
        /// <param name="args"></param>
        public static void RPC(byte rpcId, params INetSerializable[] args)
        {
            var size = 1;
            RPCUtils.AllocSize(ref size, args);

            var message = peer.CreateMessage(size);
            message.Write(rpcId);
            RPCUtils.WriteParams(ref message, args);

            peer.SendToAll(message, null, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
        }
    }
}
