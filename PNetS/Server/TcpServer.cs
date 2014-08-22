using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PNetS.Utils;

namespace PNetS.Server
{
    internal class TcpServer
    {
        private Socket _listener;
        private Thread _listenThread;
        private IPEndPoint _localEp;

        private bool _listening = true;
        private readonly ManualResetEvent _allDone = new ManualResetEvent(false);

        public int MaxMessageSize = int.MaxValue;

        public readonly ConcurrentQueue<NetworkMessage> ReceiveQueue = new ConcurrentQueue<NetworkMessage>();
        /// <summary>
        /// called on events becoming available in the ReceiveQueue.
        /// This will not run on any particular thread.
        /// </summary>
        public event Action MessageReceived;
        public readonly ConcurrentQueue<NetworkMessage> SendQueue = new ConcurrentQueue<NetworkMessage>();

        public readonly ConcurrentDictionary<EndPoint, Socket> Clients = new ConcurrentDictionary<EndPoint, Socket>();

        public TcpServer()
        {
            _statePool = new Pool<NetworkMessage>(CreateNetworkMessage);
        }

        public void Start(int port)
        {
            _localEp = new IPEndPoint(IPAddress.Any, port);

            _listener = new Socket(_localEp.Address.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp);

            _listenThread = new Thread(ListenConnections);
            _listenThread.Start();
        }

        void ListenConnections()
        {
            try
            {
                _listener.Bind(_localEp);
                _listener.Listen(10);

                while (_listening)
                {
                    _allDone.Reset();

                    Debug.Log("[TCP] Waiting for connection");
                    _listener.BeginAccept(
                        AcceptCallback,
                        _listener);

                    _allDone.WaitOne();
                }

                _listener.Close(1);
            }
            catch (Exception e)
            {
                Debug.LogError("[TCP] {0}", e);
                _listener.Close();
            }

            Debug.Log("[TCP] closing");
        }

        public void Stop()
        {
            _listening = false;
        }

        public NetworkMessage GetMessageForSend()
        {
            return _statePool.GetItem();
        }

        public void SendMessage(NetworkMessage msg)
        {
            var bytes = new byte[msg.MessageSize + 4];
            //little endian
            bytes[0] = (byte) msg.MessageSize;
            bytes[1] = (byte) (msg.MessageSize >> 8);
            bytes[2] = (byte)(msg.MessageSize >> 16);
            bytes[3] = (byte)(msg.MessageSize >> 24);
            Buffer.BlockCopy(msg.Message, 0, bytes, 4, msg.MessageSize);

            
            RecycleMessage(msg);
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            var listener = (Socket)ar.AsyncState;
            var handler = listener.EndAccept(ar);

            //allow listening to continue now that we've obtained the client's connection
            _allDone.Set();

            if (!Clients.TryAdd(handler.RemoteEndPoint, handler))
            {
                handler.Disconnect(false);
                Debug.LogError("Could not add client {0}", handler.RemoteEndPoint);
                return;
            }

            // Create the state object.
            var state = _statePool.GetItem();
            state.WorkSocket = handler;
            handler.BeginReceive(state.Buffer, 0, NetworkMessage.BufferSize, 0,
                ReadCallback, state);
        }

        private void ReadCallback(IAsyncResult ar)
        {
            var state = (NetworkMessage)ar.AsyncState;
            var handler = state.WorkSocket;

            // Read data from the client socket.
            int read = handler.EndReceive(ar);

            // Data was read from the client socket.
            if (read > 0)
            {
                state.ConsumeBuffer(read);
                if (state.IsValidMessage)
                {
                    FinalizeMessage(state);
                    state = _statePool.GetItem();
                    state.WorkSocket = handler;
                }

                handler.BeginReceive(state.Buffer, 0, NetworkMessage.BufferSize, 0,
                    ReadCallback, state);
            }
            else
            {
                //disconnection.
                if (state.IsValidMessage)
                {
                    FinalizeMessage(state);
                }
                else
                {
                    RecycleMessage(state);
                }
                Socket removed;
                if (!Clients.TryRemove(handler.RemoteEndPoint, out removed))
                {
                    Debug.LogError("Could not remove client {0}", handler.RemoteEndPoint);
                }

                handler.Close();
            }
        }

        void FinalizeMessage(NetworkMessage state)
        {
            if (state.IsValidMessage)
            {
                ReceiveQueue.Enqueue(state);
                Task.Factory.StartNew(InvokeMessageReceive);
            }
            else
            {
                Debug.LogError("State was not valid. Expected {0}, but got {1}", state.MessageSize, state.ReadPos - 4);
                _statePool.Recycle(state);
            }
        }

        void InvokeMessageReceive()
        {
            var msgRecv = MessageReceived;
            if (msgRecv != null)
                msgRecv();
        }

        internal void RecycleMessage(NetworkMessage state)
        {
            _statePool.Recycle(state);
        }

        readonly Pool<NetworkMessage> _statePool;

        NetworkMessage CreateNetworkMessage()
        {
            return new NetworkMessage();
        }

        internal class NetworkMessage : IPoolItem
        {
            public Socket WorkSocket = null;
            public int ReadPos = 0;
            public const int BufferSize = 1024;
            public int MessageSize = 0;
            public byte[] Message = new byte[BufferSize];
            public byte[] Buffer = new byte[BufferSize];

            public bool IsValidMessage
            {
                get { return ReadPos == (4 + MessageSize); }
            }

            public void Reset()
            {
                MessageSize = 0;
                WorkSocket = null;
            }

            internal void ConsumeBuffer(int read)
            {
                var i = 0;
                while (ReadPos < 4 && i < read)
                {
                    MessageSize ^= Buffer[i] << (ReadPos * 8);
                    ReadPos++;
                    i++;
                }

                if (ReadPos < 4) return;

                var bufferRead = read - i;
                var dstOffset = ReadPos - 4;
                if (dstOffset + bufferRead > Message.Length)
                {
                    //need to resize NetworkMessage to fit the larger byte array.
                    Array.Resize(ref Message, Math.Max(Message.Length * 2, dstOffset + bufferRead));
                }

                System.Buffer.BlockCopy(Buffer, i, Message, dstOffset, bufferRead);
                ReadPos += bufferRead;
            }
        }
    }
}
