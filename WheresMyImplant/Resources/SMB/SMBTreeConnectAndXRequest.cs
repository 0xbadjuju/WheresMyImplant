using System;
using System.Linq;

namespace WheresMyImplant
{
    class SMBTreeConnectAndXRequest
    {
        private readonly Byte[] WordCount = { 0x04 };
        private readonly Byte[] AndXCommand = { 0xff };
        private readonly Byte[] Reserved = { 0x00 };
        private readonly Byte[] AndXOffset = { 0x00, 0x00 };
        private readonly Byte[] Flags = { 0x00, 0x00 };
        private readonly Byte[] PasswordLength = { 0x01, 0x00 };
        private Byte[] ByteCount;
        private readonly Byte[] Password = { 0x00 };
        private Byte[] Tree;
        private readonly Byte[] Service = { 0x3f, 0x3f, 0x3f, 0x3f, 0x3f, 0x00 };

        internal SMBTreeConnectAndXRequest()
        {

        }

        internal void SetTree(Byte[] Tree)
        {
            this.Tree = Tree;
            ByteCount = BitConverter.GetBytes(Tree.Length + 7).Take(2).ToArray();
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(WordCount, AndXCommand);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, AndXOffset);
            request = Misc.Combine(request, Flags);
            request = Misc.Combine(request, PasswordLength);
            request = Misc.Combine(request, ByteCount);
            request = Misc.Combine(request, Password);
            request = Misc.Combine(request, Tree);
            return Misc.Combine(request, Service);
        }
    }
}
