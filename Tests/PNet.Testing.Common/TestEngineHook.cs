using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PNetC;

namespace PNet.Testing.Common
{
    public class TestEngineHook : IEngineHook
    {
        private Thread _clientThread;

        public int UpdateSleepTime = 30;
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

        public void StartUpdateThread()
        {
            _clientThread = new Thread(DoUpdateThread);
            _clientThread.Start();
        }
        public void StopUpdateThread()
        {
            if (_clientThread != null && _clientThread.IsAlive)
                _clientThread.Abort();
        }

        private void DoUpdateThread()
        {
            while (true)
            {
                if (EngineUpdate != null)
                    EngineUpdate();
                Thread.Sleep(UpdateSleepTime);
            }
        }
    }
}
