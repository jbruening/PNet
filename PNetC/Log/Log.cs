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
        /// <param name="sender"></param>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void Log(Net sender, string value, params object[] args)
        {
            Logger.Full(sender, value, args);
        }

        /// <summary>
        /// Info message. Semi important.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogInfo(Net sender, string value, params  object[] args)
        {
            Logger.Info(sender, value, args);
        }

        /// <summary>
        /// Error message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogError(Net sender, string value, params object[] args)
        {
            Logger.Error(sender, value, args);
        }

        /// <summary>
        /// Warning message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogWarning(Net sender, string value, params object[] args)
        {
            Logger.Warning(sender, value, args);
        }
    }
}
