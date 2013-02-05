using System;
using Lidgren.Network;
using PNet;
using UnityEngine;

public class NetPlayer : NetBehaviour 
{
    void Awake()
    {
        netView.OnFinishedCreation += OnFinishedCreation;
        //TODO: use netView.SetSerializationMethod to set the method used for stream serialization
    }

    private void OnFinishedCreation()
    {
        //TODO: enable/disable things because this is mine.
        
        if (netView.IsMine)
        {
            Debug.Log("network view " + netView.viewID + " is mine");
            
            //The object is mine, so let's stream things to the server
            netView.SetSerializationMethod(SerializeStream);
            netView.StateSynchronization = NetworkStateSynchronization.Unreliable;
            netView.OnDeserializeStream += OnDeserializeStream;
        }
    }

    private void OnDeserializeStream(NetIncomingMessage netIncomingMessage)
    {
        //TODO: deserialize from stream. Will probably be position of the object
    }

    private void SerializeStream(NetOutgoingMessage netOutgoingMessage)
    {
        //TODO: serialize data into the stream. Probably something like position or input or something.
    }

    [Rpc(1)]
    void SimpleMessage(NetIncomingMessage msg)
    {
        Debug.Log("Message from the server on the player: " + msg.ReadString());
    }
}