using System;

namespace WheresMyImplant
{
    class SMBReadAndXRequest
    {
        private readonly Byte[] WordCount = { 0x0a };
        private readonly Byte[] AndXCommand = { 0xff };
        private readonly Byte[] Reserved = { 0x00 };
        private readonly Byte[] AndXOffset = { 0x00, 0x00 };
        private readonly Byte[] FID = { 0x00, 0x40 };
        private readonly Byte[] Offset = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] MaxCountLow = { 0x58, 0x02 };
        private readonly Byte[] MinCount = { 0x58, 0x02 };
        private readonly Byte[] Unknown = { 0xff, 0xff, 0xff, 0xff };
        private readonly Byte[] Remaining = { 0x00, 0x00 };
        private readonly Byte[] ByteCount = { 0x00, 0x00 };

        internal SMBReadAndXRequest()
        {

        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(WordCount, AndXCommand);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, AndXOffset);
            request = Misc.Combine(request, FID);
            request = Misc.Combine(request, Offset);
            request = Misc.Combine(request, MaxCountLow);
            request = Misc.Combine(request, MinCount);
            request = Misc.Combine(request, Unknown);
            request = Misc.Combine(request, Remaining);
            return Misc.Combine(request, ByteCount);
        }
    }
}
