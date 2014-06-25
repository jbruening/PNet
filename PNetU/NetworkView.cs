using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PNetC;
using UnityEngine;
using Lidgren.Network;
using PNet;
using System.Collections;
using System.Reflection;
using Debug = UnityEngine.Debug;
using NetworkStateSynchronization = UnityEngine.NetworkStateSynchronization;
using Object = UnityEngine.Object;

namespace PNetU
{
    /// <summary>
    /// network synchronization
    /// </summary>
    [AddComponentMenu("PNet/Network View")]
    public partial class NetworkView : MonoBehaviour
    {
        private PNetC.NetworkView _networkView;
        internal void SetNetworkView(PNetC.NetworkView netView)
        {
            _networkView = netView;
            IsMine = netView.IsMine;
            OwnerId = netView.OwnerId;
            viewID = netView.ViewID;

            netView.StateSynchronization = _stateSynchronization.ToPNetC();

            _networkView.OnDeserializeStream += StreamDeserializeCaller;
            _networkView.OnRemove += DoOnRemove;

            if (_queuedSer != null)
            {
                _networkView.SetSerializationMethod(_queuedSer, _queuedStreamSize);
            }

            var components = gameObject.GetComponents<MonoBehaviour>().OrderBy(c => c.name);

            foreach (var component in components)
            {
                SubscribeMarkedRPCsOnComponent(component);
                SubscribeSynchronizedFields(component);
            }
        }

        /// <summary>
        /// Send an rpc
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="mode"></param>
        /// <param name="args"></param>
        public void RPC(byte rpcID, UnityEngine.RPCMode mode, params INetSerializable[] args)
        {
            _networkView.RPC(rpcID, mode.ToPNetCMode(), args);
        }

        /// <summary>
        /// Send an rpc to the owner of this object
        /// </summary>
        /// <param name="rpcID"></param>
        /// <param name="args"></param>
        public void RPCToOwner(byte rpcID, params INetSerializable[] args)
        {
            _networkView.RPCToOwner(rpcID, args);
        }

        #region serialization
        void Start()
        {
            if (_stateSynchronization != NetworkStateSynchronization.Off && !_isSerializing)
            {
                _isSerializing = true;
                StartCoroutine(Serialize());
            }
        }
        /// <summary>
        /// stream size. Helps prevent array resizing
        /// </summary>
        public int defaultStreamSize { get { return _networkView.DefaultStreamSize; } set { _networkView.DefaultStreamSize = value; } }
        /// <summary>
        /// set the method to be used during stream serialization
        /// </summary>
        /// <param name="newMethod"></param>
        /// <param name="defaultStreamSize"></param>
        public void SetSerializationMethod(Action<NetOutgoingMessage> newMethod, int defaultStreamSize = 16)
        {
            if (newMethod == null) return;
            if (_networkView == null)
            {
                _queuedSer = newMethod;
                _queuedStreamSize = defaultStreamSize;
                return;
            }

            _networkView.SetSerializationMethod(newMethod, defaultStreamSize);
            this.defaultStreamSize = defaultStreamSize;
        }

        private Action<NetOutgoingMessage> _queuedSer;
        private int _queuedStreamSize;


        private NetworkStateSynchronization _stateSynchronization = NetworkStateSynchronization.Off;
        private bool _isSerializing = false;
        /// <summary>
        /// method of serialization
        /// </summary>
        public NetworkStateSynchronization StateSynchronization
        {
            get
            {
                return _stateSynchronization;
            }
            set
            {
                _stateSynchronization = value;

                if (_networkView != null)
                    _networkView.StateSynchronization = _stateSynchronization.ToPNetC();

                if (_stateSynchronization != NetworkStateSynchronization.Off && !_isSerializing)
                {
                    _isSerializing = true;
                    StartCoroutine(Serialize());
                }
                else if (_stateSynchronization == NetworkStateSynchronization.Off && _isSerializing)
                {
                    _isSerializing = false;
                }
            }
        }

