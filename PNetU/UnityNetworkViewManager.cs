using System.Collections.Generic;
using PNetC;

namespace PNetU
{
    internal class UnityNetworkViewManager
    {
        readonly Dictionary<NetworkViewId, NetworkView> _networkViews = new Dictionary<NetworkViewId, NetworkView>();

        internal void AddView(PNetC.NetworkView newView, NetworkView view)
        {
            _networkViews.Add(newView.ViewID, view);
            view.SetNetworkView(newView);
        }

        internal bool TryGetView(NetworkViewId viewId, out NetworkView view)
        {
            return _networkViews.TryGetValue(viewId, out view);
        }

        internal bool Remove(NetworkViewId viewId)
        {
            return _networkViews.Remove(viewId);
        }
    }
}