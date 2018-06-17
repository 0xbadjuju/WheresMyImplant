using System;

namespace WheresMyImplant
{
    class SMB2GetInfo
    {
        private readonly Byte[] StructureSize = { 0x29, 0x00 };
        private Byte[] Class = new Byte[1];
        private Byte[] InfoLevel = new Byte[1];
        private Byte[] MaxResponseSize = new Byte[4];
        private Byte[] GetInfoInputOffset = new Byte[2];
        private readonly Byte[] Reserved = { 0x00, 0x00 };
        private readonly Byte[] InputBufferLength = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] AdditionalInformation = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Flags = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] GUIDHandleFile;
        private Byte[] Buffer = new Byte[1];


        internal SMB2GetInfo()
        {
        }

        internal void SetClass(Byte[] Class)
        {
            this.Class = Class;
        }

        internal void SetInfoLevel(Byte[] infoLevel)
        {
            this.InfoLevel = infoLevel;
        }

        internal void SetMaxResponseSize(Byte[] maxResponseSize)
        {
            this.MaxResponseSize = maxResponseSize;
        }

        internal void SetGetInfoInputOffset(Byte[] getInfoInputOffset)
        {
            this.GetInfoInputOffset = getInfoInputOffset;
        }

        internal void SetGUIDHandleFile(Byte[] guidHandleFile)
        {
            this.GUIDHandleFile = guidHandleFile;
        }

        internal void SetBuffer(Int32 bufferSize)
        {
            Buffer = new Byte[bufferSize];
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(StructureSize, Class);
            request = Misc.Combine(request, InfoLevel);
            request = Misc.Combine(request, MaxResponseSize);
            request = Misc.Combine(request, GetInfoInputOffset);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, InputBufferLength);
            request = Misc.Combine(request, AdditionalInformation);
            request = Misc.Combine(request, Flags);
            request = Misc.Combine(request, GUIDHandleFile);
            request = Misc.Combine(request, Buffer);
            return request;
        }

    }
}