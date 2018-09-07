using System;
using System.Linq;

namespace WheresMyImplant.Resources
{
    class SMBNTCreateAndXRequest
    {
        private readonly Byte[] WordCount = { 0x18 };
        private readonly Byte[] AndXCommand = { 0xff };
        private readonly Byte[] Reserved = { 0x00 };
        private readonly Byte[] AndXOffset = { 0x00, 0x00 };
        private readonly Byte[] Reserved2 = { 0x00 };
        private Byte[] FileNameLen;
        private readonly Byte[] CreateFlags = { 0x16, 0x00, 0x00, 0x00 };
        private readonly Byte[] RootFID = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] AccessMask = { 0x00, 0x00, 0x00, 0x02 };
        private readonly Byte[] AllocationSize = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] FileAttributes = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] ShareAccess = { 0x07, 0x00, 0x00, 0x00 };
        private readonly Byte[] Disposition = { 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] CreateOptions = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Impersonation = { 0x02, 0x00, 0x00, 0x00 };
        private readonly Byte[] SecurityFlags = { 0x00 };
        private Byte[] ByteCount;
        private Byte[] Filename;

        internal SMBNTCreateAndXRequest()
        {

        }

        internal void SetFileName(Byte[] Filename)
        {
            this.Filename = Filename;
            FileNameLen = BitConverter.GetBytes(Filename.Length - 1).Take(2).ToArray();
            ByteCount = BitConverter.GetBytes(Filename.Length).Take(2).ToArray();
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(WordCount, AndXCommand);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, AndXOffset);
            request = Misc.Combine(request, Reserved2);
            request = Misc.Combine(request, FileNameLen);
            request = Misc.Combine(request, CreateFlags);
            request = Misc.Combine(request, RootFID);
            request = Misc.Combine(request, AccessMask);
            request = Misc.Combine(request, AllocationSize);
            request = Misc.Combine(request, FileAttributes);
            request = Misc.Combine(request, ShareAccess);
            request = Misc.Combine(request, Disposition);
            request = Misc.Combine(request, CreateOptions);
            request = Misc.Combine(request, Impersonation);
            request = Misc.Combine(request, SecurityFlags);
            request = Misc.Combine(request, ByteCount);
            return  Misc.Combine(request, Filename);
        }
    }
}
