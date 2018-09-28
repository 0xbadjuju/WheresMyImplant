using System;

namespace WheresMyImplant
{
    class SMB2ReadRequest
    {
        private readonly Byte[] StructureSize = { 0x31, 0x00 };
        private readonly Byte[] Padding = { 0x50 };
        private readonly Byte[] Flags = { 0x00 };
        private Byte[] Length = { 0x00, 0x10, 0x00, 0x00 };
        private readonly Byte[] Offset = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private Byte[] GuidHandleFile;
        private readonly Byte[] MinimumCount = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Channel = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] RemainingBytes = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] ReadChannelInfoOffset = { 0x00, 0x00 };
        private readonly Byte[] ReadChannelInfoLength = { 0x00, 0x00 };
        private readonly Byte[] Buffer = { 0x30 };

        internal SMB2ReadRequest()
        {

        }

        internal void SetGuidHandleFile(Byte[] GuidHandleFile)
        {
            this.GuidHandleFile = GuidHandleFile;
        }

        internal void SetLength(Byte[] Length)
        {
            if (this.Length.Length == Length.Length)
            {
                this.Length = Length;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(StructureSize, Padding);
            request = Misc.Combine(request, Flags);
            request = Misc.Combine(request, Length);
            request = Misc.Combine(request, Offset);
            request = Misc.Combine(request, GuidHandleFile);
            request = Misc.Combine(request, MinimumCount);
            request = Misc.Combine(request, Channel);
            request = Misc.Combine(request, RemainingBytes);
            request = Misc.Combine(request, ReadChannelInfoOffset);
            request = Misc.Combine(request, ReadChannelInfoLength);
            return Misc.Combine(request, Buffer);
        }
    }
}
