using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PNet.Testing.Common
{
    class TestClientLogger : PNetC.ILogger
    {
        public void Full(PNetC.Net sender, string info, params object[] args)
        {
            Debug.WriteLine(string.Format(info, args));
        }

        public void Info(PNetC.Net sender, string info, params object[] args)
        {
            Debug.WriteLine(string.Format(info, args));
        }

        public void Warning(PNetC.Net sender, string info, params object[] args)
        {
            Debug.WriteLine(string.Format(info, args));
        }

        public void Error(PNetC.Net sender, string info, params object[] args)
        {
            Assert.Fail(info, args);
        }
    }
}
