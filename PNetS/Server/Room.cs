using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PNetS.Utils;
using SlimMath;
using Lidgren.Network;
using PNet;
using System.ComponentModel;

namespace PNetS
{
    /// <summary>
    /// A container for players/network views
    /// </summary>
    public sealed class Room
    {
        private List<Player> m_Players = new List<Player>();
        /// <summary>
        /// players in this room
        /// </summary>
        public List<Player> players { get { return m_Players; } }
        private List<NetworkView> m_Actors = new List<NetworkView>();
        /// <summary>
        /// actors owned by players in the room
        /// </summary>
        public List<NetworkView> actors { get { return m_Actors; } }
        List<bool> phases = new List<bool>();
        /// <summary>
        /// name of this room
        /// </summary>
        public string name;
        /// <summary>
        /// Close the room
        /// </summary>
        /// <param name="roomToChangeLeftoverPlayersTo"></param>
        public void Close(Room roomToChangeLeftoverPlayersTo)
        {
            GameState.RoomUpdates -= Update;
            
            try
            {
                for (int i = 0; i < _roomBehaviours.Count; ++i)
                {
                    _roomBehaviours[i].Closing();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Room Closing] {0}: {1}", name, e);
            }

            foreach (var player in players)
            {
                player.ChangeRoom(roomToChangeLeftoverPlayersTo);
            }

            foreach (var routine in rootRoutines)
            {
                GameState.RemoveRoutine(routine);
            }
            rootRoutines = null;
            
            foreach (var act in m_Actors)
            {
                NetworkView.RemoveView(act);
                GameObject.Destroy(act.gameObject);
            }
            m_Actors = null;

            foreach (var gobj in roomObjects)
            {
                GameObject.Destroy(gobj.Value.gameObject);
            }

            roomObjects = null;
            m_Players = null;
            phases = null;

            GC.Collect();
        }

        /// <summary>
        /// Instantiate an object over the network
        /// </summary>
        /// <param name="resourcePath">Path to the resource on the client</param>
        /// <param name="position"></param>
        /// <param name="rotation"></param>
        /// <param name="owner"></param>
        /// <returns></returns>
        public GameObject NetworkInstantiate(string resourcePath, Vector3 position, Quaternion rotation, Player owner)
        {
            //TODO: actually get the object out of the resource path...

            var gobj = GameState.CreateGameObject(position, rotation);
            gobj.Room = this;
            gobj.Resource = resourcePath;
            var netview = gobj.GetComponent<NetworkView>();

            if (netview == null)
            {
                netview = gobj.AddComponent<NetworkView>();
            }

            NetworkView.RegisterNewView(ref netview);

            netview.owner = owner;
            netview.MoveToPhase(0);

            m_Actors.Add(netview);

            SendNetworkInstantiate(GetConnectionsInPhase(owner.primaryPhase), gobj);

            return gobj;
        }

        internal void ResourceNetworkInstantiate(GameObject resourceLoadedObject)
        {
            var loadedView = resourceLoadedObject.GetComponent<NetworkView>();

            NetworkView.RegisterNewView(ref loadedView);

            loadedView.owner = Player.Server;
            loadedView.MoveToPhase(loadedView.phase);

            m_Actors.Add(loadedView);

            SendNetworkInstantiate(GetConnectionsInPhase(loadedView.phase), resourceLoadedObject);
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

            //sceneObject.OnFinishedCreation();
        }

        void SendNetworkInstantiate(List<NetConnection> connections, GameObject gobj)
        {
            var netView = gobj.GetComponent<NetworkView>();
            if (netView == null)
            {
                Debug.Log("[Instantiate] the specified object {0} does not have a network view to actually use for network instantiation", gobj.Resource);
                return;
            }

            var message = PNetServer.peer.CreateMessage(33 + (gobj.Resource.Length * 2));
            message.Write(RPCUtils.Instantiate);
            message.Write(gobj.Resource);
            
            message.Write(netView.viewID.guid);
            message.Write(netView.owner.Id);

            var vs = new Vector3Serializer(gobj.Position);
            vs.OnSerialize(message);
            var qs = new QuaternionSerializer(gobj.Rotation);
            qs.OnSerialize(message);

            if (connections.Count > 0)
                PNetServer.peer.SendMessage(message, connections , NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);
        }

        /// <summary>
        /// Destroy the specified view over the network
        /// </summary>
        /// <param name="view"></param>
        public void NetworkDestroy(NetworkView view)
        {
            m_Actors.Remove(view);

            GameObject.Destroy(view.gameObject);

            var msg = PNetServer.peer.CreateMessage(3);
            msg.Write(RPCUtils.Remove);
            msg.Write(view.viewID.guid);

            NetworkView.RemoveView(view);

            //send a destruction message to everyone, just in case.
            var connections = players.Select(p => p.connection).ToList();
            if (connections.Count == 0) return;
            PNetServer.peer.SendMessage(msg, connections, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_UTILS);

            
        }

        internal List<Player> GetPlayersInPhase(int phase)
        {
            List<Player> valid = new List<Player>(players.Count);
            foreach (var player in players)
            {
                if (player.IsInPhase(phase))
                {
                    valid.Add(player);
                }
            }

            return valid;
        }

        internal List<NetConnection> GetConnectionsInPhase(int phase)
        {
            List<NetConnection> valid = new List<NetConnection>(players.Count);
            foreach (var player in players)
            {
                if (player.IsInPhase(phase))
                {
                    valid.Add(player.connection);
                }
            }

            return valid;
        }

