using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PNet;
using Lidgren.Network;

namespace PNetC
{
    public partial class NetworkView
    {
        /// <summary>
        /// find a network view based on the given NetworkViewId
        /// </summary>
        /// <param name="viewID"></param>
        /// <returns></returns>
        public static NetworkView Find(NetworkViewId viewID)
        {
            return allViews[viewID.guid];
        }

        /// <summary>
        /// find a networkview based on a networkviewid that was serialized into an rpc
        /// </summary>
        /// <param name="message">uses deserialize, so the read location does advance</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool Find(ref NetIncomingMessage message, out NetworkView view)
        {
            var id = NetworkViewId.Deserialize(message);

            return Find(id, out view);
        }

        public static bool Find(ushort id, out NetworkView view)
        {
            view = allViews[id];
            if (view != null)
                return true;
            return false;
        }

        internal static void RemoveView(ushort viewId)
        {
            allViews.Remove(viewId);
        }

        static IntDictionary<NetworkView> allViews = new IntDictionary<NetworkView>();

        internal static void DestroyAllViews()
        {
            var cap = allViews.Capacity;
            for (int i = 0; i < cap; i++)
            {
                NetworkView view;
                if (allViews.TryGetValue(i, out view))
                {
                    if (view != null)
                        view.DoOnRemove();
                }

                allViews.Remove(i);
            }
        }

        internal static void RegisterView(NetworkView view, ushort viewId)
        {
            allViews.Add(viewId, view);
        }
    }
}
