using System;
using UnityEngine;

/// <summary>
/// Simple class to override instead of monobehaviour, has some extra network functions
/// </summary>
public class NetBehaviour : MonoBehaviour
{
    PNetU.NetworkView _netView;

    /// <summary>
    /// Get the PNetU.NetworkView attached to the gameObject
    /// </summary>
    public PNetU.NetworkView netView
    {
        get { return _netView ?? (_netView = GetComponent<PNetU.NetworkView>()); }
        internal set
        {
            _netView = value;
        }
    }
    internal void CallFinished() { OnFinishedCreating(); }
    /// <summary>
    /// Called once the network view has finished attaching and instantiating
    /// </summary>
    protected virtual void OnFinishedCreating() { }
}