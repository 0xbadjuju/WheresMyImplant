using System;
using System.Linq;

namespace WheresMyImplant
{
    class DCERPCRequest
    {
        private readonly Byte[] Version = { 0x05 };
        private readonly Byte[] VersionMinor = { 0x00 };
        private readonly Byte[] PacketType = { 0x00 };
        private Byte[] PacketFlags = new Byte[1];
        private readonly Byte[] DataRepresentation = { 0x10, 0x00, 0x00, 0x00 };
        private Byte[] FragLength;
        private Byte[] AuthLength;
        private Byte[] CallID;
        private Byte[] AllocHint;
        private Byte[] ContextID;
        private Byte[] Opnum;
        private Byte[] Data = new Byte[0];

        internal DCERPCRequest()
        {

        }

        internal void SetPacketFlags(Byte[] PacketFlags)
        {
            if (this.PacketFlags.Length == PacketFlags.Length)
            {
                this.PacketFlags = PacketFlags;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetFragLength(Int32 dwFragLength, Int32 dwAuthLength, Int32 dwAuthPadding)
        {
            Int32 dwFullAuthLength = 0;
            if (dwAuthLength > 0)
            {
                dwFullAuthLength = dwAuthLength + dwAuthPadding + 8;
            }
            FragLength = BitConverter.GetBytes(dwFragLength + 24 + dwFullAuthLength + Data.Length).Take(2).ToArray();
            AuthLength = BitConverter.GetBytes(dwAuthLength).Take(2).ToArray();
            AllocHint = BitConverter.GetBytes(dwFragLength + Data.Length);
        }

        internal void SetCallID(Byte[] CallID)
        {
            this.CallID = CallID;
        }

        internal void SetContextID(Byte[] ContextID)
        {
            this.ContextID = ContextID;
        }

        internal void SetOpnum(Byte[] Opnum)
        {
            this.Opnum = Opnum;
        }

        internal void SetData(Byte[] Data)
        {
            this.Data = Data;
        }

        internal Byte[] GetRequest()
        {
            Combine combine = new Combine();
            combine.Extend(Version);
            combine.Extend(VersionMinor);
            combine.Extend(PacketType);
            combine.Extend(PacketFlags);
            combine.Extend(DataRepresentation);
            combine.Extend(FragLength);
            combine.Extend(AuthLength);
            combine.Extend(CallID);
            combine.Extend(AllocHint);
            combine.Extend(ContextID);
            combine.Extend(Opnum);
            combine.Extend(Data);
            return combine.Retrieve();
        }
    }
}