        internal void SendMessage(NetOutgoingMessage message)
        {
            var toSendConnections = players.Select(p => p.connection).ToList();
            if (toSendConnections.Count == 0) return;
            PNetServer.peer.SendMessage(message, toSendConnections, NetDeliveryMethod.ReliableOrdered, Channels.STATIC_RPC);
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
                    SendNetworkInstantiate(new List<NetConnection>() {connection}, actor.gameObject);
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
            SendMessage(message);
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
                Debug.LogWarning("Room {1}: unhandled RPC {0}", rpcID, name);
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

        /// <summary>
        /// called when a player enters the room
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerEnter(Player player)
        {
            for (int i = 0; i < _roomBehaviours.Count; ++i)
            {
                _roomBehaviours[i].OnPlayerEnter(player);
            }

            for (int i = 0; i < m_Actors.Count; i++)
            {
                m_Actors[i].gameObject.OnPlayerEnteredRoom(player);
            }
        }
        /// <summary>
        /// called when a player exists the room
        /// </summary>
        /// <param name="player"></param>
        public void OnPlayerExit(Player player)
        {
            for (int i = 0; i < _roomBehaviours.Count; ++i)
            {
                _roomBehaviours[i].OnPlayerExit(player);
            }

            for (int i = 0; i < m_Actors.Count; i++)
            {
                m_Actors[i].gameObject.OnPlayerLeftRoom(player);
            }
        }

        internal void AddPlayer(Player player)
        {
            player.currentRoom = this;
            m_Players.Add(player);

            // nobjs update
            foreach (var ro in roomObjects)
            {
                ro.Value.UpdateConnections();
            }

            //statics buffer, network instantiate
            SendBuffer(player.connection);

            // network views buffer
            m_Actors.ForEach(a => 
                {
                    a.UpdateConnections();
                });

            OnPlayerEnter(player);
        }

        internal void RemovePlayer(Player player)
        {
            player.FireLeaveRoom(this);

            m_Players.Remove(player);
            
            m_Actors.ForEach(a => a.UpdateConnections());

            foreach (var ro in roomObjects)
            {
                ro.Value.UpdateConnections();
            }

            OnPlayerExit(player);

            player.currentRoom = null;
        }

        /// <summary>
        /// in order to actually start a coroutine chain, you need to set IsRootRoutine to true on the first call in a coroutine call chain.
        /// </summary>
        /// <param name="routine"></param>
        /// <param name="IsRootRoutine"></param>
        /// <returns></returns>
        public Coroutine StartCoroutine(IEnumerator<YieldInstruction> routine, bool IsRootRoutine = false)
        {
            if (IsRootRoutine)
            {
                GameState.AddRoutine(routine);
                rootRoutines.Add(routine);
            }
            return new Coroutine(routine);
        }

        internal List<IEnumerator<YieldInstruction>> rootRoutines = new List<IEnumerator<YieldInstruction>>();


        Dictionary<ushort, NetworkedSceneObjectView> roomObjects = new Dictionary<ushort, NetworkedSceneObjectView>(8);

        /// <summary>
        /// all the static objects in the scene
        /// </summary>
        public Dictionary<ushort, NetworkedSceneObjectView> SceneObjects { get { return roomObjects; } }

        internal NetworkedSceneObjectView IncomingObjectRPC(NetIncomingMessage msg)
        {
 	        var viewId = msg.ReadUInt16();
            var rpcId = msg.ReadByte();
            NetworkedSceneObjectView view;
            if (roomObjects.TryGetValue(viewId, out view))
            {
                view.CallRPC(rpcId, msg, new NetMessageInfo(RPCMode.Server, msg.SenderConnection.Tag as Player));
            }

            return view;
        }

        private Room()
        {
            GameState.RoomUpdates+= Update;
        }

        /// <summary>
        /// Create a room
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Room CreateRoom(string name)
        {
            return new Room(){name = name};
        }

        /// <summary>
        /// Add a behaviour
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T AddBehaviour<T>()
            where T : RoomBehaviour, new()
        {
            var newT = new T();
            _roomBehaviours.Add(newT);
            newT.Room = this;

            GameState.AddStart(newT.Start);
            SubscribeMarkedRPCsOnBehaviour(newT);
            return newT;
        }

        /// <summary>
        /// get the first type that is t
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetBehaviour<T>()
            where T : RoomBehaviour
        {
            return (T)_roomBehaviours.FirstOrDefault(b => b as T != null);
        }

        /// <summary>
        /// get all behaviours that are of the type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T[] GetBehavours<T>()
            where T : class
        {
            return _roomBehaviours.OfType<T>().ToArray();
        }

        /// <summary>
        /// Remove the behaviour
        /// </summary>
        /// <param name="behaviour"></param>
        public void RemoveBehaviour(RoomBehaviour behaviour)
        {
            var ind = _roomBehaviours.FindIndex(o => object.ReferenceEquals(behaviour, o));

            if (ind != -1)
            {
                try
                {
                    _roomBehaviours[ind].Disposing();
                }catch(Exception e)
                {
                    Debug.LogError("[Disposing behaviour] {0}", e);
                }

                _roomBehaviours.RemoveAt(ind);
                behaviour.Room = null;
            }
        }

        private readonly List<RoomBehaviour> _roomBehaviours = new List<RoomBehaviour>();
        internal void Update()
        {
            try
            {
                for (int i = 0; i < _roomBehaviours.Count; ++i)
                {
                    _roomBehaviours[i].Update();
                }
            }catch(Exception e)
            {
                Debug.LogError("[Room Update] {0}: {1}", name, e);
            }
        }
    }
}
