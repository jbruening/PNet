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
    public class WaitForSeconds : YieldInstruction
    {
        float waitTime;
        double startTime;

        /// <summary>
        /// wait for the specified number of seconds
        /// </summary>
        /// <param name="seconds"></param>
        public WaitForSeconds(float seconds)
        {
            this.waitTime = seconds;
            this.startTime = Time.time;
        }

        /// <summary>
        /// when the yieldinstruction finishes
        /// </summary>
        public override bool IsDone
        {
            get
            {
                if (Time.time - startTime > waitTime)
                    return true;
                return false;
            }
        }
    }
}
