using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
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
    public sealed partial class Room
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
        public string Name { get; set; }
        /// <summary>
        /// Close the room
        /// </summary>
        /// <param name="roomToChangeLeftoverPlayersTo"></param>
        public void Close(Room roomToChangeLeftoverPlayersTo)
        {
            Peer.Shutdown("Room closing");
            try
            {
                for (int i = 0; i < _roomBehaviours.Count; ++i)
                {
                    _roomBehaviours[i].Closing();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Room Closing] {0}: {1}", Name, e);
            }

            if (roomToChangeLeftoverPlayersTo == null)
            {
                foreach (var player in players)
                {
                    player.Disconnect("Room closing");
                }
            }
            else
            {
                foreach (var player in players)
                {
                    player.ChangeRoom(roomToChangeLeftoverPlayersTo);
                }
            }
            
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

            lock (RoomPorts)
            {
                RoomPorts.Remove(Peer.Configuration.Port - PNetServer.Configuration.ListenPort);
            }

            GC.Collect();
        }

        /// <summary>
        /// called when a player enters the room
        /// </summary>
        /// <param name="player"></param>
        private void OnPlayerEnter(Player player)
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
        /// called when a player exits the room
        /// </summary>
        /// <param name="player"></param>
        private void OnPlayerExit(Player player)
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

        void AddPlayer(Player player)
        {
            player.CurrentRoom = this;
            m_Players.Add(player);

            //statics buffer, network instantiate
            SendBuffer(player.connection);

            //todo: wait with these, until a player actually joins via connecting to the room server.
            // nobjs update
            foreach (var ro in roomObjects)
            {
                ro.Value.UpdateConnections();
            }

            OnPlayerEnter(player);
        }

        internal void RemovePlayer(Player player)
        {
            player.FireLeaveRoom(this);

            m_Players.Remove(player);
            
            //m_Actors.ForEach(a => a.UpdateConnections());

            foreach (var ro in roomObjects)
            {
                ro.Value.UpdateConnections();
            }

            OnPlayerExit(player);

            player.CurrentRoom = null;
        }

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
            GameState.AllRooms.Add(this);
        }

        private static readonly Dictionary<int, Room> RoomPorts = new Dictionary<int, Room>();

        /// <summary>
        /// Create a room
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Room CreateRoom(string name)
        {
            var room = new Room(){Name = name};
            //initialize the room's networking.
            var config = new NetPeerConfiguration(PNetServer.Configuration.AppIdentifier);

            int port = -1;
            int counter = 1;
            lock (RoomPorts)
            {
                //get first available integer
                foreach (var keyValuePair in RoomPorts)
                {
                    if (keyValuePair.Key != counter)
                    {
                        port = counter;
                        break;
                    }
                    counter++;
                }
                if (port == -1)
                {
                    port = counter;
                }
                
                RoomPorts[port] = room;
            }
            config.Port = PNetServer.Configuration.ListenPort + port;
            config.MaximumConnections = PNetServer.Configuration.MaximumConnections;
            config.ReceiveBufferSize = PNetServer.Configuration.ReceiveBuffer;
            config.SendBufferSize = PNetServer.Configuration.SendBuffer;
            config.AutoFlushSendQueue = false;
            config.SetMessageTypeEnabled(NetIncomingMessageType.ConnectionApproval, true);

            room.Peer = new NetServer(config);
            room.Peer.Start();

            return room;
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

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>'s name.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            if (string.IsNullOrWhiteSpace(Name))
                return "PNetS.Room";
            return "Room " + Name;
        }

        internal void OnGameObjectAdded(GameObject gameObject)
        {
            try
            {
                for(int i = 0; i < _roomBehaviours.Count; i++)
                {
                    _roomBehaviours[i].OnGameObjectAdded(gameObject);
                }
            }
            catch(Exception e)
            {
                Debug.LogError("[Room GameObjectAdded] {0}: {1}", Name, e.ToString());
            }
        }
    }
}
