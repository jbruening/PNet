using System;
using Lidgren.Network;
using PNet;
using PNetS;

namespace ExampleServer
{
    class PlayerComponent : Component
    {
        private NetworkView netView;

        //The following methods are all run by the game machine during the life of this componet
        
        /// <summary>
        /// Called right after the component is added. 
        /// If AddComponents(params Type[]) is used to add this, it will get called after all the components have been added
        /// </summary>
        void Awake()
        {
            netView = GetComponent<NetworkView>();
            netView.SetSerializationMethod(Serialize);
            netView.OnDeserializeStream += OnDeserializeStream;
        }

        /// <summary>
        /// Called during the next Game loop after awake, before Update
        /// </summary>
        void Start()
        {
            
        }
        /// <summary>
        /// Once per game loop
        /// </summary>
        void Update()
        {
            
        }
        /// <summary>
        /// Once per game loop, after all other updates have been run
        /// </summary>
        void LateUpdate()
        {
            
        }
        /// <summary>
        /// whenever a component is added to the same gameobject this is on
        /// </summary>
        /// <param name="component"></param>
        void OnComponentAdded(Component component)
        {
            
        }
        /// <summary>
        /// When this object is being destroyed
        /// </summary>
        void OnDestroy()
        {
            
        }


        /// <summary>
        /// Ran after the specified player has finished instantiating the gameobject this is attached to. 
        /// Only gets run if this component was added before the player finishes instantiating.
        /// If this was added in the same game loop that the gameobject was instantiated, it will always get run.
        /// </summary>
        /// <param name="player"></param>
        private void OnInstantiationFinished(Player player)
        {
            if (netView.owner == player)
            {
                //The StringSerializer object implements the interface INetSerializable, 
                //which you can of course use to make your own serialization objects to serialize pretty much anything you want
                //though limited by the practicality of sending large amounts of data (you shouldn't be sending images, as udp is bad idea for that)
                //Rpcs are reliable
                netView.RPC(1, player, new StringSerializer("Congratulations on spawning your first object"));
            }
            else
            {
                netView.RPC(1, player, new StringSerializer("Another player's object spawned!"));
                //A non-owner finished spawning the object. We might send them similar data, like what the player looks like, 
                //but don't send them player specific data like health
            }

            //This will start state synchronization
            netView.StateSynchronization = NetworkStateSynchronization.Unreliable;
        }

        private void Serialize(NetOutgoingMessage msg)
        {
            //serialize data into the stream
            //this is only run if the netView.StateSynchronization is not set to Off
            //TODO: implement smoothing/lag compensation
            Vector3Serializer.Instance.Value = gameObject.Position;
            Vector3Serializer.Instance.OnSerialize(msg);
        }

        private void OnDeserializeStream(NetIncomingMessage netIncomingMessage, Player player)
        {
            //deserialize data from the stream
            //this is run if the client serializes data.
            //TODO: optionally, ignore data if the StateSynchronization is Off
            if (player != netView.owner)
            {
                //Uh oh! someone is serializing data into something they don't own. 
                //Either you coded it so that non-owners can serialize, or someone is trying to cheat.
                //ignore the value
            }
            else
            {
                //TODO: implement smoothing/lag compensation
                Vector3Serializer.Instance.OnDeserialize(netIncomingMessage);
                gameObject.Position = Vector3Serializer.Instance.Value;
            }
        }

        [Rpc(7, false)]
        void ReceiveMessage(NetIncomingMessage msg, NetMessageInfo info)
        {
            //if you want to prevent the message from the player from continuing to other players (if it was sent with rpcmode.all or others or owner,
            //then you set info.continueForwarding to false
            //alternatively, the second parameter of the rpc attribute can set what the default value of info.continueForwarding is (in this case, false), 
            //and then change it to true if you figure out you do need to let the message continue on.

            Debug.Log("Player {0} sent a message on this component: {1}", info.player, msg.ReadString());
        }
    }
}