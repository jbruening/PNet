using System.Linq;
using PNetS;
using SlimMath;

namespace ExampleServer
{
    class BasicRoom : RoomBehaviour
    {
        public override void OnPlayerEnter(Player player)
        {
            var playerObject = Room.NetworkInstantiate("player", Vector3.Zero, Quaternion.Identity, player);
            playerObject.AddComponent<PlayerComponent>();
            
        }
        public override void OnPlayerExit(Player player)
        {
            foreach (var actor in Room.actors.Where(a => a.owner == player))
            {
                Room.NetworkDestroy(actor);
            }
        }
    }
}