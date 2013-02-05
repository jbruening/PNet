using System;
using UnityEngine;

/// <summary>
/// network hooking into the Update method of unity. Don't put in the scene.
/// </summary>
internal class EngineUpdateHook : MonoBehaviour
{
    internal Action UpdateSubscription = delegate { };

    /// <summary>
    /// Run every frame, as long as the script is enabled
    /// </summary>
    void Update()
    {
        UpdateSubscription();
    }

    void OnDestroy()
    {
        PNetU.Net.Disconnect();
    }
}