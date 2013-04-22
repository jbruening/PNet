using Lidgren.Network;
using PNetU;
using UnityEngine;

public class ExamplePNet : MonoBehaviour
{

    public string ip = "127.0.0.1";
    public int port = 14000;

    private static readonly ExamplePNet Singleton = null;

    void Awake()
    {
        if (Singleton)
        {
            Destroy(this);
            return;
        }

        DontDestroyOnLoad(this);

        Net.OnRoomChange += OnRoomChange;
        Net.OnDisconnectedFromServer += OnDisconnectedFromServer;
        Net.WriteHailMessage = WriteHailMessage;
        Net.ProcessRPC += ProcessRpc;
    }

    private void WriteHailMessage(NetOutgoingMessage netOutgoingMessage)
    {
        //TODO: serialize authentication information to the netoutgoingMessage
    }

    //This is run whenever a room has a Room or server RPC is run
    private void ProcessRpc(byte b, NetIncomingMessage msg)
    {
        Debug.Log("Room rpc " + b + " received");

        //TODO: deserialize data from the msg object
    }

    private void OnDisconnectedFromServer()
    {
        Debug.Log("Disconnected from server");
    }

    private void OnRoomChange(string s)
    {
        Debug.Log("server switched us to room " + s);

        //TODO: technically, this should be called after we actually switch scenes, but because we're not changing scenes, we'll call it right now
        Net.FinishedRoomChange();
    }

    // Use this for initialization
	void Start ()
	{
	    var config = new ClientConfiguration(ip, port);
	    Net.Connect(config);
	}
}