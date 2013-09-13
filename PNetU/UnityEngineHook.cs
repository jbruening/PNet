using System;
using System.Collections.Generic;
using UnityEngine;
using NetworkView = PNetC.NetworkView;
using Object = UnityEngine.Object;

namespace PNetU
{
    /// <summary>
    /// network hooking into the Update method of unity
    /// </summary>
    internal class UnityEngineHook : MonoBehaviour, PNetC.IEngineHook
    {
        public event Action EngineUpdate;

        static UnityEngineHook _instance;
        public static UnityEngineHook Instance 
        { 
            get 
            {
                if (_instance == null)
                {
                    var gobj = new GameObject("PNetU Singleton Engine Hook");
                    _instance = gobj.AddComponent<UnityEngineHook>();
                    //gobj.hideFlags = HideFlags.DontSave;
                    DontDestroyOnLoad(gobj);
                }
                return _instance; 
            } 
        }
        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
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
                Net.Peer.Disconnect();
                //run some cleanup too, just in case
                EngineUpdate = null;
            }
        }

        internal static Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();
        public object Instantiate(string path, PNetC.NetworkView newView, PNetC.Vector3 location, PNetC.Quaternion rotation)
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
                return null;
            }

            if (Debug.isDebugBuild)
            {
                Debug.Log(string.Format("network instantiate of {0}. Loc: {1} Rot: {2}", path, location, rotation));
            }

            //look for a networkview..

            var view = instance.GetComponent<NetworkView>();
            if (view == null)
                view = instance.AddComponent<NetworkView>();

            view.SetNetworkView(newView);

            var nBehaviours = instance.GetComponents<NetBehaviour>();

            foreach (var behave in nBehaviours)
            {
                behave.netView = view;

                view.OnFinishedCreation += behave.CallFinished;
            }

            view.DoOnFinishedCreation();

            return view;
        }

        public object AddNetworkView(PNetC.NetworkView view, PNetC.NetworkView newView, string customFunction)
        {
            var uview = view.Container as NetworkView;

            if (uview == null)
            {
                Debug.LogError("Could not attach extra networkview because we could not pull the source");
                return null;
            }

            var unewView = uview.gameObject.AddComponent<NetworkView>();
            unewView.SetNetworkView(newView);

            if (Debug.isDebugBuild)
            {
                Debug.Log("Attached extra networkview " + newView.ViewID.guid, uview.gameObject);
            }

            return unewView;
        }
    }
}