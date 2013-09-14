using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using PNetC;

namespace UnitTestsPNetC
{
    class TestablePNet : PNetC.Net
    {
        private static PropertyInfo _playerIDProp = typeof (PNetC.Net).GetProperty("PlayerId");

        public TestablePNet(IEngineHook engineHook) : base(engineHook)
        {
            
        }

        public TestablePNet() : this(new TestEngineHook()){}

        public ushort TestablePlayerID
        {
            get { return PlayerId; }
            set { _playerIDProp.SetValue(this, value, null); }
        }
    }
}
