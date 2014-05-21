using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using PNet;

namespace PNetS
{
    partial class Room
    {
        private readonly List<RoomBehaviour> _roomBehaviours = new List<RoomBehaviour>();
        
        private readonly IntDictionary<GameObject> _gameObjects = new IntDictionary<GameObject>(256);

        internal void Update()
        {
            try
            {
                for (int i = 0; i < _roomBehaviours.Count; ++i)
                {
                    _roomBehaviours[i].Update();
                }
                for (int i = 0; i < _gameObjects.Capacity; ++i)
                {
                    GameObject gobj;
                    if (_gameObjects.TryGetValue(i, out gobj))
                        gobj.Update();
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[Room Update] {0}: {1}", Name, e);
            }
        }
    }
}