using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace PNetS
{
    /// <summary>
    /// Basic entity that exists in the gamemachine
    /// </summary>
    public sealed partial class GameObject
    {
        internal void Update()
        {
            for (var i = 0; i < components.Count; i++)
            {
                components[i].InternalUpdateCall();
            }
        }

        internal void LateUpdate()
        {
            for (var i = 0; i < components.Count; i++)
            {
                components[i].InternalLateUpdateCall();
            }
        }

        internal void OnPlayerConnected(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].InternalOnPlayerConnectedCall(player);
            }
        }

        internal void OnPlayerDisconnected(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].InternalOnPlayerDisconnectedCall(player);
            }
            if (player == Owner)
                Owner = null;
        }

        internal void OnPlayerLeftRoom(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].InternalOnPlayerLeftRoomCall(player);
            }
        }

        internal void OnPlayerEnteredRoom(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].InternalOnPlayerEnteredRoomCall(player);
            }
        }

        internal void OnComponentAdded(Component component)
        {
            for (int i = 0; i < components.Count; i++)
            {
                if (!object.ReferenceEquals(components[i], component))
                    components[i].InternalOnComponentAddedCall(component);
            }
        }

        internal void OnFinishedInstantiate(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].InternalOnFinishedInstantiateCall(player);
            }
        }

        internal void OnDestroy()
        {
            for (int i = 0; i < components.Count; i++)
            {
                components[i].InternalOnDestroyCall();
            }
        }
    }
}
