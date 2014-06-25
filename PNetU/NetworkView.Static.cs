using Lidgren.Network;
using PNetC;

namespace PNetU
{
    public partial class NetworkView
    {
        /// <summary>
        /// find a network view based on the given NetworkViewId
        /// </summary>
        /// <param name="viewId"></param>
        /// <returns></returns>
        public static NetworkView Find(NetworkViewId viewId)
        {
            if (UnityEngineHook.ValidInstance)
                return null;
            
            NetworkView view;
            UnityEngineHook.Instance.Manager.TryGetView(viewId, out view);
            return view;
        }

        /// <summary>
        /// find a networkview based on a networkviewid that was serialized into an rpc
        /// </summary>
        /// <param name="message">uses deserialize, so the read location does advance</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool Find(ref NetIncomingMessage message, out NetworkView view)
        {
            var viewId = new NetworkViewId();
            viewId.OnDeserialize(message);
            if (UnityEngineHook.ValidInstance)
                return UnityEngineHook.Instance.Manager.TryGetView(viewId, out view);
            
            view = null;
            return false;
        }
    }
}
