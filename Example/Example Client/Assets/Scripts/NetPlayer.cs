using System;
using Lidgren.Network;
using PNet;
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
            netView.StateSynchronization = NetworkStateSynchronization.Unreliable;
            netView.OnDeserializeStream += OnDeserializeStream;
        }
        else
        {
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

    private void OnDeserializeStream(NetIncomingMessage netIncomingMessage)
    {
        //TODO: deserialize from stream. Will probably be position of the object
    }

    private void SerializeStream(NetOutgoingMessage netOutgoingMessage)
    {
        //TODO: serialize data into the stream. Probably something like position or input or something.
    }

    //TODO: use a shared library with client and server with const byte fields
    //then use those in the Rpc attributes, so you have names instead of numbers
    [Rpc(1)]
    void SimpleMessage(NetIncomingMessage msg)
    {
        Debug.Log("Message from the server on the player: " + msg.ReadString(), this);
    }
}