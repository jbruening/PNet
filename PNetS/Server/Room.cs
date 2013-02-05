using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SlimMath;
using Lidgren.Network;
using PNet;
using System.ComponentModel;

namespace PNetS
{
    /// <summary>
    /// A container for players/network views
    /// </summary>
    public abstract class Room
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
            //TODO: close the room, move players to a new room or lobby or something

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
                NetworkView.RemoveView(act.viewID.guid);
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
        /// <param name="resourcePath"></param>
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

            NetworkView.RemoveView(view.viewID.guid);

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

        List<NetBuffer> bufferedMessages = new List<NetBuffer>(16);

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
            SendMessage(message);
        }

        #region RPC Processing
        Dictionary<byte, Action<NetIncomingMessage, NetMessageInfo>> RPCProcessors = new Dictionary<byte, Action<NetIncomingMessage, NetMessageInfo>>();

        /// <summary>
        /// Subscribe to an rpc
        /// </summary>
        /// <param name="rpcID">id of the rpc</param>
        /// <param name="rpcProcessor">action to process the rpc with</param>
        /// <param name="overwriteExisting">overwrite the existing processor if one exists.</param>
        /// <returns>Whether or not the rpc was subscribed to. Will return false if an existing rpc was attempted to be subscribed to, and overwriteexisting was set to false</returns>
        public bool SubscribeToRPC(byte rpcID, Action<NetIncomingMessage, NetMessageInfo> rpcProcessor, bool overwriteExisting = true)
        {
            if (rpcProcessor == null)
                throw new ArgumentNullException("rpcProcessor", "the processor delegate cannot be null");
            if (overwriteExisting)
            {
                RPCProcessors[rpcID] = rpcProcessor;
                return true;
            }
            else
            {
                Action<NetIncomingMessage, NetMessageInfo> checkExist;
                if (RPCProcessors.TryGetValue(rpcID, out checkExist))
                {
                    return false;
                }
                else
                {
                    RPCProcessors.Add(rpcID, checkExist);
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
            RPCProcessors.Remove(rpcID);
        }

        internal void CallRPC(byte rpcID, NetIncomingMessage message, NetMessageInfo info)
        {
            Action<NetIncomingMessage, NetMessageInfo> processor;
            if (RPCProcessors.TryGetValue(rpcID, out processor))
            {
                if (processor != null)
                    processor(message, info);
                else
                {
                    Debug.LogWarning("RPC processor for {0} was null. Automatically cleaning up. Please be sure to clean up after yourself in the future.", rpcID);
                    RPCProcessors.Remove(rpcID);
                }
            }
            else
            {
                Debug.LogWarning("Room {1}: unhandled RPC {0}", rpcID, name);
                info.continueForwarding = false;
            }
        }

        #endregion

        /// <summary>
        /// called when a player enters the room
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnPlayerEnter(Player player) { }
        /// <summary>
        /// called when a player exists the room
        /// </summary>
        /// <param name="player"></param>
        public virtual void OnPlayerExit(Player player) { }

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
        protected Dictionary<ushort, NetworkedSceneObjectView> SceneObjects { get { return roomObjects; } }

        internal NetworkedSceneObjectView IncomingObjectRPC(NetIncomingMessage msg)
        {
 	        var viewId = msg.ReadUInt16();
            var rpcId = msg.ReadByte();
            NetworkedSceneObjectView view;
            if (roomObjects.TryGetValue(viewId, out view))
            {
                view.CallRPC(rpcId, msg, new NetMessageInfo(){player = msg.SenderConnection.Tag as Player, mode = RPCMode.Server});
            }

            return view;
        }
    }
}
