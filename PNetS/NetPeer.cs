using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren;
using Lidgren.Network;

namespace PNet
{
    public abstract class NetPeer
    {
        internal abstract Lidgren.Network.NetPeer peer {get;}
        protected abstract Context context { get; }
        protected abstract Configuration configuration { get; }
        protected NetPeerConfiguration netConfig;

        public bool UseCustomMessageConsuming = false;

        ILogger log;
        public ILogger Log
        {
            get
            {
                if (log == null)
                    log = new NullLogger();
                return log;
            }
            set
            {
                if (value == null)
                    return;
                else
                    log = value;
            }
        }

        NetConnectionStatus _status;
        public NetConnectionStatus Status { get { return _status; } }
        string _statusReason;
        public string StatusReason { get { return _statusReason; } }

        #region actors
        private List<Actor> actors = new List<Actor>();
        internal Actor CreateActor<T>(SlimMath.Vector3 position, SlimMath.Quaternion rotation) where T : ActorDefinition, new()
        {
            var nActor = new Actor(new T(), context);
            nActor.position = position;
            nActor.rotation = rotation;

            var fnull = actors.FindIndex(a => a == null);
            if (fnull != -1)
            {
                nActor.Id = (ushort)fnull;
                actors[fnull] = nActor;
                
            }
            else
            {
                if (actors.Count > ushort.MaxValue)
                {
                    log.Error("Cannot have more than {0} actors at any given time", ushort.MaxValue);
                    return null;
                }
                nActor.Id = (ushort)actors.Count;
                actors.Add(nActor);
            }

            
            return nActor;
        }

        public void DestroyActors(Func<Actor, bool> comparator)
        {
            foreach (var actor in actors)
            {
                if (comparator(actor))
                    DestroyActor(actor);
            }
        }

        public void DestroyActor(Actor actor)
        {
            actors[actor.Id] = null;
            if (actor.Room != null)
            {
                actor.Room.actors.Remove(actor);
            }
        }

        public Actor GetActor(ushort id)
        {
            if (actors.Count < id - 1)
                return null;
            return actors[id];
        }

        internal void AddActor(Actor actor)
        {
            actor.Context = context;
            if (actors[actor.Id] == null)
                actors[actor.Id] = actor;
            else
                log.Error("could not add actor with id {0} as it already exists", actor.Id);
        }

