using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using PNet;
using PNetU;
using Lidgren.Network;
using System.Xml.Serialization;

/// <summary>
/// Objects that exist in a scene with pre-synchronized network id's
/// </summary>
[AddComponentMenu("PNet/Networked Scene Object")]
public abstract class NetworkedSceneObject : MonoBehaviour
{
    /// <summary>
    /// The scene/room Network ID of this item. Should unique per room
    /// </summary>
    public int NetworkID = 0;

    private bool _lastEnableState;
    private static bool _hasResetDictionary = false;
    void Awake()
    {
        if (!_hasResetDictionary)
        {
            sceneObjects = new Dictionary<int, NetworkedSceneObject>();
            _hasResetDictionary = true;
        }


        _lastEnableState = this.enabled;
        this.enabled = true;
        
    }

    void Start()
    {
        sceneObjects[NetworkID] = this;
        this.enabled = _lastEnableState;
    }

    #region RPC Processing
    Dictionary<byte, Action<NetIncomingMessage>> RPCProcessors = new Dictionary<byte, Action<NetIncomingMessage>>();

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
                Debug.LogWarning("RPC processor for " + rpcID + " was null. Automatically cleaning up. Please be sure to clean up after yourself in the future.");
                RPCProcessors.Remove(rpcID);
            }
        }
        else
        {
            Debug.LogWarning("NetworkedSceneObject on " + gameObject.name + ": unhandled RPC " + rpcID);
        }
    }

    #endregion

    /// <summary>
    /// Send an rpc to the server
    /// </summary>
    /// <param name="rpcID"></param>
    /// <param name="args"></param>
    public void RPC(byte rpcID, params INetSerializable[] args)
    {
        var size = 3;
        RPCUtils.AllocSize(ref size, args);

        var message = Net.peer.CreateMessage(size);
        message.Write((ushort)NetworkID);
        message.Write(rpcID);
        RPCUtils.WriteParams(ref message, args);

        Net.peer.SendMessage(message, NetDeliveryMethod.ReliableOrdered, Channels.OBJECT_RPC);
    }

    /// <summary>
    /// serialize this into a string
    /// </summary>
    /// <returns></returns>
    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.AppendLine("-NetworkedSceneObject-");
        sb.Append("id: ").Append(NetworkID).AppendLine(";");
        sb.Append("type:").Append(this.GetType().Name).AppendLine(";");
        sb.Append("data:").Append(SerializeObjectData()).AppendLine(";");
        sb.Append("pos:").Append(transform.position.ToString()).AppendLine(";");
        sb.Append("rot:").Append(transform.rotation.ToString()).AppendLine(";");

        return sb.ToString();
    }

    /// <summary>
    /// Get the data to serialize for this scene object
    /// </summary>
    /// <returns></returns>
    protected abstract string SerializeObjectData();

    void OnDestroy()
    {
        _hasResetDictionary = false;
    }
    internal static Dictionary<int, NetworkedSceneObject> sceneObjects = new Dictionary<int, NetworkedSceneObject>();
}