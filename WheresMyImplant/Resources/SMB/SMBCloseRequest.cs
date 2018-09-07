using System;

namespace WheresMyImplant
{
    class SMBCloseRequest
    {
        private readonly Byte[] WordCount = { 0x03 };
        private Byte[] FID;
        private readonly Byte[] LastWrite = { 0xff, 0xff, 0xff, 0xff };
        private readonly Byte[] ByteCount = { 0x00, 0x00 };

        internal SMBCloseRequest()
        {

        }

        internal void SetFID(Byte[] FID)
        {
            this.FID = FID;
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(WordCount, FID);
            request = Misc.Combine(request, LastWrite);
            return Misc.Combine(request, ByteCount);
        }
    }
}
