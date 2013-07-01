using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Lidgren.Network;
using PNet;
using System.Collections;
using System.Reflection;
using Object = UnityEngine.Object;

namespace PNetU
{
    /// <summary>
    /// network synchronization
    /// </summary>
    [AddComponentMenu("PNet/Network View")]
    public class NetworkView : MonoBehaviour
    {
        

        /// <summary>
        /// Send an rpc
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, UnityEngine.RPCMode mode, params INetSerializable[] args)
        {
            var size = 2;
            RPCUtils.AllocSize(ref size, args);

            var message = Net.peer.CreateMessage(size);
            message.Write(viewID.guid);
            message.Write(rpcID);
            RPCUtils.WriteParams(ref message, args);

            Net.peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, (int)mode + Channels.BEGIN_RPCMODES);
        }

        /// <summary>
        /// Send an rpc to the owner of this object
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="args"></param>
        public void RPCToOwner(byte rpcID, params INetSerializable[] args)
        {
            var size = 3;
            RPCUtils.AllocSize(ref size, args);

            var message = Net.peer.CreateMessage(size);
            message.Write(viewID.guid);
            message.Write(rpcID);
            RPCUtils.WriteParams(ref message, args);

            Net.peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.OWNER_RPC);
        }

        #region serialization
        void Start()
        {
            if (m_StateSynchronization != NetworkStateSynchronization.Off && !m_IsSerializing)
            {
                m_IsSerializing = true;
                StartCoroutine(Serialize());
            }
        }
        /// <summary>
        /// stream size. Helps prevent array resizing
        /// </summary>
        public int defaultStreamSize;
        /// <summary>
        /// set the method to be used during stream serialization
        /// </summary>
        /// <param name="newMethod"></param>
        /// <param name="defaultStreamSize"></param>
        public void SetSerializationMethod(Action<NetOutgoingMessage> newMethod, int defaultStreamSize = 16)
        {
            if (newMethod != null)
            {
                OnSerializeStream = newMethod;
                this.defaultStreamSize = defaultStreamSize;
            }
        }
        Action<NetOutgoingMessage> OnSerializeStream = delegate { };

        private NetworkStateSynchronization m_StateSynchronization = NetworkStateSynchronization.Off;
        private bool m_IsSerializing = false;
        /// <summary>
        /// method of serialization
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

        void OnEnable()
        {
            //the behaviour has become active again. Check if we have serialization running, and if not, start it up again.
            //otherwise a disable/enable of the behaviour/gameobject will kill the serialization.

            if (m_StateSynchronization != NetworkStateSynchronization.Off && !m_IsSerializing)
            {
                m_IsSerializing = true;
                StartCoroutine(Serialize());
            }
        }

        /// <summary>
        /// subscribe to this in order to deserialize streaming data
        /// </summary>
        public Action<NetIncomingMessage> OnDeserializeStream = delegate { };

        IEnumerator Serialize()
        {
            while (m_IsSerializing)
            {
                if (SerializationTime < 0.01f)
                    SerializationTime = 0.01f;

                if (Net.status == NetConnectionStatus.Connected && this.enabled)
                {
                    var nMessage = Net.peer.CreateMessage(defaultStreamSize);
                    nMessage.Write(viewID.guid);
                    OnSerializeStream(nMessage);

                    if (m_StateSynchronization == NetworkStateSynchronization.Unreliable)
                        Net.peer.SendMessage(nMessage, NetDeliveryMethod.Unreliable, Channels.UNRELIABLE_STREAM);
                    else if (m_StateSynchronization == NetworkStateSynchronization.ReliableDeltaCompressed)
                        Net.peer.SendMessage(nMessage, NetDeliveryMethod.ReliableOrdered, Channels.RELIABLE_STREAM);
                }
                yield return new WaitForSeconds(SerializationTime);
            }
        }

        /// <summary>
        /// Time between each stream send serialization
        /// </summary>
        public float SerializationTime = 0.05f;

        #endregion

        void Awake()
        {
            var components = gameObject.GetComponents<MonoBehaviour>().OrderBy(c => c.name);

            foreach (var component in components)
            {
                SubscribeMarkedRPCsOnComponent(component);
                SubscribeSynchronizedFields(component);
            }
        }

        /// <summary>
        /// Subscribe all the marked rpcs on the supplied component
        /// </summary>
        /// <param name="behaviour"></param>
        public void SubscribeMarkedRPCsOnComponent(MonoBehaviour behaviour)
        {
            if (behaviour == this) return;
            if (behaviour == null) return;

            var thisType = behaviour.GetType();

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

                    Action<NetIncomingMessage> del = Delegate.CreateDelegate(typeof(Action<NetIncomingMessage>), behaviour, method, false) as Action<NetIncomingMessage>;

                    if (del != null)
                        SubscribeToRPC(token.rpcId, del);
                    else
                        Debug.LogWarning("The method " + 
                            method.Name + 
                            " for type " + 
                            method.DeclaringType.Name + 
                            " does not match the RPC delegate of Action<NetInComingMessage>, but is marked to process RPC's. Please either fix this method, or remove the attribute");
                }
            }
        }

        internal void SubscribeSynchronizedFields(MonoBehaviour behaviour)
        {
            if (behaviour == this) return;
            if (behaviour == null) return;

            var thisType = behaviour.GetType();

            if (thisType == typeof(NetworkView)) //speedup
                return;
            //get all the methods of the derived type
            var fields = thisType.GetFields(
                BindingFlags.NonPublic |
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.FlattenHierarchy
                ).OrderBy(f => f.Name);

            foreach (FieldInfo field in fields)
            {
                Type fieldType = field.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(SynchronizedField<>))
                {
                    var instance = fieldType.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, 
                        new Type[] { typeof(NetworkView) }, null).Invoke(new object[] { this });
                    field.SetValue(behaviour, instance);
                }
            }
        }

        Dictionary<byte, Action<NetIncomingMessage>> RPCProcessors = new Dictionary<byte,Action<NetIncomingMessage>>();
        IntDictionary<Action<NetIncomingMessage>> FieldProcessors = new IntDictionary<Action<NetIncomingMessage>>();

        /// <summary>
        /// Subscribe to an rpc
        /// </summary>
        /// <param name="rpcID">id of the rpc</param>
        /// <param name="rpcProcessor">action to process the rpc with</param>
        /// <param name="overwriteExisting">overwrite the existing processor if one exists.</param>
        /// <returns>Whether or not the rpc was subscribed to. Will return false if an existing rpc was attempted to be subscribed to, and overwriteexisting was set to false</returns>
        public bool SubscribeToRPC(byte rpcID, Action<NetIncomingMessage> rpcProcessor, bool overwriteExisting = true)
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
                Action<NetIncomingMessage> checkExist;
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

        internal void SubscribeToSynchronizedField<T>(SynchronizedField<T> field)
        {
            int fieldId = FieldProcessors.Add(field.OnReceiveValue);
            field.FieldId = (byte)fieldId;
        }

        internal void UnsubscribeSynchronizedField(int fieldId)
        {
            FieldProcessors.Remove(fieldId);
        }

        /// <summary>
        /// Unsubscribe from an rpc
        /// </summary>
        /// <param name="rpcID"></param>
        public void UnsubscribeFromRPC(byte rpcID)
        {
            RPCProcessors.Remove(rpcID);
        }

        internal void CallRPC(byte rpcID, NetIncomingMessage message)
        {
            Action<NetIncomingMessage> processor;
            if (RPCProcessors.TryGetValue(rpcID, out processor))
            {
                if (processor != null)
                    processor(message);
                else
                {
                    //Debug.LogWarning("RPC processor for " + rpcID + " was null. Automatically cleaning up. Please be sure to clean up after yourself in the future.");
                    RPCProcessors.Remove(rpcID);
                }
            }
            else
            {
                Debug.LogWarning("NetworkView on " + gameObject.name + ": unhandled RPC " + rpcID);
            }
        }

        internal void SetSynchronizedField(byte fieldID, NetIncomingMessage message)
        {
            Action<NetIncomingMessage> processor;
            if (FieldProcessors.HasValue(fieldID))
            {
                processor = FieldProcessors[fieldID];

                if (processor != null)
                    processor(message);
                else
                    FieldProcessors.Remove(fieldID);
            }
            else
                Debug.LogWarning("Unhandled synchronized field " + fieldID);
        }

        /// <summary>
        /// Subscribe to this to know when an object is being destroyed by the server.
        /// </summary>
        public event Action OnRemove = delegate { };
        /// <summary>
        /// run once we've finished setting up the networkview variables
        /// </summary>
        public event Action OnFinishedCreation = delegate { };

        private void Destroy()
        {
            RPCProcessors = null;
            FieldProcessors = null;
            Destroy(gameObject);
        }

        #region NetworkViewID
        /// <summary>
        /// If i'm the owner
        /// </summary>
        public bool IsMine { get; internal set; }
        /// <summary>
        /// identifier for the network view
        /// </summary>
        public NetworkViewId viewID = NetworkViewId.Zero;

        /// <summary>
        /// find a network view based on the given NetworkViewId
        /// </summary>
        /// <param name="viewID"></param>
        /// <returns></returns>
        public static NetworkView Find(NetworkViewId viewID)
        {
            return allViews[viewID.guid];
        }

        /// <summary>
        /// find a networkview based on a networkviewid that was serialized into an rpc
        /// </summary>
        /// <param name="message">uses deserialize, so the read location does advance</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool Find(ref NetIncomingMessage message, out NetworkView view)
        {
            var id = NetworkViewId.Deserialize(message);

            return Find(id, out view);
        }

        internal static bool Find(ushort id, out NetworkView view)
        {
            view = allViews[id];
            if (view != null)
                return true;
            return false;
        }

        internal static void RemoveView(ushort viewId)
        {
            allViews.Remove(viewId);
        }

        static IntDictionary<NetworkView> allViews = new IntDictionary<NetworkView>();

        internal static void DestroyAllViews()
        {
            var cap = allViews.Capacity;
            for(int i = 0; i < cap; i++)
            {
                NetworkView view;
                if (allViews.TryGetValue(i, out view))
                {
                    if (view != null)
                        Destroy(view.gameObject);
                }

                allViews.Remove(i);
            }
        }

        internal static void RegisterView(NetworkView view, ushort viewId)
        {
            allViews.Add(viewId, view);
        }

        /// <summary>
        /// ID of the owner. 0 is the server.
        /// </summary>
        public ushort OwnerId { get; internal set; }

        #endregion

        internal void DoOnFinishedCreation()
        {
            if (OnFinishedCreation != null) OnFinishedCreation();
        }

        internal void DoOnRemove()
        {
            if (OnRemove != null) OnRemove();
        }
    }
}
