using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// Game time. Duplication class of unity's Time class
    /// </summary>
    public static class Time
    {
        /// <summary>
        /// Time since the game started
        /// </summary>
        public static double time { get { return GameState.TimeSinceStartup; } }
        /// <summary>
        /// Time since last 'frame'
        /// </summary>
        public static float deltaTime { get { return (float)(GameState.TimeSinceStartup - GameState.PreviousFrameTime); } }
    }
}
