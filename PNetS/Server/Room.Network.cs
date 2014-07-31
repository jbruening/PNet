using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Lidgren.Network;
using PNet;
using PNetS.Utils;
using SlimMath;

namespace PNetS
{
    public partial class Room
    {
        /// <summary>
        /// port that this room is using for its own netserver
        /// </summary>
        public int Port { get { return _peer.Port; } }

        private NetServer _peer;
        internal NetServer Peer
        {
            get { return _peer; }
            private set { _peer = value; }
        }

        /// <summary>
        /// Instantiate an object over the network
        /// </summary>
        /// <param name="resourcePath">Path to the resource on the client</param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="owner"></param>
        /// <param name="visibleToAll">if false, the object is only visible to the owner. Additional visiblity is achieved via setting the subscriptions on the networkview </param>
        /// <returns></returns>
        public GameObject NetworkInstantiate(string resourcePath, Vector3 position, Quaternion rotation, Player owner, bool visibleToAll = true)
        {
            var gobj = GameState.CreateGameObject(position, rotation);
            gobj.Room = this;
            gobj.Resource = resourcePath;
            gobj.Owner = owner;
            var netview = gobj.GetComponent<NetworkView>() ?? gobj.AddComponent<NetworkView>();

            NetworkView.RegisterNewView(ref netview);

            netview.owner = owner;

            m_Actors.Add(netview);

            SendNetworkInstantiate(netview.Connections, gobj);

            OnGameObjectAdded(gobj);

            return gobj;
        }

        internal void ResourceNetworkInstantiate(GameObject resourceLoadedObject, bool visibleToAll, Player owner)
        {
            var loadedView = resourceLoadedObject.GetComponent<NetworkView>();

            NetworkView.RegisterNewView(ref loadedView);

            if (owner != null && owner.CurrentRoom == this)
                loadedView.owner = owner;
            else
                loadedView.owner = Player.Server;

            resourceLoadedObject.Owner = loadedView.owner;

            m_Actors.Add(loadedView);

            SendNetworkInstantiate(loadedView.Connections, resourceLoadedObject);
        }

        /// <summary>
        /// Create a static game object that can receive rpc's in the scene. 
        /// The client should have a similar object in the scene matching the room with the same id
        /// </summary>
        /// <param name="sceneObject"></param>
        /// <param name="gobj"></param>
        public void NetworkedRoomObject(NetworkedSceneObject sceneObject, GameObject gobj)
        {
            if (gobj.Room != this && gobj.Room != null)
            {
                Debug.LogError("cannot move the object to the room. it must have no room or be in this one.");
                return;
            }

            var sceneView = gobj.AddComponent<NetworkedSceneObjectView>();

            gobj.Room = this;

            sceneView.room = this;
            sceneView.NetworkID = sceneObject.NetworkID;

            roomObjects[sceneObject.NetworkID] = sceneView;
            OnGameObjectAdded(gobj);
            //sceneObject.OnFinishedCreation();
        }

