using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PNetS
{
    /// <summary>
    /// An object containing information about spawning a GameObject
    /// </summary>
    public class Prefab
    {
        /// <summary>
        /// Path to the resource description
        /// </summary>
        public string ResourcePath { get; private set; }

        internal List<Type> Components = new List<Type>();


    }
}
