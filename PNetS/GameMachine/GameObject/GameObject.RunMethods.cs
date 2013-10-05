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
            foreach (var c in components)
            {
                if (c.update != null)
                    try { c.update(); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
        }

        internal void LateUpdate()
        {
            foreach (var c in components)
            {
                if (c.lateUpdate != null)
                    try { c.lateUpdate(); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
        }

        internal void OnPlayerConnected(Player player)
        {
            foreach (var c in components)
            {
                if (c.onPlayerConnected != null)
                    try { c.onPlayerConnected(player); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
        }

        internal void OnPlayerDisconnected(Player player)
        {
            foreach (var c in components)
            {
                if (c.onPlayerDisconnected != null)
                    try { c.onPlayerDisconnected(player); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
            if (player == Owner)
                Owner = null;
        }

        internal void OnPlayerLeftRoom(Player player)
        {
            foreach(var c in components)
            {
                if (c.onPlayerLeftRoom != null)
                    try { c.onPlayerLeftRoom(player); }
                    catch(Exception e){Debug.LogError(e.ToString());}
            }
        }

        internal void OnPlayerEnteredRoom(Player player)
        {
            foreach (var c in components)
            {
                if (c.onPlayerEnteredRoom != null)
                    try { c.onPlayerEnteredRoom(player); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
        }

        internal void OnComponentAdded(Component component)
        {
            foreach (var c in components)
            {
                if (c.onComponentAdded != null)
                    try { c.onComponentAdded(component); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
        }

        internal void OnFinishedInstantiate(Player player)
        {
            foreach (var c in components)
            {
                if (c.onFinishedInstantiate != null)
                    try { c.onFinishedInstantiate(player); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
        }

        internal void OnDestroy()
        {
            foreach (var c in components)
            {
                if (c.onDestroy != null)
                    try { c.onDestroy(); }
                    catch (Exception e) { Debug.LogError(e.ToString()); }
            }
        }
    }
}