        internal void SendNetworkInstantiate(List<NetConnection> connections, GameObject gobj)
        {
            var netView = gobj.GetComponent<NetworkView>();
            if (netView == null)
            {
                Debug.Log("[Instantiate] the specified object {0} does not have a network view to actually use for network instantiation", gobj.Resource);
                return;
            }

            if (connections.Count <= 0) return;

            var message = PNetServer.peer.CreateMessage(33 + (gobj.Resource.Length * 2));
            message.Write(RPCUtils.Instantiate);
            message.Write(gobj.Resource);

            message.Write(netView.viewID.guid);
            message.Write(netView.owner.Id);

            var vs = new Vector3Serializer(gobj.Position);
            vs.OnSerialize(message);
            var qs = new QuaternionSerializer(gobj.Rotation);
            qs.OnSerialize(message);

            PNetServer.peer.SendMessage(message, connections, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
        }

        /// <summary>
        /// Destroy the specified view over the network
        /// </summary>
        /// <param name="view"></param>
        /// <param name="reasonCode">specify a reason to be destroying this networkview. Received on client</param>
        public void NetworkDestroy(NetworkView view, byte reasonCode = 0)
        {
            GameState.DestroyDelays += () => DoNetworkDestroy(view, reasonCode);
        }

        void DoNetworkDestroy(NetworkView view, byte reasonCode)
        {
            m_Actors.Remove(view);

            //if we don't do it now, it'll just get cleared once gamestate leaves the delegate call
            GameObject.Destroy(view.gameObject);

            NetworkView.RemoveView(view);

            //send a destruction message to everyone, just in case.
            var connections = new List<NetConnection>(players.Count);
            for (int i = 0; i < players.Count; i++)
                connections.Add(players[i].connection);
            if (connections.Count == 0) return;

            var msg = PNetServer.peer.CreateMessage(3);
            msg.Write(RPCUtils.Remove);
            msg.Write(view.viewID.guid);
            msg.Write(reasonCode);
            PNetServer.peer.SendMessage(msg, connections, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
        }

        internal void SendMessage(NetOutgoingMessage message, bool inOrder = true)
        {
            var connections = new List<NetConnection>(players.Count);
            for (int i = 0; i < players.Count; i++)
                connections.Add(players[i].connection);
            if (connections.Count > 0)
            {
                if (inOrder)
                    PNetServer.peer.SendMessage(message, connections, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
                else
                    PNetServer.peer.SendMessage(message, connections, NetDeliveryMethod.ReliableUnordered, Channels.STATIC_RPC_UNORDERED);
            }
            else
                PNetServer.peer.Recycle(message);
        }

        readonly List<NetBuffer> bufferedMessages = new List<NetBuffer>(16);

        private void Buffer(NetOutgoingMessage message, RPCMode mode)
        {
            var nBuffer = new NetBuffer();
            message.Clone(nBuffer);
            bufferedMessages.Add(nBuffer);
        }

        void SendBuffer(NetConnection connection)
        {
            foreach (var buffer in bufferedMessages)
            {
                var reuseMessage = PNetServer.peer.CreateMessage();
                buffer.Clone(reuseMessage);
                PNetServer.peer.SendMessage(reuseMessage, connection, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
            }
            //and also the instantiates.

            foreach (var actor in m_Actors)
            {
                if (!actor.IsSecondaryView)
                    SendNetworkInstantiate(new List<NetConnection>() { connection }, actor.gameObject);
            }
        }

        /// <summary>
        /// Send a message to all players in the room
        /// </summary>
        /// <param name="rpcId">id of the RPC</param>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcId, RPCMode mode, params INetSerializable[] args)
        {
            var size = 1;
            RPCUtils.AllocSize(ref size, args);

            var message = PNetServer.peer.CreateMessage(size);
            message.Write(rpcId);
            RPCUtils.WriteParams(ref message, args);

            if (mode == RPCMode.AllBuffered || mode == RPCMode.OthersBuffered)
            {
                Buffer(message, mode);
            }

            if (mode == RPCMode.AllUnordered || mode == RPCMode.OthersUnordered)
                SendMessage(message, false);
            else
                SendMessage(message, true);
        }

        private static int _lastFrameSize = 16;
        void NetworkUpdate()
        {
            //is reinstantiating faster? are we dealing with enough messages to make a difference?
            var messages = new List<NetIncomingMessage>(_lastFrameSize * 2);
            int counter = _peer.ReadMessages(messages);
            _lastFrameSize = counter;

            //for faster than foreach
            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < messages.Count; i++)
            {
                var msg = messages[i];

                //faster than switch, as most will be Data messages.
                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    Consume(msg);
                    _peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.DebugMessage)
                {
                    Debug.Log("[Room] {0}: {1}", Name, msg.ReadString());
                    _peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    Debug.LogWarning("[Room] {0}: {1}", Name, msg.ReadString());
                    _peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionLatencyUpdated)
                {
                    var latency = msg.ReadFloat();
                    //todo: do something with this latency.
                    _peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ErrorMessage)
                {
                    Debug.LogError("[Room] {0}: {1}", Name, msg.ReadString());
                    _peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionApproval)
                {
                    ApproveConnection(msg);
                    _peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var status = (NetConnectionStatus)msg.ReadByte();
                    var statusReason = msg.ReadString();
                    Debug.Log("Room {2} Status: {0}, {1}", status, statusReason, Name);

                    if (status == NetConnectionStatus.Disconnected || status == NetConnectionStatus.Disconnecting)
                    {
                        msg.SenderConnection.Tag = null;
                    }
                    _peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.Error)
                {
                    Debug.LogError("Room {0} error: {1}", Name, msg.ReadString()); //this should really never happen...
                    _peer.Recycle(msg);
                }
                else
                    _peer.Recycle(msg);
            }
        }

        private void Consume(NetIncomingMessage msg)
        {
            if (msg.SenderConnection.Tag == null) 
                return;
            try
            {
                //faster than switch, as this is in most to least common order
                if (msg.SequenceChannel == Channels.UNRELIABLE_STREAM)
                {
                    var actorId = msg.ReadUInt16();
                    NetworkView find;
                    if (NetworkView.Find(actorId, out find))
                    {
                        Player player = PNetServer.GetPlayer(msg.SenderConnection);
                        find.OnDeserializeStream(msg, player);
                    }
                    else
                    {
                        if (PNetServer.GetPlayer(msg.SenderConnection).CurrentRoom != null)
                        {
                            Debug.LogWarning(
                                "[Room.Consume] Player {0} attempted to send unreliable stream data for view {1}, but it does not exist",
                                msg.SenderConnection.Tag, actorId);
                            (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                        }
                    }
                }
                else if (msg.SequenceChannel == Channels.RELIABLE_STREAM)
                {
                    var actorId = msg.ReadUInt16();
                    NetworkView find;
                    if (NetworkView.Find(actorId, out find))
                    {
                        Player player = PNetServer.GetPlayer(msg.SenderConnection);
                        find.OnDeserializeStream(msg, player);
                    }
                    else
                    {
                        if (PNetServer.GetPlayer(msg.SenderConnection).CurrentRoom != null)
                        {
                            Debug.LogWarning(
                                "[Room.Consume] Player {0} attempted to send reliable stream data for view {1}, but it does not exist",
                                msg.SenderConnection.Tag, actorId);
                            (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                        }
                    }
                }
                else if (msg.SequenceChannel >= Channels.BEGIN_RPCMODES && msg.SequenceChannel <= Channels.OWNER_RPC)
                {
                    //rpc...
                    var viewId = msg.ReadUInt16();
                    var rpcId = msg.ReadByte();
                    Player player = PNetServer.GetPlayer(msg.SenderConnection);
                    NetworkView find;
                    var info = new NetMessageInfo((RPCMode)(msg.SequenceChannel - Channels.BEGIN_RPCMODES), player);
                    if (NetworkView.Find(viewId, out find))
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
                        if (player.CurrentRoom != null)
                        {
                            Debug.LogWarning(
                                "[Room.Consume] Player {0} attempted RPC {1} on view {2}, but the view does not exist",
                                player, rpcId, viewId);
                            player.InternalErrorCount++;
                        }
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
                    IncomingObjectRPC(msg);
                }
                else
                {
                    Debug.LogWarning("[Room] {2} bytes received over unhandled channel {0}, delivery {1}", msg.SequenceChannel, msg.DeliveryMethod, msg.LengthBytes);
                    (msg.SenderConnection.Tag as Player).InternalErrorCount++;
                }
            }
            catch (Exception ex)
            {
                Debug.Log("[Consumption] {0} : {1}", ex.Message, ex.StackTrace);
            }
        }

        void ApproveConnection(NetIncomingMessage msg)
        {
            var playerId = msg.ReadUInt16();
            var roomKey = msg.ReadBytes(16);
            var player = PNetServer.GetPlayer(playerId);
            if (player == null)
            {
                msg.SenderConnection.Deny("INVID");
                return;
            }
            if (!roomKey.SequenceEqual(player.RoomChangeKeyBytes))
            {
                msg.SenderConnection.Deny("INVKEY");
                return;
            }
            msg.SenderConnection.Tag = player;
            player.RoomConnection = msg.SenderConnection;
            msg.SenderConnection.Approve();

            AddPlayer(player);
            Array.Clear(player.RoomChangeKeyBytes, 0, player.RoomChangeKeyBytes.Length);
        }

        #region RPC Processing

        readonly Dictionary<byte, RPCProcessor> _rpcProcessors = new Dictionary<byte, RPCProcessor>();

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
                    Debug.LogWarning("RPC processor for {0} was null. Automatically cleaning up. Please be sure to clean up after yourself in the future.", rpcID);
                    _rpcProcessors.Remove(rpcID);
                }
            }
            else
            {
                Debug.LogWarning("Room {1}: unhandled RPC {0}", rpcID, Name);
                info.continueForwarding = false;
            }
        }

        /// <summary>
        /// Subscribe all the marked rpcs on the supplied component
        /// </summary>
        /// <param name="behaviour"> </param>
        public void SubscribeMarkedRPCsOnBehaviour(RoomBehaviour behaviour)
        {
            if (behaviour == null) return;

            var thisType = behaviour.GetType();

            //get all the methods of the derived type
            MethodInfo[] methods = thisType.GetMethods(
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy
                );

            foreach (var method in methods)
            {
                var tokens = Attribute.GetCustomAttributes(method, typeof(RpcAttribute), false) as RpcAttribute[];

                foreach (var token in tokens)
                {

                    if (token == null)
                        continue;

                    var del = Delegate.CreateDelegate(typeof(Action<NetIncomingMessage, NetMessageInfo>), behaviour, method, false) as Action<NetIncomingMessage, NetMessageInfo>;

                    if (del != null)
                        SubscribeToRPC(token.rpcId, del, defaultContinueForwarding: token.defaultContinueForwarding);
                    else
                        Debug.LogWarning("The method {0} for type {1} does not match the RPC delegate of Action<NetInComingMessage, NetMessageInfo>, but is marked to process RPC's. Please either fix this method, or remove the attribute",
                            method.Name,
                            method.DeclaringType.Name
                            );
                }
            }
        }

        #endregion
    }
}
