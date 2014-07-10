using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;

namespace PNetS
{
    public static partial class PNetServer
    {
        /// <summary>
        /// Start a coroutine
        /// Not thread safe
        /// </summary>
        /// <param name="routine"></param>
        /// <returns></returns>
        public static Coroutine StartCoroutine(IEnumerator routine)
        {
            return GameState.StartCoroutine(routine);
        }

        private static int _lastFrameSize = 16;
        static void Update()
        {
            //is reinstantiating faster? are we dealing with enough messages to make a difference?
            var messages = new List<NetIncomingMessage>(_lastFrameSize * 2);
            int counter = peer.ReadMessages(messages);
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
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.DiscoveryRequest)
                {
                    NetOutgoingMessage resp = peer.CreateMessage();
                    if (OnDiscoveryRequest != null)
                    {
                        OnDiscoveryRequest(resp);
                        peer.SendDiscoveryResponse(resp, msg.SenderEndPoint);
                    }
                }
                else if (msg.MessageType == NetIncomingMessageType.DebugMessage)
                {
                    Debug.Log(msg.ReadString());
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    Debug.LogWarning(msg.ReadString());
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionLatencyUpdated)
                {
                    var latency = msg.ReadFloat();
                    //todo: do something with this latency.
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ErrorMessage)
                {
                    Debug.LogError(msg.ReadString());
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionApproval)
                {
                    var tag = new Player();
                    tag.connection = msg.SenderConnection;
                    msg.SenderConnection.Tag = tag;

                    if (ApproveConnection != null)
                    {
                        try
                        {
                            ApproveConnection(msg);
                        }
                        catch (Exception e)
                        {
                            msg.SenderConnection.Deny();
                            Debug.LogError(e.Message);
                        }
                    }
                    else
                    {
                        msg.SenderConnection.Approve();
                    }
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    var status = (NetConnectionStatus) msg.ReadByte();
                    statusReason = msg.ReadString();
                    Debug.Log("Status: {0}, {1}", status, statusReason);

                    if (status == NetConnectionStatus.Connected)
                    {
                        AddPlayer(msg.SenderConnection);
                    }
                    else if (status == NetConnectionStatus.Disconnected)
                    {
                        RemovePlayer(msg.SenderConnection);
                    }

                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.Error)
                {
                    Debug.LogError(msg.ReadString()); //this should really never happen...
                    peer.Recycle(msg);
                }
                else
                    peer.Recycle(msg);
            }
        }

        static void LateUpdate()
        {
            peer.FlushSendQueue();
        }
    }
}
