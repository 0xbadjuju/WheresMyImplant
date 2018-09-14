using System;
using System.Linq;

namespace WheresMyImplant
{
    class DCERPCAUTH3
    {
        private readonly Byte[] Version = { 0x05 };
        private readonly Byte[] VersionMinor = { 0x00 };
        private readonly Byte[] PacketType = { 0x10 };
        private readonly Byte[] PacketFlags = { 0x03 };
        private readonly Byte[] DataRepresentation = { 0x10, 0x00, 0x00, 0x00 };
        private Byte[] FragLength = new Byte[2];
        private Byte[] AuthLength = new Byte[2];
        private Byte[] CallID = { 0x03, 0x00, 0x00, 0x00 };
        private readonly Byte[] MaxXmitFrag = { 0xd0, 0x16 };
        private readonly Byte[] MaxRecvFrag = { 0xd0, 0x16 };
        private readonly Byte[] AuthType = { 0x0a };
        private Byte[] AuthLevel = { 0x02 };
        private readonly Byte[] AuthPadLength = { 0x00 };
        private readonly Byte[] AuthReserved = { 0x00 };
        private readonly Byte[] ContextID = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] NTLMSSP;

        internal DCERPCAUTH3()
        {

        }

        internal void SetCallID(Byte[] CallID)
        {
            if (this.CallID.Length == CallID.Length)
            {
                this.CallID = CallID;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetAuthLevel(Byte[] AuthLevel)
        {
            if (this.AuthLevel.Length == AuthLevel.Length)
            {
                this.AuthLevel = AuthLevel;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetNTLMSSP(Byte[] NTLMSSP)
        {
            FragLength = BitConverter.GetBytes(NTLMSSP.Length + 28).Take(2).ToArray();
            AuthLength = BitConverter.GetBytes(NTLMSSP.Length).Take(2).ToArray();
            this.NTLMSSP = NTLMSSP;
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
            combine.Extend(MaxXmitFrag);
            combine.Extend(MaxRecvFrag);
            combine.Extend(AuthType);
            combine.Extend(AuthLevel);
            combine.Extend(AuthPadLength);
            combine.Extend(AuthReserved);
            combine.Extend(ContextID);
            combine.Extend(NTLMSSP);
            return combine.Retrieve();
        }
    }
}
