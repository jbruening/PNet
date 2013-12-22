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

    PNetC.NetworkedSceneObject _sceneObject;

    /// <summary>
    /// If you override, you need to run SetupPNetC, probably first.
    /// </summary>
    protected void Awake()
    {
        SetupPNetC();
    }

    /// <summary>
    /// Only do this if you override awake
    /// </summary>
    protected void SetupPNetC()
    {
        _sceneObject = new PNetC.NetworkedSceneObject(NetworkID, Net.Peer);
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
        return _sceneObject.SubscribeToRPC(rpcID, rpcProcessor, overwriteExisting);
    }

    /// <summary>
    /// Unsubscribe from an rpc
    /// </summary>
    /// <param name="rpcID"></param>
    public void UnsubscribeFromRPC(byte rpcID)
    {
        _sceneObject.UnsubscribeFromRPC(rpcID);
    }
    #endregion

    /// <summary>
    /// Send an rpc to the server
    /// </summary>
    /// <param name="rpcID"></param>
    /// <param name="args"></param>
    public void RPC(byte rpcID, params INetSerializable[] args)
    {
        _sceneObject.RPC(rpcID, args);
    }

    /// <summary>
    /// serialize this into a string
    /// </summary>
    /// <returns></returns>
    public string Serialize()
    {
        var serObj = new PNetC.NetworkedSceneObject(NetworkID);
        var sb = new StringBuilder();
        sb.Append(serObj.Serialize());
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