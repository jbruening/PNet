using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PNetC;
using UnityEngine;
using Lidgren.Network;
using PNet;
using System.ComponentModel;
using Debug = UnityEngine.Debug;
using NetworkLogLevel = UnityEngine.NetworkLogLevel;
using Object = UnityEngine.Object;
using Quaternion = UnityEngine.Quaternion;
using Vector3 = UnityEngine.Vector3;

namespace PNetU
{
    /// <summary>
    /// Networking class
    /// </summary>
    public sealed class Net : PNetC.Net
    {
        /// <summary>
        /// logging level. UNUSED
        /// </summary>
        public static NetworkLogLevel logLevel;
        internal static Dictionary<string, GameObject> ResourceCache = new Dictionary<string, GameObject>();
        /// <summary>
        /// resource caching for instantiation
        /// </summary>
        public static bool resourceCaching;

        static Net()
        {
            EngineHookFactory.CreateEngineHook += () =>
                {
                    //do we have an engine hook?
                    if (SingletonEngineHook == null)
                    {
                        var gobj = new GameObject("PNetU Singleton Engine Hook");
                        SingletonEngineHook = gobj.AddComponent<UnityEngineHook>();
                        Object.DontDestroyOnLoad(gobj);
                        //gobj.hideFlags = HideFlags.DontSave;
                    }
                    return SingletonEngineHook;
                };

            PNetC.Debug.logger = new UnityDebugLogger();
        }

        private static void ProcessUtils(NetIncomingMessage msg)
        {
            var utilId = msg.ReadByte();

            if (utilId == RPCUtils.TimeUpdate)
            {

            }
            else if (utilId == RPCUtils.Instantiate)
            {
                //read the path...
                var resourcePath = msg.ReadString();
                var viewId = msg.ReadUInt16();
                var ownerId = msg.ReadUInt16();

                GameObject gobj;
                bool isCached = false;
                if (resourceCaching && (isCached = ResourceCache.ContainsKey(resourcePath)))
                    gobj = ResourceCache[resourcePath];
                else
                    gobj = Resources.Load(resourcePath) as GameObject;

                if (resourceCaching && !isCached)
                    ResourceCache.Add(resourcePath, gobj);

                var instance = (GameObject)GameObject.Instantiate(gobj);

                if (instance == null)
                {
                    Debug.LogWarning("could not find prefab " + resourcePath + " to instantiate");
                    return;
                }

                var trans = instance.transform;

                trans.position = Vector3Serializer.Deserialize(msg);
                trans.rotation = QuaternionSerializer.Deserialize(msg);
                if (Debug.isDebugBuild)
                {
                    Debug.Log(string.Format("network instantiate of {0}. Loc: {1} Rot: {2}", resourcePath, trans.position, trans.rotation));
                }

                //look for a networkview..

                var view = instance.GetComponent<NetworkView>();

                if (view)
                {
                    NetworkView.RegisterView(view, viewId);
                    view.viewID = new NetworkViewId() { guid = viewId, IsMine = PlayerId == ownerId};
                    view.IsMine = PlayerId == ownerId;
                    view.OwnerId = ownerId;

                    var nBehaviours = instance.GetComponents<NetBehaviour>();

                    foreach (var behave in nBehaviours)
                    {
                        behave.netView = view;

                        view.OnFinishedCreation += behave.CallFinished;
                    }

                    view.DoOnFinishedCreation();

                    FinishedInstantiate(view.viewID.guid);
                }
            }
            else if (utilId == RPCUtils.AddView)
            {
                var addToId = msg.ReadUInt16();
                var idToAdd = msg.ReadUInt16();
                string customFunction;
                var runCustomFunction = msg.ReadString(out customFunction);

                NetworkView view;
                if (NetworkView.Find(addToId, out view))
                {
                    var newView = view.gameObject.AddComponent<NetworkView>();
                    NetworkView.RegisterView(newView, idToAdd);
                    newView.viewID = new NetworkViewId() { guid = idToAdd, IsMine = view.IsMine };
                    newView.IsMine = view.IsMine;
                    newView.OwnerId = view.OwnerId;

                    if (runCustomFunction)
                        view.gameObject.SendMessage(customFunction, newView, SendMessageOptions.RequireReceiver);
                }
            }
        }
    }
}
