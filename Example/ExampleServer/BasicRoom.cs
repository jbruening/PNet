using System.Linq;
using Lidgren.Network;
using PNet;
using PNetS;
using SlimMath;

namespace ExampleServer
{
    class BasicRoom : RoomBehaviour
    {
        public override void OnPlayerEnter(Player player)
        {
            //make a new object for the player who just entered the room
            var playerObject = Room.NetworkInstantiate("player", Vector3.Zero, Quaternion.Identity, player);
            playerObject.AddComponent<PlayerComponent>();

            //so we can reference the player's object easier in things like rpcs
            player.UserData = playerObject;
        }
        public override void OnPlayerExit(Player player)
        {
            //cleanup
            foreach (var actor in Room.actors.Where(a => a.owner == player))
            {
                Room.NetworkDestroy(actor);
            }
            player.UserData = null;
        }

        [Rpc(3, false)]
        void PlayerStaticMessage3(NetIncomingMessage msg, NetMessageInfo info)
        {
            Debug.Log("Got a room-wide/static message {0} from player {1}", msg.ReadString(), info.player);
        }
    }
}