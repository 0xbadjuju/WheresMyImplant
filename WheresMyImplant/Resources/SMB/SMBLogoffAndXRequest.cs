using System;

namespace WheresMyImplant
{
    class SMBLogoffAndXRequest
    {
        private readonly Byte[] WordCount = { 0x02 };
        private readonly Byte[] AndXCommand = { 0xff };
        private readonly Byte[] Reserved = { 0x00 };
        private readonly Byte[] AndXOffset = { 0x00, 0x00 };
        private readonly Byte[] ByteCount = { 0x00, 0x00 };

        internal SMBLogoffAndXRequest()
        {

        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(WordCount, AndXCommand);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, AndXOffset);
            return Misc.Combine(request, ByteCount);
        }
    }
}
