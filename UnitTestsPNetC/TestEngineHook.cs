using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNetC;

namespace UnitTestsPNetC
{
    class TestEngineHook : IEngineHook
    {
        public event Action EngineUpdate;
        public object Instantiate(string path, NetworkView newView, Vector3 location, Quaternion rotation)
        {
            return null;
        }

        public object AddNetworkView(NetworkView view, NetworkView newView, string customFunction)
        {
            return null;
        }

        public void RunOneUpdate()
        {
            Assert.IsNotNull(EngineUpdate);
            EngineUpdate();
        }
    }
}
