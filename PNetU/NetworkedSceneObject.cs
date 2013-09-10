using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using PNet;
using PNetU;
using Lidgren.Network;

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

    PNetC.NetworkedSceneObject sceneObject;

    void Awake()
    {
        sceneObject = new PNetC.NetworkedSceneObject(NetworkID);
    }

    #region RPC Processing
    /// <summary>
    /// Subscribe to an rpc
    /// </summary>
    /// <param name="rpcID">id of the rpc</param>
    /// <param name="rpcProcessor">action to process the rpc with</param>
    /// <param name="overwriteExisting">overwrite the existing processor if one exists.</param>
    /// <returns>Whether or not the rpc was subscribed to. Will return false if an existing rpc was attempted to be subscribed to, and overwriteexisting was set to false</returns>
    public bool SubscribeToRPC(byte rpcID, Action<NetIncomingMessage> rpcProcessor, bool overwriteExisting = true)
    {
        return sceneObject.SubscribeToRPC(rpcID, rpcProcessor, overwriteExisting);
    }

    /// <summary>
    /// Unsubscribe from an rpc
    /// </summary>
    /// <param name="rpcID"></param>
    public void UnsubscribeFromRPC(byte rpcID)
    {
        sceneObject.UnsubscribeFromRPC(rpcID);
    }
    #endregion

    /// <summary>
    /// Send an rpc to the server
    /// </summary>
    /// <param name="rpcID"></param>
    /// <param name="args"></param>
    public void RPC(byte rpcID, params INetSerializable[] args)
    {
        sceneObject.RPC(rpcID, args);
    }

    /// <summary>
    /// serialize this into a string
    /// </summary>
    /// <returns></returns>
    public string Serialize()
    {
        var sb = new StringBuilder();
        sb.AppendLine(sceneObject.Serialize());
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
}