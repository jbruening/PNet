using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// behaviour for rooms
    /// </summary>
    public abstract class RoomBehaviour
    {
        /// <summary>
        /// The room this is attached to
        /// </summary>
        public Room Room { get; internal set; }

        /// <summary>
        /// run when a player enters the room
        /// </summary>
        public virtual void OnPlayerEnter(Player player){} 

        /// <summary>
        /// run when a player exits the room
        /// </summary>
        public virtual void OnPlayerExit(Player player){}

        /// <summary>
        /// run when a room behaviour is added to the room we're attached to
        /// </summary>
        /// <param name="behaviour"></param>
        public virtual void OnBehaviourAdded(RoomBehaviour behaviour){}

        /// <summary>
        /// Loop after the behaviour is instantiated
        /// </summary>
        public virtual void Start(){}

        /// <summary>
        /// run once every update loop, after gameobjects update
        /// </summary>
        public virtual void Update(){}

        /// <summary>
        /// run when the room is shutting down
        /// </summary>
        public virtual void Closing(){}

        /// <summary>
        /// run when the behaviour is being removed from the room
        /// </summary>
        public void Disposing(){}

        /// <summary>
        /// run when a gameobject is added to the room
        /// </summary>
        /// <param name="gameObject"></param>
        public virtual void OnGameObjectAdded(GameObject gameObject){}
    }
}
