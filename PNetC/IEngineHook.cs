using System;
namespace PNetC
{
    /// <summary>
    /// network hooking into the Update method of unity. Don't put in the scene.
    /// </summary>
    public interface IEngineHook
    {
        /// <summary>
        /// This should be run every frame by whatever engine you're using PNetC in.
        /// </summary>
        event Action EngineUpdate;
        /// <summary>
        /// Create an object, and return it. Said object should be a container to hold the NetworkView
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newView"></param>
        /// <param name="location"></param>
        /// <param name="rotation"></param>
        /// <returns></returns>
        object Instantiate(string path, NetworkView newView, Vector3 location, Quaternion rotation);

        /// <summary>
        /// Add a NetworkView to the same container as an already existing NetworkView
        /// </summary>
        /// <param name="view"></param>
        /// <param name="newView"></param>
        /// <param name="customFunction"></param>
        /// <returns></returns>
        object AddNetworkView(NetworkView view, NetworkView newView, string customFunction);
    }
}