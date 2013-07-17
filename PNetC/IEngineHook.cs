using System;
namespace PNetC
{
    /// <summary>
    /// network hooking into the Update method of unity. Don't put in the scene.
    /// </summary>
    public interface IEngineHook
    {
        event Action EngineUpdate;
        void Instantiate(string path, NetworkView newView, Vector3 location, Quaternion rotation);
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