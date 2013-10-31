using System;
using Lidgren.Network;
using Debug = UnityEngine.Debug;
using UnityEngine;
using Net = PNetU.Net;

public class ExamplePNet : MonoBehaviour
{

    public string ip = "127.0.0.1";
    public int port = 14000;

    private static ExamplePNet _singleton;

    void Awake()
    {
        //singleton behaviour, to prevent multiple event subscriptions/connectings
        if (_singleton)
        {
            Destroy(this);
            return;
        }

        _singleton = this;
        DontDestroyOnLoad(this);

        Net.OnRoomChange += OnRoomChange;
        Net.OnFailedToConnect += NetOnOnFailedToConnect;
        Net.OnDisconnectedFromServer += OnDisconnectedFromServer;
        Net.WriteHailMessage = WriteHailMessage;
        Net.ProcessRPC += ProcessRpc;
    }

    private void NetOnOnFailedToConnect(string s)
    {
        Debug.LogError("Failed to connect. " + s);
    }

    private void WriteHailMessage(NetOutgoingMessage netOutgoingMessage)
    {
        //TODO: serialize authentication information to the netoutgoingMessage
        //The data written here is what is in the ApproveConnection delegate on the server
    }

    //This is run whenever a room has a Room or server RPC is run
    private void ProcessRpc(byte b, NetIncomingMessage msg)
    {
        Debug.Log("Room rpc " + b + " received");

        //TODO: deserialize data from the msg object
        //this can be done via if/else, switches, or Dictionary<byte, Action<NetIncomingMessage>>.
        //The dictionary is recommended for its cleanness
        //the if/else is fastest if you only have a few Room/Server RPCs
    }

    private void OnDisconnectedFromServer()
    {
        Debug.Log("Disconnected from server");
    }

    private void OnRoomChange(string s)
    {
        Debug.Log("server switched us to room " + s);

        //TODO: this should probably be called after we actually switch scenes, but because we're not changing scenes, we'll call it right now
        Net.FinishedRoomChange();
    }

    private void OnDestroy()
    {
        //This is required for cleanup, as unity can't clean up delegates very well
        Net.OnRoomChange -= OnRoomChange;
        Net.OnDisconnectedFromServer -= OnDisconnectedFromServer;
        Net.WriteHailMessage = null;
        Net.ProcessRPC -= ProcessRpc;
    }

    // Use this for initialization
	void Start ()
	{
	    var config = new PNetC.ClientConfiguration(ip, port);
        Debug.Log("connecting to " + ip + ":" + port);
	    Net.Connect(config);
	}
}