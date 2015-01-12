using System;
using Lidgren.Network;
using PNet;
using PNetU;
using UnityEngine;

public class NetPlayer : NetBehaviour
{
    [SerializeField]
    private MonoBehaviour[] _behavioursToDisableIfNotMine = null;
    [SerializeField]
    private GameObject[] _objectsToDisableIfNotMine = null;

    void Awake()
    {
        netView.OnFinishedCreation += OnFinishedCreation;
    }

    private void OnFinishedCreation()
    {
        //TODO: enable/disable things because this is mine/not mine.
        
        if (netView.IsMine)
        {
            Debug.Log("network view " + netView.viewID + " is mine", netView);
            
            //The object is mine, so let's stream things to the server
            netView.SetSerializationMethod(SerializeStream);
            //turn serialization on
            netView.StateSynchronization = NetworkStateSynchronization.Unreliable;
            //but do not subscribe to deserialization, as we want to ignore stuff for ourselves
        }
        else
        {
            //subscribe to the stream from the server for objects owned by others.
            netView.OnDeserializeStream += OnDeserializeStream;

            //Anything in this array shouldn't be enabled, so disable them
            foreach (var comp in _behavioursToDisableIfNotMine)
            {
                comp.enabled = false;
            }
            foreach (var gobj in _objectsToDisableIfNotMine)
            {
                gobj.SetActive(false);
            }
        }
    }

    private readonly Vector3Serializer _serializer = new Vector3Serializer();

    private void OnDeserializeStream(NetIncomingMessage netIncomingMessage)
    {
        //deserialize position from the stream
        //TODO: implement smoothing/lag compensation
        _serializer.OnDeserialize(netIncomingMessage);
        transform.position = _serializer.vector3;
    }

    private void SerializeStream(NetOutgoingMessage netOutgoingMessage)
    {
        //send our position to the server
        //this should only be happening on an object that is ours
        _serializer.vector3 = transform.position;
        _serializer.OnSerialize(netOutgoingMessage);
    }

    //TODO: use a shared library with client and server with const byte fields
    //then use those in the Rpc attributes, so you have names instead of numbers
    [Rpc(1)]
    void SimpleMessage(NetIncomingMessage msg)
    {
        Debug.Log("Message from the server on the player: " + msg.ReadString(), this);

        netView.RPC(7, RPCMode.Server, StringSerializer.Instance.Update("This rpc goes only to the corresponding gameobject/component on the server"));
    }
}