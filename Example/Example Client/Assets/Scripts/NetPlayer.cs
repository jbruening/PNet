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
        }
    }

    [Rpc(1)]
    void SimpleMessage(NetIncomingMessage msg)
    {
        Debug.Log("Message from the server on the player: " + msg.ReadString());
    }
}