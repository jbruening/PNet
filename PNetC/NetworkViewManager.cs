using System.Collections.Generic;
using Lidgren.Network;
using PNet;

namespace PNetC
{
    /// <summary>
    /// A container, object pool, and general manager for the network views associated with all Networked objects
    /// </summary>
    public sealed class NetworkViewManager
    {
        private const int ViewPoolSize = 150;
        readonly IntDictionary<NetworkView> _allViews = new IntDictionary<NetworkView>();
        private readonly Stack<NetworkView> _netViewPool = new Stack<NetworkView>(150);
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
        /// <param name="viewId"></param>
        /// <returns></returns>
        public NetworkView Find(NetworkViewId viewId)
        {
            return _allViews[viewId.guid];
        }

        /// <summary>
        /// find a networkview based on a networkviewid that was serialized into an rpc
        /// </summary>
        /// <param name="message">uses deserialize, so the read location does advance</param>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool Find(ref NetIncomingMessage message, out NetworkView view)
        {
            var id = new NetworkViewId();
            id.OnDeserialize(message);

            return Find(id, out view);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public bool Find(NetworkViewId id, out NetworkView view)
        {
            return _allViews.TryGetValue(id.guid, out view);
        }

        internal void RemoveView(NetworkView view)
        {
            if (view.Manager == this)
            {
                _allViews.Remove(view.ViewID.guid);
                RecycleNetworkView(view);
            }
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
                        view.DoOnRemove(0);
                }
            }
        }

        private void RegisterView(NetworkView view, NetworkViewId viewId)
        {
            _allViews.Add(viewId.guid, view);
        }

        internal NetworkView Create(NetworkViewId viewId, ushort ownerId)
        {
            viewId.IsMine = Net.PlayerId == ownerId;

            var newView = GetNetworkView();
            newView.ViewID = viewId;
            newView.OwnerId = ownerId;
            RegisterView(newView, viewId);
            return newView;
        }

        //pool get for networkviews
        NetworkView GetNetworkView()
        {
            if (_netViewPool.Count == 0)
                return new NetworkView(this);
            return _netViewPool.Pop();
        }
        //pool recycle for networkviews
        void RecycleNetworkView(NetworkView view)
        {
            if (!view.EventsAreCleared())
                Debug.LogError(Net, "Networkview {0} didn't appear to cleanup properly", view.ViewID.guid);

            if (_netViewPool.Count < ViewPoolSize)
                _netViewPool.Push(view);
        }
    }
}