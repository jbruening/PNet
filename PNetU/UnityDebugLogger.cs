using PNetC;
using UnityEngine;
using NetworkLogLevel = UnityEngine.NetworkLogLevel;

namespace PNetU
{
    /// <summary>
    /// Logs to standard UnityEngine.Debug class
    /// </summary>
    internal sealed class UnityDebugLogger : ILogger
    {
        /// <summary>
        /// Same as PNetC.Debug.Log.
        /// Only shows up if you have Network.logLevel set to full
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <param name="args"></param>
        public static void Full(string info, UnityEngine.Object context, params object[] args)
        {
            if (Network.logLevel == NetworkLogLevel.Full)
                UnityEngine.Debug.Log(string.Format(info, args), context);
        }
        /// <summary>
        /// Same as PNetC.Debug.Info
        /// Shows up if Network.logLevel is set to Info, or running in a debug build
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        /// <param name="args"></param>
        public static void Info(string info, UnityEngine.Object context, params object[] args)
        {
            if (Network.logLevel > NetworkLogLevel.Off || UnityEngine.Debug.isDebugBuild)
                UnityEngine.Debug.Log(string.Format(info, args), context);
        }

        void ILogger.Full(PNetC.Net sender, string info, params object[] args)
        {
            if (Network.logLevel == NetworkLogLevel.Full)
                UnityEngine.Debug.Log(string.Format(info, args));
        }

        void ILogger.Info(PNetC.Net sender, string info, params object[] args)
        {
            if (Network.logLevel > NetworkLogLevel.Off || UnityEngine.Debug.isDebugBuild)
                UnityEngine.Debug.Log(string.Format(info, args));
        }

        void ILogger.Warning(PNetC.Net sender, string info, params object[] args)
        {
            UnityEngine.Debug.LogWarning(string.Format(info, args));
        }

        void ILogger.Error(PNetC.Net sender, string info, params object[] args)
        {
            UnityEngine.Debug.LogError(string.Format(info, args));
        }
    }
}
