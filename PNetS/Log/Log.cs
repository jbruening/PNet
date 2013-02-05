using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// Debug
    /// </summary>
    public static class Debug
    {
        /// <summary>
        /// Reference to the actual log receiver
        /// </summary>
        public static ILogger logger = new NullLogger();

        /// <summary>
        /// Info message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void Log(string value, params object[] args)
        {
            logger.Info(value, args);
        }
        /// <summary>
        /// Error message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogError(string value, params object[] args)
        {
            logger.Error(value, args);
        }
        /// <summary>
        /// Warning message
        /// </summary>
        /// <param name="value"></param>
        /// <param name="args"></param>
        public static void LogWarning(string value, params object[] args)
        {
            logger.Warning(value, args);
        }
    }
}
