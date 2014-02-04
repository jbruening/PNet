using System.Reflection;
using PNetC;

namespace PNet.Testing.Common
{
    public class TestablePNet : PNetC.Net
    {
        private static readonly PropertyInfo PlayerIDProp = typeof (PNetC.Net).GetProperty("PlayerId");

        public TestablePNet(IEngineHook engineHook) : base(engineHook)
        {
            Debug.Logger = new TestClientLogger();
        }

        public TestablePNet() : this(new TestEngineHook()){}

        public TestEngineHook TestableHook
        {
            get { return EngineHook as TestEngineHook; }
        }

        public ushort TestablePlayerID
        {
            get { return PlayerId; }
            set { PlayerIDProp.SetValue(this, value, null); }
        }

        public static ClientConfiguration GetTestConnectionConfig()
        {
            return new ClientConfiguration("127.0.0.1", 14000, appIdentifier: "PNetTest");
        }
    }
}