        void OnEnable()
        {
            //the behaviour has become active again. Check if we have serialization running, and if not, start it up again.
            //otherwise a disable/enable of the behaviour/gameobject will kill the serialization.

            if (_stateSynchronization != NetworkStateSynchronization.Off && !_isSerializing)
            {
                _isSerializing = true;
                StartCoroutine(Serialize());
            }
        }

        private void StreamDeserializeCaller(NetIncomingMessage msg)
        {
            if (OnDeserializeStream != null)
            {
                OnDeserializeStream(msg);
            }
        }


        /// <summary>
        /// subscribe to this in order to deserialize streaming data
        /// </summary>
        public Action<NetIncomingMessage> OnDeserializeStream;

        IEnumerator Serialize()
        {
            while (_isSerializing)
            {
                if (SerializationTime < 0.01f)
                    SerializationTime = 0.01f;

                if (this.enabled)
                {
                    _networkView.DoStreamSerialize();
                }
                yield return new WaitForSeconds(SerializationTime);
            }
        }

        /// <summary>
        /// Time between each stream send serialization
        /// </summary>
        public float SerializationTime = 0.05f;

        #endregion

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
                            " does not match the RPC delegate of Action<NetIncomingMessage>, but is marked to process RPC's. Please either fix this method, or remove the attribute", 
                            behaviour);
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

        /// <summary>
        /// Subscribe to an rpc
        /// </summary>
        /// <param name="rpcID">id of the rpc</param>
        /// <param name="rpcProcessor">action to process the rpc with</param>
        /// <param name="overwriteExisting">overwrite the existing processor if one exists.</param>
        /// <returns>Whether or not the rpc was subscribed to. Will return false if an existing rpc was attempted to be subscribed to, and overwriteexisting was set to false</returns>
        public bool SubscribeToRPC(byte rpcID, Action<NetIncomingMessage> rpcProcessor, bool overwriteExisting = true)
        {
            return _networkView.SubscribeToRPC(rpcID, rpcProcessor, overwriteExisting);
        }

        /// <summary>
        /// Unsubscribe from an rpc
        /// </summary>
        /// <param name="rpcID"></param>
        public void UnsubscribeFromRPC(byte rpcID)
        {
            _networkView.UnsubscribeFromRPC(rpcID);
        }

        /// <summary>
        /// Subscribe to this to know when an object is being destroyed by the server.
        /// </summary>
        public event Action<byte> OnRemove;
        /// <summary>
        /// run once we've finished setting up the networkview variables
        /// </summary>
        public event Action OnFinishedCreation;

        /// <summary>
        /// Whether or not to destroy the gameobject this is attached to when destroying the networkview
        /// </summary>
        public bool DestroyGameObjectOnNetworkDestroy = true;

        private void OnDestroy()
        {            
            OnRemove = null;
            OnFinishedCreation = null;
            OnDeserializeStream = null;

            CleanupNetView();
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
        /// ID of the owner. 0 is the server.
        /// </summary>
        public ushort OwnerId { get; internal set; }

        #endregion

        internal void DoOnFinishedCreation()
        {
            if (OnFinishedCreation != null) OnFinishedCreation();
            OnFinishedCreation = null;
        }

        internal void DoOnRemove(byte reasonCode)
        {
            CleanupNetView();

            if (DestroyGameObjectOnNetworkDestroy)
            {
                UnityDebugLogger.Full("Network Destruction. Destroying networkview and gameobject", this);
                Destroy(gameObject);
            }
            else
            {
                UnityDebugLogger.Full("Network destruction. Only destroying networkview", this);
                Destroy(this);
            }

            if (OnRemove == null) return;
            
            try
            {
                OnRemove(reasonCode);
                OnRemove = null;
            }
            catch (Exception e)
            {
                Debug.LogError(e, this);
            }
        }

        void CleanupNetView()
        {
            if (_networkView != null) //this will get run usually if we're switching scenes
            {
                if (UnityEngineHook.ValidInstance)
                    UnityEngineHook.Instance.Manager.Remove(_networkView.ViewID);

                _networkView.OnDeserializeStream -= StreamDeserializeCaller;
                _networkView.OnRemove -= DoOnRemove;

                _networkView = null;
            }
        }
    }
}
