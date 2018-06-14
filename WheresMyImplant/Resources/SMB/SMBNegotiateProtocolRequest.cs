using System;
using System.Linq;
using System.Security.Cryptography;

namespace WheresMyImplant
{
    class SMBNegotiateProtocolRequest
    {
        private readonly Byte[] lmDialectBytes = { 0x4e, 0x54, 0x20, 0x4c, 0x4d, 0x20, 0x30, 0x2e, 0x31, 0x32, 0x00 };
        private readonly Byte[] twoDialectBytes = { 0x53, 0x4d, 0x42, 0x20, 0x32, 0x2e, 0x30, 0x30, 0x32, 0x00 };
        private readonly Byte[] threeDialectBytes = { 0x53, 0x4d, 0x42, 0x20, 0x32, 0x2e, 0x3f, 0x3f, 0x3f, 0x00 };

        private readonly Byte[] WordCount = { 0x00 };
        private Byte[] ByteCount;
        private readonly Byte[] BufferFormatLM = { 0x02 };
        private Byte[] Name;
        private readonly Byte[] BufferFormat2= { 0x02 };
        private Byte[] Name2;
        private readonly Byte[] BufferFormat3 = {0x02 };
        private Byte[] Name3;

        internal SMBNegotiateProtocolRequest()
        {
            ByteCount = BitConverter.GetBytes((Int16)(lmDialectBytes.Length + twoDialectBytes.Length + threeDialectBytes.Length + 3));
            Name = lmDialectBytes;
            Name2 = twoDialectBytes;
            Name3 = threeDialectBytes;
        }

        internal Byte[] GetProtocols()
        {
            Byte[] protocols = Misc.Combine(WordCount, ByteCount);
            protocols = Misc.Combine(protocols, BufferFormatLM);
            protocols = Misc.Combine(protocols, Name);
            protocols = Misc.Combine(protocols, BufferFormat2);
            protocols = Misc.Combine(protocols, Name2);
            protocols = Misc.Combine(protocols, BufferFormat3);
            protocols = Misc.Combine(protocols, Name3);
            return protocols;
        }
    }
}