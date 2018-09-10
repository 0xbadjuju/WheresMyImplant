using System;
using System.Linq;

namespace WheresMyImplant
{
    class SMBWriteAndXRequest
    {
        private readonly Byte[] WordCount = { 0x0e };
        private readonly Byte[] AndXCommand = { 0xff };
        private readonly Byte[] Reserved = { 0x00 };
        private readonly Byte[] AndXOffset = { 0x00, 0x00 };
        private Byte[] FID;
        private readonly Byte[] Offset = { 0xea, 0x03, 0x00, 0x00 };
        private readonly Byte[] Reserved2 = { 0xff, 0xff, 0xff, 0xff };
        private readonly Byte[] WriteMode = { 0x08, 0x00 };
        private Byte[] Remaining;
        private readonly Byte[] DataLengthHigh = { 0x00, 0x00 };
        private Byte[] DataLengthLow;
        private readonly Byte[] DataOffset = { 0x3f, 0x00 };
        private readonly Byte[] HighOffset = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] ByteCount;

        internal SMBWriteAndXRequest()
        {

        }

        internal void SetFID(Byte[] FID)
        {
            this.FID = FID;
        }

        internal void SetLength(Int32 dwLength)
        {
            Byte[] bLength = BitConverter.GetBytes(dwLength).Take(2).ToArray();
            Remaining = bLength;
            DataLengthLow = bLength;
            ByteCount = bLength;
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(WordCount, AndXCommand);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, AndXOffset);
            request = Misc.Combine(request, FID);
            request = Misc.Combine(request, Offset);
            request = Misc.Combine(request, Reserved2);
            request = Misc.Combine(request, WriteMode);
            request = Misc.Combine(request, Remaining);
            request = Misc.Combine(request, DataLengthHigh);
            request = Misc.Combine(request, DataLengthLow);
            request = Misc.Combine(request, DataOffset);
            request = Misc.Combine(request, HighOffset);
            return Misc.Combine(request, ByteCount);
        }
    }
}
