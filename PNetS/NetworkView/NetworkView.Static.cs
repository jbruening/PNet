using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using PNet;
using System.Collections;
using PNetS;
using System.Threading;

namespace PNetS
{
    /// <summary>
    /// Class used for networking
    /// </summary>
    public partial class NetworkView
    {

        static IntDictionary<NetworkView> views = new IntDictionary<NetworkView>();

        /// <summary>
        /// Find the networkview based on a viewID
        /// </summary>
        /// <param name="viewID"></param>
        /// <returns></returns>
        public static NetworkView Find(NetworkViewId viewID)
        {
            return views[viewID.guid];
        }
        /// <summary>
        /// Find a network view from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool Find(ushort id, out NetworkView view)
        {
            view = views[id];
            if (view != null)
                return true;
            return false;
        }

        internal static void RemoveView(ushort viewId)
        {
            views.Remove(viewId);
        }

        internal static void RegisterView(NetworkView view, ushort viewId)
        {
            views.Add(viewId, view);
        }

        /// <summary>
        /// register a network view you've just added as a component. Assignes the network id.
        /// </summary>
        /// <param name="view"></param>
        public static void RegisterNewView(ref NetworkView view)
        {
            view.viewID.guid = (ushort)views.Add(view);
        }
    }
}
