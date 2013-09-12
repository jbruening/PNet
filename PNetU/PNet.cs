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

namespace PNetU
{
    /// <summary>
    /// Networking class
    /// </summary>
    public static class Net
    {
        /// <summary>
        /// Bind unity things to PNetC.Net
        /// </summary>
        public static void SetupUnity()
        {
            EngineHookFactory.CreateEngineHook = () =>
            {
                return UnityEngineHook.Instance;
            };

            PNetC.Debug.logger = new UnityDebugLogger();
        }

        /// <summary>
        /// resource caching for instantiation
        /// </summary>
        public static bool resourceCaching;
    }
}
