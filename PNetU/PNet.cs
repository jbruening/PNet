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
                    return UnityEngineHook.Instance;
                };

            PNetC.Debug.logger = new UnityDebugLogger();
        }
    }
}
