using System;
using System.Linq;

namespace WheresMyImplant
{
    class NetBIOSSessionService
    {
        private readonly Byte[] MessageType = { 0x00 };
        private Byte[] Length = new Byte[3];
        private Int32 headerLength;
        private Int32 dataLength;

        internal NetBIOSSessionService()
        {
        }

        internal void SetHeaderLength(Int32 headerLength)
        {
            this.headerLength = headerLength;
        }

        internal void SetDataLength(Int32 dataLength)
        {
            this.dataLength = dataLength;
        }

        internal Byte[] GetNetBIOSSessionService()
        {
            Length = BitConverter.GetBytes(this.headerLength + this.dataLength).Take(3).ToArray();
            Array.Reverse(Length);
            return Misc.Combine(MessageType, Length);
        }
    }
}