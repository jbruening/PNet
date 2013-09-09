using System;
namespace PNetC
{
    /// <summary>
    /// network hooking into the Update method of unity. Don't put in the scene.
    /// </summary>
    public interface IEngineHook
    {
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

        object AddNetworkView(NetworkView view, NetworkView newView, string customFunction);
    }

    public static class EngineHookFactory
    {
        public static event Func<IEngineHook> CreateEngineHook;

        internal static IEngineHook DoCreateEngineHook()
        {
            return CreateEngineHook != null ? CreateEngineHook() : null;
        }
    }
}