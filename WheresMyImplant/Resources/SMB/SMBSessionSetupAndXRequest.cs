using System;
using System.Linq;

namespace WheresMyImplant
{
    class SMBSessionSetupAndXRequest
    {
        private readonly Byte[] WordCount = { 0x0c };
        private readonly Byte[] AndXCommand = { 0xff };
        private readonly Byte[] Reserved = { 0x00 };
        private readonly Byte[] AndXOffset = { 0x00, 0x00 };
        private readonly Byte[] MaxBuffer = { 0xff, 0xff };
        private readonly Byte[] MaxMpxCount = { 0x02, 0x00 };
        private readonly Byte[] VCNumber = { 0x01, 0x00 };
        private readonly Byte[] SessionKey = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] SecurityBlobLength;
        private readonly Byte[] Reserved2 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Capabilities = { 0x44, 0x00, 0x00, 0x80 };
        private Byte[] ByteCount;
        private Byte[] SecurityBlob;
        private readonly Byte[] NativeOS = { 0x00, 0x00, 0x00 };
        private readonly Byte[] NativeLANManage = { 0x00, 0x00 };

        internal SMBSessionSetupAndXRequest()
        {

        }

        internal void SetSecurityBlog(Byte[] SecurityBlob)
        {
            this.SecurityBlob = SecurityBlob;
            ByteCount = BitConverter.GetBytes(SecurityBlob.Length).Take(2).ToArray();
            SecurityBlobLength = BitConverter.GetBytes(SecurityBlob.Length).Take(2).ToArray();
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(WordCount, AndXCommand);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, AndXOffset);
            request = Misc.Combine(request, MaxBuffer);
            request = Misc.Combine(request, MaxMpxCount);
            request = Misc.Combine(request, VCNumber);
            request = Misc.Combine(request, SessionKey);
            request = Misc.Combine(request, SecurityBlobLength);
            request = Misc.Combine(request, Reserved2);
            request = Misc.Combine(request, Capabilities);
            request = Misc.Combine(request, ByteCount);
            request = Misc.Combine(request, SecurityBlob);
            request = Misc.Combine(request, NativeOS);
            return Misc.Combine(request, NativeLANManage);
        }
    }
}
