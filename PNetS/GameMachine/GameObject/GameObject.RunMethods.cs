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
                var c = components[i];
                if (c.update != null)
                    try
                    {
                        c.update();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }

        internal void RunCoroutines()
        {
            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                try
                {
                    c.component.RunCoroutines();
                }
                catch (Exception e)
                {
                    Debug.LogError("[RunCoroutine {0}] {1}", Name, e);
                }
            }
        }

        internal void LateUpdate()
        {
            for (var i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.lateUpdate != null)
                    try
                    {
                        c.lateUpdate();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }

        internal void OnPlayerConnected(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.onPlayerConnected != null)
                    try
                    {
                        c.onPlayerConnected(player);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }

        internal void OnPlayerDisconnected(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.onPlayerDisconnected != null)
                    try
                    {
                        c.onPlayerDisconnected(player);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
            if (player == Owner)
                Owner = null;
        }

        internal void OnPlayerLeftRoom(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.onPlayerLeftRoom != null)
                    try
                    {
                        c.onPlayerLeftRoom(player);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }

        internal void OnPlayerEnteredRoom(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.onPlayerEnteredRoom != null)
                    try
                    {
                        c.onPlayerEnteredRoom(player);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }

        internal void OnComponentAdded(Component component)
        {
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.onComponentAdded != null)
                    try
                    {
                        c.onComponentAdded(component);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }

        internal void OnFinishedInstantiate(Player player)
        {
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.onFinishedInstantiate != null)
                    try
                    {
                        c.onFinishedInstantiate(player);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }

        internal void OnDestroy()
        {
            for (int i = 0; i < components.Count; i++)
            {
                var c = components[i];
                if (c.onDestroy != null)
                    try
                    {
                        c.onDestroy();
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e.ToString());
                    }
            }
        }
    }
}