        #endregion

        
        public void Update(object _)
        {
            context.BeforeUpdate();
            
            List<NetIncomingMessage> messages = new List<NetIncomingMessage>();
            int counter = peer.ReadMessages(messages);

            foreach (var msg in messages)
            {
                //faster than switch, as most will be Data messages.

                if (msg.MessageType == NetIncomingMessageType.Data)
                {
                    Consume(msg);
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.WarningMessage)
                {
                    log.Warning(msg.ReadString());
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionLatencyUpdated)
                {
                    var latency = msg.ReadFloat();
                    peer.Recycle(msg);
                    context.LatencyChanged(latency);
                }
                else if (msg.MessageType == NetIncomingMessageType.ErrorMessage)
                {
                    log.Error(msg.ReadString());
                    peer.Recycle(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.ConnectionApproval)
                {
                    ApproveConnection(msg);
                }
                else if (msg.MessageType == NetIncomingMessageType.StatusChanged)
                {
                    _status = (NetConnectionStatus)msg.ReadByte();
                    _statusReason = msg.ReadString();
                    var conn = msg.SenderConnection;
                    peer.Recycle(msg);
                    ConnectionStatusChanged(_status, _statusReason, conn);
                }
                else if (msg.MessageType == NetIncomingMessageType.Error)
                {
                    log.Error(msg.ReadString()); //this should really never happen...
                    peer.Recycle(msg);
                }
                else
                    peer.Recycle(msg);
            }

            context.AfterUpdate();
        }

        #region incoming
        protected void Consume(NetIncomingMessage msg)
        {
            //faster than switch, as this is in most to least common order
            if (msg.SequenceChannel == Channels.ACTOR_STREAM)
            {
                var actorId = msg.ReadUInt16();
                var actor = GetActor(actorId);
            }
            else if (msg.SequenceChannel == Channels.ACTOR_EVENT)
            {
                var actorId = msg.ReadUInt16();
                var eventId = msg.ReadByte();
                var eventToInvoke = Events.GetEvent(eventId);
                eventToInvoke.ReadFromBuffer(msg);
                eventToInvoke.Caller = GetPlayer(msg.SenderConnection);
                var actor = GetActor(actorId);
                if (actor != null)
                    actor.Invoke(eventToInvoke);
            }
            else if (msg.SequenceChannel == Channels.STATIC_EVENT)
            {
                var eventId = msg.ReadByte();

                var eventToInvoke = Events.GetEvent(eventId);
                eventToInvoke.ReadFromBuffer(msg);
                eventToInvoke.Caller = GetPlayer(msg.SenderConnection);

                Invoke(eventToInvoke);
            }
            else if (UseCustomMessageConsuming)
            {
                context.CustomConsumeBuffer(msg);
            }
            else
            {
                Log.Warning("data received over unhandled channel {0}", msg.SequenceChannel);
            }
        }

        protected virtual void ApproveConnection(NetIncomingMessage message)
        {
            message.SenderConnection.Deny();
            peer.Recycle(message);
        }

        protected virtual void ConnectionStatusChanged(NetConnectionStatus status, string reason, NetConnection connection)
        {
            if (status == NetConnectionStatus.Connected)
                Connected(connection);
            else if (status == NetConnectionStatus.Disconnected)
                Disconnected(connection);
            context.StatusChanged(status, reason, connection);
        }

        abstract protected void Connected(NetConnection connection);
        abstract protected void Disconnected(NetConnection connection);

        #endregion
        
        #region outgoing

        /// <summary>
        /// Send the buffer to the specified connection
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="recipient"></param>
        /// <param name="deliveryMethod"></param>
        internal void SendMessage(NetOutgoingMessage netMessage, NetConnection recipient, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.ReliableOrdered)
        {
            if (recipient== null)
            {
                return;
            }
            peer.SendMessage(netMessage, recipient, deliveryMethod);
        }

        /// <summary>
        /// Send the buffer to the specified connections
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="recipients"></param>
        /// <param name="deliveryMethod"></param>
        internal void SendMessage(NetOutgoingMessage netMessage, List<NetConnection> recipients, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0)
        {
            if (recipients.Count == 0)
            {
                return;
            }
            peer.SendMessage(netMessage, recipients, deliveryMethod, channel);
        }

        /// <summary>
        /// send the message to the default players (server is everyone, client is server)
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="deliveryMethod"></param>
        /// <param name="channel"></param>
        internal abstract void SendMessage(NetOutgoingMessage netMessage, NetDeliveryMethod deliveryMethod = NetDeliveryMethod.ReliableOrdered, int channel = 0);

        
        #endregion

        #region event system

        /// <summary>
        /// Send an event to all connected players. Will be sent as a static event
        /// </summary>
        /// <param name="e"></param>
        public void Event(Event e)
        {
            var buffer = peer.CreateMessage();
            buffer.Write(e.Id);
            e.WriteToBuffer(buffer);

            SendMessage(buffer, e.Reliability, Channels.STATIC_EVENT);
        }


        Dictionary<byte, Action<Event>> receivers = new Dictionary<byte, Action<Event>>();
        public void RegisterEventReceiver<T>(Action<Event> receiver) where T : Event, new()
        {
            var registerevent = new T();
            Action<Event> pRec;
            if (!receivers.TryGetValue(registerevent.Id, out pRec))
                receivers.Add(registerevent.Id, receiver);
            else
                pRec += receiver;
        }

        internal void Invoke(Event eventToInvoke)
        {
            Action<Event> prec;
            if (receivers.TryGetValue(eventToInvoke.Id, out prec))
                prec(eventToInvoke);
        }
        #endregion

        public void ShutDown(string message)
        {
            peer.Shutdown(message);
        }

        
    }
}
