using System;
using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace PNetU
{
    /// <summary>
    /// network hooking into the Update method of unity
    /// </summary>
    internal class UnityEngineHook : MonoBehaviour, PNetC.IEngineHook
    {
        readonly UnityNetworkViewManager _manager = new UnityNetworkViewManager();
        internal UnityNetworkViewManager Manager { get { return _manager; } }

        public event Action EngineUpdate;

        static UnityEngineHook _instance;
        //because we're dontdestroyonload, if we're ever destroyed
        //we shouldn't attempt to recreate an instance if requested
        //because someone might be erronously calling us.
        static bool _destroyed = false;
        public static UnityEngineHook Instance 
        { 
            get 
            {
                if (_instance == null && !_destroyed)
                {
                    var gobj = new GameObject("PNetU Singleton Engine Hook");
                    _instance = gobj.AddComponent<UnityEngineHook>();
                    //gobj.hideFlags = HideFlags.DontSave;
                    DontDestroyOnLoad(gobj);
                }
                return _instance;
            } 
        }
        public static bool ValidInstance
        {
            get
            {
                if (_instance != null || (_instance == null && !_destroyed))
                    return true;
                return false;
            }
        }

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                //we're probably creating a new object after having explicitly destroyed, so reset.
                _destroyed = false;
                DontDestroyOnLoad(this);
            }

            if (_instance != this)
                Destroy(this);
        }

        /// <summary>
        /// Run every frame, as long as the script is enabled
        /// </summary>
        void Update()
        {
            if (EngineUpdate != null)
                EngineUpdate();
        }

        void OnDestroy()
        {
            if (_instance == this)
            {
                _destroyed = true;
                Net.Peer.Disconnect();
                //run some cleanup too, just in case
                EngineUpdate = null;
                Net.CleanupEvents();
                Manager.Clear();
            }
        }

        internal static Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();
        public void Instantiate(string path, PNetC.NetworkView newView, PNetC.Vector3 location, PNetC.Quaternion rotation)
        {
            GameObject gobj;
            bool isCached = false;
            if (Net.resourceCaching && (isCached = ResourceCache.ContainsKey(path)))
                gobj = ResourceCache[path];
            else
                gobj = Resources.Load(path) as GameObject;

            if (Net.resourceCaching && !isCached)
                ResourceCache.Add(path, gobj);

            var instance = (GameObject)Instantiate(gobj, new Vector3(location.X, location.Y, location.Z), new Quaternion(rotation.X, rotation.Y, rotation.Z, rotation.W));

            if (instance == null)
            {
                Debug.LogWarning("could not find prefab " + path + " to instantiate");
                instance = new GameObject("BROKEN NETWORK PREFAB " + newView.ViewID);
            }

            UnityDebugLogger.Full("network instantiate of {0}. Loc: {1} Rot: {2}", null, path, location, rotation);

            //look for a networkview..
            var view = instance.GetComponent<NetworkView>();
            if (view == null)
                view = instance.AddComponent<NetworkView>();

            _manager.AddView(newView, view);

            var nBehaviours = instance.GetComponents<NetBehaviour>();

            foreach (var behave in nBehaviours)
            {
                behave.netView = view;

                view.OnFinishedCreation += behave.CallFinished;
            }

            view.DoOnFinishedCreation();
        }

        public void AddNetworkView(PNetC.NetworkView view, PNetC.NetworkView newView, string customFunction)
        {
            NetworkView uview;
            if (!_manager.TryGetView(view.ViewID, out uview))
            {
                Debug.LogError(
                    string.Format("Could not attach extra networkview {0} to networkview {1}. It might have been destroyed accidentally, or not yet instantiated", 
                    newView.ViewID, 
                    view.ViewID));
            }

            var unewView = uview.gameObject.AddComponent<NetworkView>();
            _manager.AddView(newView, unewView);

            UnityDebugLogger.Info("Attached extra networkview " + newView.ViewID.guid, uview.gameObject);

            if (!string.IsNullOrEmpty(customFunction))
                uview.gameObject.SendMessage(customFunction, unewView, SendMessageOptions.DontRequireReceiver);
        }
    }
}