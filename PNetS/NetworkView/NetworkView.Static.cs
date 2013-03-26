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

        static readonly IntDictionary<NetworkView> Views = new IntDictionary<NetworkView>();

        /// <summary>
        /// Find the networkview based on a viewID
        /// </summary>
        /// <param name="viewID"></param>
        /// <returns></returns>
        public static NetworkView Find(NetworkViewId viewID)
        {
            NetworkView view;
            Find(viewID.guid, out view);
            return view;
        }
        /// <summary>
        /// Find a network view from an ID
        /// </summary>
        /// <param name="id"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static bool Find(ushort id, out NetworkView view)
        {
            lock (ViewsLock) {view = Views[id];}
            return view != null;
        }

        private readonly static object ViewsLock = new object();
        internal static void RemoveView(NetworkView view)
        {
            lock (ViewsLock)
            {
                var findView = Views[view.viewID.guid];
                if (findView != null)
                {
                    if (findView == view)
                    {
                        Views.Remove(view.viewID.guid);
                    }
                }
            }
        }

        internal static void RegisterView(NetworkView view, ushort viewId)
        {
            lock(ViewsLock) {Views.Add(viewId, view);}
        }

        /// <summary>
        /// register a network view you've just added as a component. Assignes the network id.
        /// </summary>
        /// <param name="view"></param>
        public static void RegisterNewView(ref NetworkView view)
        {
            int addedId;
            lock (ViewsLock)
            {
                addedId = Views.Add(view);
            }
            view.viewID.guid = (ushort) addedId;
        }
    }
}
