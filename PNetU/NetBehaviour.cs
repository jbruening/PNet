using System;
using UnityEngine;

/// <summary>
/// Simple class to override instead of monobehaviour, has some extra network functions
/// </summary>
public class NetBehaviour : MonoBehaviour
{
    PNetU.NetworkView m_netView;

    /// <summary>
    /// Get the PNetU.NetworkView attached to the gameObject
    /// </summary>
    public PNetU.NetworkView netView
    {
        get
        {
            if (m_netView == null)
                m_netView = GetComponent<PNetU.NetworkView>();
            return m_netView;
        }
        internal set
        {
            m_netView = value;
        }
    }
    internal void CallFinished() { OnFinishedCreating(); }
    /// <summary>
    /// Called once the network view has finished attaching and instantiating
    /// </summary>
    protected virtual void OnFinishedCreating() { }
}