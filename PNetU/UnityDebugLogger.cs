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
        void ILogger.Full(PNetC.Net sender, string info, params object[] args)
        {
            if (Network.logLevel == NetworkLogLevel.Full || UnityEngine.Debug.isDebugBuild)
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
