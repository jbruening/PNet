using System;
using System.Collections.Generic;
using System.Threading;
using Lidgren.Network;
using PNetS;

namespace ExampleServer
{
    class Program
    {
        static void Main(string[] args)
        {

            PNetServer.InitializeServer(
                Properties.Settings.Default.MaximumPlayers, 
                Properties.Settings.Default.ListenPort);

            PNetServer.ApproveConnection = ApproveConnection;
            PNetServer.OnPlayerConnected += OnPlayerConnected;
            PNetServer.OnPlayerDisconnected += delegate(Player player) { Debug.Log("player {0} disconnected", player.Id); };
            
            //If you want a global 'update' function, assign it here
            GameState.update += Update;

            Debug.logger = new DefaultConsoleLogger();

            //TODO: make some Room child classes, and load them into the _rooms dictionary
            Room newRoom = Room.CreateRoom("basic room");
            _room = newRoom.AddBehaviour<BasicRoom>();
            //loading of other data as well

            //Finish starting the server. Started in a new thread so that the console can sit open and still accept input
            _serverThread = new Thread(() => PNetServer.Start(Properties.Settings.Default.FrameTime));
            _serverThread.Start();

            //let the console sit open, waiting for a quit
            //this will throw errors if the program isn't running as a console app, like on unix as a background process
            //recommend including Mono.Unix.Native, and separately handling unix signals if this is running on unix.
            //you could also write a service, and run things that way. (Might also work on Unix better)
            while(true)
            {
                //This will throw errors on linux if not attached to a terminal
                var input = Console.ReadLine();

                if (input == "quit")
                    break;

                Thread.Sleep(100);
            }
            //we're exiting. close the server thread.
            if (_serverThread.IsAlive) _serverThread.Abort();
        }

        //This is called AFTER a connection has been approved
        private static void OnPlayerConnected(Player player)
        {
            //TODO: you could save where the player was last when they dc'd
            //then in here, move them to that room again. (if you have an mmo or something)
            //or move them to a lobby room
            //or a character select
            Debug.Log("player {0} connected", player.Id);
            player.ChangeRoom(_room.Room);
        }

        private static BasicRoom _room;
        private static Thread _serverThread;

        //main loop. run once every game tick.
        private static void Update()
        {
            //Approve connections that are waiting
            foreach (var client in _clientsWaitingToBeApproved)
            {
                client.Approve();
                //TODO: maybe deny clients if their login credentials weren't valid?
            }
        }

        //This is called very first when a client connects to the server, before OnPlayerConnected
        //if you don't want the clients to use the HailMessage, just call netIncomingMessage.SenderConnection.Approve here
        //alternatively, don't even assign the ApproveConnection delegate, and it will just auto-approve
        private static void ApproveConnection(NetIncomingMessage netIncomingMessage)
        {
            //TODO: If you have login data, read it from netIncomingMessage.
            //netIncomingMessage is the data that was serialized in the WriteHailMessage on the client
            _clientsWaitingToBeApproved.Add(netIncomingMessage.SenderConnection);
        }

        private static readonly List<NetConnection> _clientsWaitingToBeApproved = new List<NetConnection>();
    }
}
