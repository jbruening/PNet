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
            //}
            //else if (utilId == RPCUtils.AddView)
            //{
            //    var addToId = msg.ReadUInt16();
            //    var idToAdd = msg.ReadUInt16();
            //    string customFunction;
            //    var runCustomFunction = msg.ReadString(out customFunction);

            //    NetworkView view;
            //    if (NetworkView.Find(addToId, out view))
            //    {
            //        var newView = view.gameObject.AddComponent<NetworkView>();
            //        NetworkView.RegisterView(newView, idToAdd);
            //        newView.viewID = new NetworkViewId() { guid = idToAdd, IsMine = view.IsMine };
            //        newView.IsMine = view.IsMine;
            //        newView.OwnerId = view.OwnerId;

            //        if (runCustomFunction)
            //            view.gameObject.SendMessage(customFunction, newView, SendMessageOptions.RequireReceiver);
            //    }
            //}
        }
    }
}
