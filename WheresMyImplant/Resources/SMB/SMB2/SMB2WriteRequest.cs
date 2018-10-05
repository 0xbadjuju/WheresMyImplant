using System;

namespace WheresMyImplant
{
    sealed class SMB2WriteRequest
    {
        private readonly Byte[] StructureSize = { 0x31, 0x00 };
        private readonly Byte[] DataOffset = { 0x70, 0x00 };
        private Byte[] BufferLength;
        private Byte[] Offset = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private Byte[] FileID;
        private readonly Byte[] Channel = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] RemainingBytes = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] WriteChannelInfoOffset = { 0x00, 0x00 };
        private readonly Byte[] WriteChannelInfoLength = { 0x00, 0x00 };
        private readonly Byte[] Flags = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] Buffer = new Byte[0];

        internal SMB2WriteRequest()
        {

        }

        internal void SetLength(Int32 BufferLength)
        {
            this.BufferLength = BitConverter.GetBytes(BufferLength);
        }

        internal void SetOffset(Int64 Offset)
        {
            this.Offset = BitConverter.GetBytes(Offset);
        }

        internal void SetGuidHandleFile(Byte[] FileID)
        {
            this.FileID = FileID;
        }

        internal void SetBuffer(Byte[] Buffer)
        {
            this.Buffer = Buffer;
            BufferLength = BitConverter.GetBytes(Buffer.Length);
        }

        internal Byte[] GetRequest()
        {
            Combine combine = new Combine();
            combine.Extend(StructureSize);
            combine.Extend(DataOffset);
            combine.Extend(BufferLength);
            combine.Extend(Offset);
            combine.Extend(FileID);
            combine.Extend(Channel);
            combine.Extend(RemainingBytes);
            combine.Extend(WriteChannelInfoOffset);
            combine.Extend(WriteChannelInfoLength);
            combine.Extend(Flags);
            combine.Extend(Buffer);
            return combine.Retrieve();
        }
    }
}
