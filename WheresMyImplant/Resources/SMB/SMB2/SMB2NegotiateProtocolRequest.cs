using System;
using System.Linq;
using System.Security.Cryptography;

namespace WheresMyImplant
{
    sealed class SMB2NegotiateProtocolRequest
    {
        private readonly Byte[] StructureSize = { 0x24, 0x00 };
        private readonly Byte[] DialectCount = { 0x02, 0x00 };
        private readonly Byte[] SecurityMode = { 0x01, 0x00 };
        private readonly Byte[] Reserved = { 0x00, 0x00 };
        private readonly Byte[] Capabilities = { 0x40, 0x00, 0x00, 0x00 };
        private readonly Byte[] ClientGUID = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] NegotiateContextOffset = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] NegotiateContextCount = { 0x00, 0x00 };
        private readonly Byte[] Reserved2 = { 0x00, 0x00 };
        private readonly Byte[] Dialect = { 0x02, 0x02 };
        private readonly Byte[] Dialect2 = { 0x10, 0x02 };

        internal Byte[] GetProtocols()
        {
            Byte[] protocols = Misc.Combine(StructureSize, DialectCount);
            protocols = Misc.Combine(protocols, SecurityMode);
            protocols = Misc.Combine(protocols, Reserved);
            protocols = Misc.Combine(protocols, Capabilities);
            protocols = Misc.Combine(protocols, ClientGUID);
            protocols = Misc.Combine(protocols, NegotiateContextOffset);
            protocols = Misc.Combine(protocols, NegotiateContextCount);
            protocols = Misc.Combine(protocols, Reserved2);
            protocols = Misc.Combine(protocols, Dialect);
            protocols = Misc.Combine(protocols, Dialect2);
            return protocols;
        }
    }
}