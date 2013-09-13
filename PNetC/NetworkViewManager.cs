using Lidgren.Network;
using PNet;

namespace PNetC
{
    /// <summary>
    /// A container, object pool, and general manager for the network views associated with a Net object
    /// </summary>
    public sealed class NetworkViewManager
    {
        readonly IntDictionary<NetworkView> _allViews = new IntDictionary<NetworkView>();
        internal readonly Net Net;

        /// <summary>
        /// Container for all the NetworkViews associated with the PNetC.Net object
        /// </summary>
        internal NetworkViewManager(Net net)
        {
            Net = net;
        }

        /// <summary>
        /// find a network view based on the given NetworkViewId
        /// </summary>
        /// <param name="viewID"></param>
        /// <returns></returns>
        public NetworkView Find(NetworkViewId viewID)
        {
            return _allViews[viewID.guid];
        }

        /// <summary>
        /// find a networkview based on a networkviewid that was serialized into an rpc
        /// </summary>
        /// <param name="message">uses deserialize, so the read location does advance</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool Find(ref NetIncomingMessage message, out NetworkView view)
        {
            var id = NetworkViewId.Deserialize(message);

            return Find(id, out view);
        }

        internal bool Find(ushort id, out NetworkView view)
        {
            view = _allViews[id];
            if (view != null)
                return true;
            return false;
        }

        internal void RemoveView(ushort viewId)
        {
            _allViews.Remove(viewId);
        }

        internal void DestroyAllViews()
        {
            var cap = _allViews.Capacity;
            for (int i = 0; i < cap; i++)
            {
                NetworkView view;
                if (_allViews.TryGetValue(i, out view))
                {
                    if (view != null)
                        view.DoOnRemove();
                }

                _allViews.Remove(i);
            }
        }

        private void RegisterView(NetworkView view, ushort viewId)
        {
            _allViews.Add(viewId, view);
        }

        internal NetworkView Create(ushort viewId, ushort ownerId)
        {
            var newView = new NetworkView(this);
            newView.ViewID = new NetworkViewId(){guid = viewId, IsMine = Net.PlayerId == ownerId};
            newView.OwnerId = ownerId;
            RegisterView(newView, viewId);
            return new NetworkView(this);
        }
    }
}