using System;

namespace WheresMyImplant
{
    sealed class SMB2SetInfo
    {
        private readonly Byte[] StructureSize = new Byte[] { 0x21, 0x00 };
        private Byte[] Class;
        private Byte[] InfoLevel;
        private Byte[] BufferLength;
        private readonly Byte[] BufferOffset = new Byte[] { 0x60, 0x00 };
        private readonly Byte[] Reserved = new Byte[] { 0x00, 0x00 };
        private readonly Byte[] AdditionalInformation = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] GUIDHandleFile;
        private Byte[] Buffer;

        internal SMB2SetInfo()
        {

        }

        internal void SetClass(Byte[] Class)
        {
            this.Class = Class;
        }

        internal void SetInfoLevel(Byte[] InfoLevel)
        {
            this.InfoLevel = InfoLevel;
        }

        internal void SetGUIDHandleFile(Byte[] GUIDHandleFile)
        {
            this.GUIDHandleFile = GUIDHandleFile;
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
            combine.Extend(Class);
            combine.Extend(InfoLevel);
            combine.Extend(BufferLength);
            combine.Extend(BufferOffset);
            combine.Extend(Reserved);
            combine.Extend(AdditionalInformation);
            combine.Extend(GUIDHandleFile);
            combine.Extend(Buffer);
            return combine.Retrieve();
        }
    }
}