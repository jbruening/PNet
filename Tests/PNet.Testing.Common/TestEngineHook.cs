using System;
using System.Collections.Generic;
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

        public Dictionary<string, NetworkView> Instantiates = new Dictionary<string, NetworkView>();
        public object Instantiate(string path, NetworkView newView, Vector3 location, Quaternion rotation)
        {
            Console.WriteLine("{0} instantiated", path);
            Instantiates.Add(path, newView);
            return path;
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
            _quit = true;
        }

        private bool _quit = false;

        public bool UpdatePause = false;

        private void DoUpdateThread()
        {
            while (!_quit)
            {
                if (EngineUpdate != null && !UpdatePause)
                    EngineUpdate();
                Thread.Sleep(UpdateSleepTime);
            }
        }
    }
}
