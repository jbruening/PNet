using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// ReSharper disable CheckNamespace
namespace PNetC
// ReSharper restore CheckNamespace
{
    /// <summary>
    /// Interface for logging information
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// message only done during full debugging
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        void Full(Net sender, string info, params object[] args);

        /// <summary>
        /// informational message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        void Info(Net sender, string info, params object[] args);

        /// <summary>
        /// warning message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        void Warning(Net sender, string info, params object[] args);

        /// <summary>
        /// error message
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="info"></param>
        /// <param name="args"></param>
        void Error(Net sender, string info, params object[] args);
    }
}
