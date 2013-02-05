using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            GameState.update += Update;

            Debug.logger = new DefaultConsoleLogger();

            //TODO: make some Room child classes, and load them into the _rooms dictionary
            //loading of other data as well

            //Finish starting the server. Started in a new thread so that the console can sit open and still accept input
            _serverThread = new Thread(() => PNetServer.Start(Properties.Settings.Default.FrameTime));
            _serverThread.Start();

            //let the console sit open, waiting for a quit
            //this will throw errors if the program isn't running as a console app, like on unix as a background process
            //recommend including Mono.Unix.Native, and separately handling unix signals if this is running on unix.
            while(true)
            {
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
            //TODO: move the player to a room maybe?
            Debug.Log("player {0} connected", player.Id);
        }

        private Dictionary<string, Room> _rooms;
        private static Thread _serverThread;

        //main loop. run once every game tick.
        private static void Update()
        {
            
            //Maybe finish approving connections that are waiting?
        }

        //This is called very first when a client connects to the server, but before OnPlayerConnected
        private static void ApproveConnection(NetIncomingMessage netIncomingMessage)
        {
            netIncomingMessage.SenderConnection.Approve();

            //Other things you can do: spawn a separate thread that eventually decides if a connection should be approved or not, 
            //then approve the connection in the gameupdate loop
        }
    }
}
