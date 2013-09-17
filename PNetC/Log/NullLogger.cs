using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetC
{
    /// <summary>
    /// Logger, but logs to nowhere
    /// </summary>
    public sealed class NullLogger : ILogger
    {
        /// <summary>
        /// message only done during full debugging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Full(Net sender, string info, params object[] args)
        {
            
        }

        /// <summary>
        /// informational message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Info(Net sender, string info, params object[] args)
        {
            
        }

        /// <summary>
        /// warning message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Warning(Net sender, string info, params object[] args)
        {
            
        }

        /// <summary>
        /// error message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        public void Error(Net sender, string info, params object[] args)
        {
            
        }
    }
}
