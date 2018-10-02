using System;

namespace WheresMyImplant
{
    sealed class SMB2WriteRequest
    {
        private readonly Byte[] StructureSize = { 0x31, 0x00 };
        private readonly Byte[] DataOffset = { 0x70, 0x00 };
        private Byte[] Length;
        private readonly Byte[] Offset = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private Byte[] FileID;
        private readonly Byte[] Channel = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] RemainingBytes = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] WriteChannelInfoOffset = { 0x00, 0x00 };
        private readonly Byte[] WriteChannelInfoLength = { 0x00, 0x00 };
        private readonly Byte[] Flags = { 0x00, 0x00, 0x00, 0x00 };

        internal SMB2WriteRequest()
        {

        }

        internal void SetLength(Int32 dwLength)
        {
            Length = BitConverter.GetBytes(dwLength);
        }

        internal void SetGuidHandleFile(Byte[] FileID)
        {
            this.FileID = FileID;
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(StructureSize, DataOffset);
            request = Misc.Combine(request, Length);
            request = Misc.Combine(request, Offset);
            request = Misc.Combine(request, FileID);
            request = Misc.Combine(request, Channel);
            request = Misc.Combine(request, RemainingBytes);
            request = Misc.Combine(request, WriteChannelInfoOffset);
            request = Misc.Combine(request, WriteChannelInfoLength);
            return Misc.Combine(request, Flags);
        }
    }
}
