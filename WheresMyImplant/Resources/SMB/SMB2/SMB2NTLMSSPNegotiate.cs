using System;
using System.Linq;
using System.Security.Cryptography;

namespace WheresMyImplant
{
    class SMB2NTLMSSPNegotiate
    {
        private String version = String.Empty;

        private readonly Byte[] InitialContextTokenID = { 0x60 };
        private Byte[] InitialcontextTokenLength;
        private readonly Byte[] ThisMechID = { 0x06 };
        private readonly Byte[] ThisMechLength = { 0x06 };
        private readonly Byte[] OID = { 0x2b, 0x06, 0x01, 0x05, 0x05, 0x02 };
        private readonly Byte[] InnerContextTokenID = { 0xa0 };
        private Byte[] InnerContextTokenLength;
        private readonly Byte[] InnerContextTokenID2 = { 0x30 };
        private Byte[] InnerContextTokenLength2;
        private readonly Byte[] MechTypesID = { 0xa0 };
        private readonly Byte[] MechTypesLength = { 0x0e };
        private readonly Byte[] MechTypesID2 = { 0x30 };
        private readonly Byte[] MechTypesLength2 = { 0x0c };
        private readonly Byte[] MechTypesID3 = { 0x06 };
        private readonly Byte[] MechTypesLength3 = { 0x0a };
        private readonly Byte[] MechType = { 0x2b, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x02, 0x0a };
        private readonly Byte[] MechTokenID = { 0xa2 };
        private Byte[] MechTokenLength;
        private readonly Byte[] NTLMSSPID = { 0x04 };
        private Byte[] NTLMSSPLength;
        private readonly Byte[] Identifier = { 0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00 };
        private readonly Byte[] MessageType = { 0x01, 0x00, 0x00, 0x00 };
        private Byte[] NegotiateFlags;
        private readonly Byte[] CallingWorkstationDomain = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] CallingWorkstationName = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        internal SMB2NTLMSSPNegotiate(String version)
        {
            this.version = version;
        }

        internal void SetFlags(Byte[] flags)
        {
            this.NegotiateFlags = flags;
        }

        internal Byte[] GetSMB2NTLMSSPNegotiate()
        {
            Byte[] NTLMSSPLength = BitConverter.GetBytes(32 + version.Length);
            NTLMSSPLength = new Byte[] { NTLMSSPLength[0] };

            InitialcontextTokenLength = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSPLength[0]) + 32) };
            InnerContextTokenLength = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSPLength[0]) + 22) };
            InnerContextTokenLength2 = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSPLength[0]) + 20) };
            MechTokenLength = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSPLength[0]) + 2) };

            Byte[] negotiate = Misc.Combine(InitialContextTokenID, InitialcontextTokenLength);
            negotiate = Misc.Combine(negotiate, ThisMechID);
            negotiate = Misc.Combine(negotiate, ThisMechLength);
            negotiate = Misc.Combine(negotiate, OID);
            negotiate = Misc.Combine(negotiate, InnerContextTokenID);
            negotiate = Misc.Combine(negotiate, InnerContextTokenLength);
            negotiate = Misc.Combine(negotiate, InnerContextTokenID2);
            negotiate = Misc.Combine(negotiate, InnerContextTokenLength2);
            negotiate = Misc.Combine(negotiate, MechTypesID);
            negotiate = Misc.Combine(negotiate, MechTypesLength);
            negotiate = Misc.Combine(negotiate, MechTypesID2);
            negotiate = Misc.Combine(negotiate, MechTypesLength2);
            negotiate = Misc.Combine(negotiate, MechTypesID3);
            negotiate = Misc.Combine(negotiate, MechTypesLength3);
            negotiate = Misc.Combine(negotiate, MechType);
            negotiate = Misc.Combine(negotiate, MechTokenID);
            negotiate = Misc.Combine(negotiate, MechTokenLength);
            negotiate = Misc.Combine(negotiate, NTLMSSPID);
            negotiate = Misc.Combine(negotiate, NTLMSSPLength);
            negotiate = Misc.Combine(negotiate, Identifier);
            negotiate = Misc.Combine(negotiate, MessageType);
            negotiate = Misc.Combine(negotiate, NegotiateFlags);
            negotiate = Misc.Combine(negotiate, CallingWorkstationDomain);
            negotiate = Misc.Combine(negotiate, CallingWorkstationName);

            if (version.Length > 0)
            {
                negotiate = Misc.Combine(negotiate, System.Text.Encoding.ASCII.GetBytes(version));
            }

            return negotiate;
        }
    }
}