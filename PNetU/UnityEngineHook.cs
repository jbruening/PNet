using System;
using PNetC;
using UnityEngine;
using NetworkView = PNetC.NetworkView;
using Object = UnityEngine.Object;
using Quaternion = PNetC.Quaternion;
using Vector3 = PNetC.Vector3;

/// <summary>
/// network hooking into the Update method of unity. Don't put in the scene.
/// </summary>
internal class UnityEngineHook : MonoBehaviour, PNetC.IEngineHook
{
    /// <summary>
    /// Run every frame, as long as the script is enabled
    /// </summary>
    void Update()
    {
        if (EngineUpdate != null)
            EngineUpdate();
    }

    void OnDestroy()
    {
        PNetC.Net.Disconnect();
    }

    public event Action EngineUpdate;
    public void Instantiate(string path, NetworkView newView, Vector3 location, Quaternion rotation)
    {
        
    }
}