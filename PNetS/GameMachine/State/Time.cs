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
        /// Time since the game started, affected by the timescale
        /// </summary>
        public static double time 
        { 
            get 
            { 
                return GameState.TimeSinceStartup - GameState.TimeSinceStartupScaleOffset; 
            } 
        }
        /// <summary>
        /// time since the game started, unaffected by timescale
        /// </summary>
        public static double realtimeSinceStartup { get { return GameState.TimeSinceStartup; } }
        static double _scale = 1d;
        /// <summary>
        /// Time scaling. Setting to anything other than 1 will affect time and deltaTime.
        /// </summary>
        public static double Scale
        {
            get { return _scale; }
            set
            {
                _scale = Math.Min(Math.Max(0, value), double.MaxValue);
            }
        }
        /// <summary>
        /// Time since last 'frame', scaled via the Scale
        /// </summary>
        public static float deltaTime { get { return (float)((GameState.TimeSinceStartup - GameState.PreviousFrameTime) * _scale); } }
    }
}
