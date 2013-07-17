using PNetC;

namespace PNetU
{
    /// <summary>
    /// Logs to standard UnityEngine.Debug class
    /// </summary>
    public sealed class UnityDebugLogger : ILogger
    {
        /// <summary>
        /// informational message
        /// </summary>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Info(string info, params object[] args)
        {
            UnityEngine.Debug.Log(string.Format(info, args));
        }

        /// <summary>
        /// warning message
        /// </summary>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Warning(string info, params object[] args)
        {
            UnityEngine.Debug.LogWarning(string.Format(info, args));
        }

        /// <summary>
        /// error message
        /// </summary>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Error(string info, params object[] args)
        {
            UnityEngine.Debug.LogError(string.Format(info, args));
        }
    }
}
