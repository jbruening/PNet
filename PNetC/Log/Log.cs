using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetC
{
    /// <summary>
    /// Debug
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Reference to the actual log receiver
        /// </summary>
        public static ILogger Logger = new NullLogger();

        /// <summary>
        /// Only done when you want full logging
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void Log(string value, params object[] args)
        {
            Logger.Full(value, args);
        }

        /// <summary>
        /// Info message. Semi important.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogInfo(string value, params  object[] args)
        {
            Logger.Info(value, args);
        }
        /// <summary>
        /// Error message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogError(string value, params object[] args)
        {
            Logger.Error(value, args);
        }
        /// <summary>
        /// Warning message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogWarning(string value, params object[] args)
        {
            Logger.Warning(value, args);
        }
    }
}
