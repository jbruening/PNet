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
        void ILogger.Full(string info, params object[] args)
        {
            if (Network.logLevel == NetworkLogLevel.Full || UnityEngine.Debug.isDebugBuild)
                UnityEngine.Debug.Log(string.Format(info, args));

        }

        void ILogger.Info(string info, params object[] args)
        {
            if (Network.logLevel > NetworkLogLevel.Off || UnityEngine.Debug.isDebugBuild)
                UnityEngine.Debug.Log(string.Format(info, args));
        }

        void ILogger.Warning(string info, params object[] args)
        {
            UnityEngine.Debug.LogWarning(string.Format(info, args));
        }

        void ILogger.Error(string info, params object[] args)
        {
            UnityEngine.Debug.LogError(string.Format(info, args));
        }
    }
}
