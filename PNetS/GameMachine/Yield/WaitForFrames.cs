using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace PNetS
{
    /// <summary>
    /// Yield for the specified time
    /// </summary>
    public class WaitForFrames : YieldInstruction
    {
        private int _waitFrames;
        private int _frameCounter;

        /// <summary>
        /// wait for the specified number of seconds
        /// </summary>
        public WaitForFrames(int waitFrames)
        {
            _waitFrames = waitFrames;
        }

        /// <summary>
        /// when the yieldinstruction finishes
        /// </summary>
        public override bool IsDone()
        {
            _frameCounter++;
            if (_frameCounter < _waitFrames)
            {
                return false;
            }
            return true;
        }
    }
}
