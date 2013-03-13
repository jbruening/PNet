using System;
using Lidgren.Network;

namespace PNetS.Utils
{
    internal sealed class RPCProcessor
    {
        internal readonly Action<NetIncomingMessage, NetMessageInfo> Method;
        internal readonly bool DefaultContinueForwarding;

        public RPCProcessor(Action<NetIncomingMessage, NetMessageInfo> method, bool defaultContinueForwarding)
        {
            Method = method;
            DefaultContinueForwarding = defaultContinueForwarding;
        }
    }
}
