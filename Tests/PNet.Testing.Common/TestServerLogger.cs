using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace PNet.Testing.Common
{
    class TestServerLogger : PNetS.ILogger
    {
        public void Full(string info, params object[] args)
        {
            Debug.WriteLine(string.Format(info, args));
        }

        public void Info(string info, params object[] args)
        {
            Debug.WriteLine(string.Format(info, args));
        }

        public void Warning(string info, params object[] args)
        {
            Debug.WriteLine(string.Format(info, args));
        }

        public void Error(string info, params object[] args)
        {
            Assert.Fail(info, args);
        }
    }
}
