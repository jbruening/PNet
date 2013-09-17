using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PNet;
using Lidgren.Network;

namespace PNetC
{
    /// <summary>
    /// Creates a field which updates it's value on the network when changed
    /// </summary>
    /// <typeparam name="T">Must be serializable</typeparam>
    public class SynchronizedField<T>
    {
        internal byte FieldId;
        private T _value;
        private readonly NetworkView _netView;

        /// <summary>
        /// Gets called when a new value is received over the network
        /// </summary>
        public Action<T> OnValueUpdated = delegate { };

        /// <summary>
        /// Use this to access or set the actual value
        /// </summary>
        public T Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (_netView.IsMine  && !EqualityComparer<T>.Default.Equals(_value, value))
                {
                    _value = value;
                    SendUpdatedValue();
                } 
            }
        }

        /// <summary>
        /// Use to trigger update on reference types
        /// </summary>
        public void Update()
        {
            SendUpdatedValue();
        }

        #region Constructors
        /// <summary>
        /// Initializes the synchronized field
        /// </summary>
        /// <param name="netView">The NetworkView this value belongs to</param>
        internal SynchronizedField(NetworkView netView)
        {
            if (netView == null)
            {
                Debug.LogError(null, "Synchronized fields must be initialized with a valid network view!");
                return;
            }
            if (!typeof(T).IsSerializable)
            {
                Debug.LogError(netView.Manager.Net, "Synchronized field with non serializable Type " + typeof(T));
                return;
            }

            _netView = netView;
            _value = (T)Activator.CreateInstance(typeof(T));
            _netView.SubscribeToSynchronizedField<T>(this);
            SendUpdatedValue();
        }

        internal static SynchronizedField<T> Create(NetworkView netView)
        {
            return new SynchronizedField<T>(netView);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SynchronizedField()
        {
            _netView.UnsubscribeSynchronizedField(FieldId);
        }
        #endregion

        #region Serialization
        private byte[] SerializeValue(T val)
        {
            MemoryStream stream = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();

            formatter.Serialize(stream, val);

            byte[] serVal = stream.ToArray();
            stream.Close();
            return serVal;
        }

        private T DeserializeValue(byte[] val)
        {
            MemoryStream stream = new MemoryStream(val);
            BinaryFormatter formatter = new BinaryFormatter();

            T newVal = (T)formatter.Deserialize(stream);
            stream.Close();
            return newVal;
        }
        #endregion

        #region Network
        
        private void SendUpdatedValue()
        {
            if (!_netView.IsMine)
                return;
            try
            {
                byte[] serializedData = SerializeValue(_value);
                int msgSize = serializedData.Length + 3;
                NetOutgoingMessage msg = _netView.Manager.Net.Peer.CreateMessage(msgSize);

                msg.Write(_netView.ViewID.guid);
                msg.Write(FieldId);
                msg.Write(serializedData);

                _netView.Manager.Net.Peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, Channels.SYNCHED_FIELD);
            }
            catch
            {
                Debug.LogError(_netView.Manager.Net, "Failed to serialize synchronized field value!");
            }
            
        }

        internal void OnReceiveValue(NetIncomingMessage msg)
        {
            byte[] buffer = msg.ReadBytes(msg.LengthBytes - (int)msg.PositionInBytes);

            try
            {
                _value = DeserializeValue(buffer);
                OnValueUpdated(_value);
            }
            catch
            {
                Debug.LogError(_netView.Manager.Net, "Failed to deserialize synchronized field value!");
            }
        }
        #endregion
    }
}
