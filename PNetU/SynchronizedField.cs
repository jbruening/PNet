using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using PNet;
using Lidgren.Network;

namespace PNetU
{
    /// <summary>
    /// Creates a field which updates it's value on the network when changed
    /// </summary>
    /// <typeparam name="T">Must be serializable</typeparam>
    public class SynchronizedField<T>
    {
        internal byte FieldId;
        private T m_value;
        private PNetU.NetworkView m_netView;

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
                return m_value;
            }
            set
            {
                if (m_netView.IsMine  && !EqualityComparer<T>.Default.Equals(m_value, value))
                {
                    m_value = value;
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
        internal SynchronizedField(PNetU.NetworkView netView)
        {
            if (netView == null)
            {
                Debug.LogError("Synchronized fields must be initialized with a valid network view!");
                return;
            }
            if (!typeof(T).IsSerializable)
            {
                Debug.LogError("Synchronized field with non serializable Type " + typeof(T));
                return;
            }

            m_netView = netView;
            m_value = (T)Activator.CreateInstance(typeof(T));
            m_netView.SubscribeToSynchronizedField<T>(this);
            SendUpdatedValue();
        }

        internal static SynchronizedField<T> Create(PNetU.NetworkView netView)
        {
            return new SynchronizedField<T>(netView);
        }

        /// <summary>
        /// Destructor
        /// </summary>
        ~SynchronizedField()
        {
            m_netView.UnsubscribeSynchronizedField(FieldId);
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
            if (!m_netView.IsMine)
                return;
            try
            {
                byte[] serializedData = SerializeValue(m_value);
                int msgSize = serializedData.Length + 3;
                NetOutgoingMessage msg = Net.peer.CreateMessage(msgSize);

                msg.Write(m_netView.viewID.guid);
                msg.Write(FieldId);
                msg.Write(serializedData);

                Net.peer.SendMessage(msg, NetDeliveryMethod.ReliableOrdered, Channels.SYNCHED_FIELD);
            }
            catch
            {
                Debug.LogError("Failed to serialize synchronized field value!");
            }
            
        }

        internal void OnReceiveValue(NetIncomingMessage msg)
        {
            byte[] buffer = msg.ReadBytes(msg.LengthBytes - (int)msg.PositionInBytes);

            try
            {
                m_value = DeserializeValue(buffer);
                OnValueUpdated(m_value);
            }
            catch
            {
                Debug.LogError("Failed to deserialize synchronized field value!");
            }
        }
        #endregion
    }
}
