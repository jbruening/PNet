using PNetS;
using SlimMath;

namespace ExampleServer
{
    class BasicRoom : Room
    {
        public BasicRoom()
        {
            //This is the actual room name that gets passed to the clients on scene change
            name = "basic room";
        }

        public override void OnPlayerEnter(Player player)
        {
            var playerObject = NetworkInstantiate("player", Vector3.Zero, Quaternion.Identity, player);
            playerObject.AddComponent<PlayerComponent>();
            
        }
    }
}