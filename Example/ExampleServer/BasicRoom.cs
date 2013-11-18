using System.Linq;
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
            player.UserData = player;
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
    }
}