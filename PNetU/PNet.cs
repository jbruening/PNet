using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PNetC;
using UnityEngine;
using Lidgren.Network;
using PNet;
using System.ComponentModel;
using Debug = UnityEngine.Debug;
using NetworkLogLevel = UnityEngine.NetworkLogLevel;

namespace PNetU
{
    /// <summary>
    /// Networking class
    /// </summary>
    public static class Net
    {
        /// <summary>
        /// resource caching for instantiation
        /// </summary>
        public static bool resourceCaching;

        /// <summary>
        /// the PNetC.Net instance used by this static class
        /// </summary>
        public static PNetC.Net Peer { get; private set; }
        
        static Net()
        {
            //do da setup
            Peer = new PNetC.Net(UnityEngineHook.Instance);
            PNetC.Debug.logger = new UnityDebugLogger();
        }

        #region PNetC.Net instance -> PNetU.Net static bindings

        /// <summary>
        /// When the room is changing
        /// </summary>
        public static event Action<string> OnRoomChange
        {
            add { Peer.OnRoomChange += value; }
            remove { Peer.OnRoomChange -= value; }
        }

        /// <summary>
        /// When disconnected from the server
        /// </summary>
        public static event Action OnDisconnectedFromServer
        {
            add { Peer.OnDisconnectedFromServer += value; }
            remove { Peer.OnDisconnectedFromServer -= value; }
        }

        /// <summary>
        /// When finished connecting to the server
        /// </summary>
        public static event Action OnConnectedToServer
        {
            add { Peer.OnConnectedToServer += value; }
            remove { Peer.OnConnectedToServer -= value; }
        }

        /// <summary>
        /// The function to use for writing the connect data (username/password/etc)
        /// </summary>
        public static Action<NetOutgoingMessage> WriteHailMessage
        {
            get { return Peer.WriteHailMessage; }
            set { Peer.WriteHailMessage = value; }
        }

        /// <summary>
        /// subscribe to this in order to receive static RPC's from the server. you need to manually process them.
        /// </summary>
        public static event Action<byte, NetIncomingMessage> ProcessRPC
        {
            add { Peer.ProcessRPC += value; }
            remove { Peer.ProcessRPC -= value; }
        }

        /// <summary>
        /// When a discovery response is received
        /// </summary>
        public static event Action<NetIncomingMessage> OnDiscoveryResponse
        {
            add { Peer.OnDiscoveryResponse += value; }
            remove { Peer.OnDiscoveryResponse -= value; }
        }

        /// <summary>
        /// pause the processing of the network queue
        /// </summary>
        public static bool IsMessageQueueRunning { get { return Peer.IsMessageQueueRunning; } set { Peer.IsMessageQueueRunning = value; } }
        
        /// <summary>
        /// latest status
        /// </summary>
        public static NetConnectionStatus Status{get { return Peer.Status; }}
        
        /// <summary>
        /// reason for the most latest status
        /// </summary>
        public static string StatusReason { get { return Peer.StatusReason; } }

        /// <summary>
        /// The Network ID of this client
        /// </summary>
        public static ushort PlayerId { get { return Peer.PlayerId; } }

        /// <summary>
        /// last received latency value from the lidgren's calculations
        /// </summary>
        public static float Latency { get { return Peer.Latency; } }

        /// <summary>
        /// Connect with the specified configuration
        /// </summary>
        /// <param name="configuration"></param>
        public static void Connect(ClientConfiguration configuration)
        {
            Peer.Connect(configuration);
        }

        /// <summary>
        /// Disconnect if connected
        /// </summary>
        public static void Disconnect()
        {
            Peer.Disconnect();
        }

        /// <summary>
        /// Send an rpc to the server
        /// </summary>
        /// <param name="rpcId"></param>
        /// <param name="args"></param>
        public static void RPC(byte rpcId, params INetSerializable[] args)
        {
            Peer.RPC(rpcId, args);
        }

        /// <summary>
        /// Run this when the room changing has completed (tells the server you're actually ready to be in a room)
        /// </summary>
        public static void FinishedRoomChange()
        {
            Peer.FinishedRoomChange();
        }
        #endregion
    }
}
