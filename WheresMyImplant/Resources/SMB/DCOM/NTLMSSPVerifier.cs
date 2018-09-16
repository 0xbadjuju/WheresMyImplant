using System;

namespace WheresMyImplant
{
    class NTLMSSPVerifier
    {
        private Byte[] AuthPadding = new Byte[0];
        private readonly Byte[] AuthType = { 0x0a };
        private Byte[] AuthLevel;
        private Byte[] AuthPadLen;
        private readonly Byte[] AuthReserved = { 0x00 };
        private readonly Byte[] AuthContextID = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] NTLMSSPVerifierVersionNumber = { 0x01, 0x00, 0x00, 0x00 };
        private Byte[] NTLMSSPVerifierChecksum = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private Byte[] NTLMSSPVerifierSequenceNumber;

        internal NTLMSSPVerifier()
        {

        }

        internal void SetAuthLevel(Byte[] AuthLevel)
        {
            this.AuthLevel = AuthLevel;
        }

        internal void SetAuthPadLen(Int32 dwAuthPadLen)
        {
            switch (dwAuthPadLen)
            {
                case 0:
                    AuthPadLen = new Byte[] { 0x00 };
                    return;
                case 4:
                    AuthPadding = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
                    AuthPadLen = new Byte[] { 0x04 };
                    return;
                case 8:
                    AuthPadding = new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    AuthPadLen = new Byte[] { 0x08 };
                    return;
                case 12:
                    AuthPadding = new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                    AuthPadLen = new Byte[] { 0x0c };
                    return;
                default:
                    Console.WriteLine("Invalid AuthPadLen");
                    return;
            }
        }

        internal void SetNTLMSSPVerifierChecksum(Byte[] NTLMSSPVerifierChecksum)
        {
            this.NTLMSSPVerifierChecksum = NTLMSSPVerifierChecksum;
        }

        internal void SetNTLMSSPVerifierSequenceNumber(Byte[] NTLMSSPVerifierSequenceNumber)
        {
            this.NTLMSSPVerifierSequenceNumber = NTLMSSPVerifierSequenceNumber;
        }

        internal Byte[] GetRequest()
        {
            Combine combine = new Combine();
            combine.Extend(AuthPadding);
            combine.Extend(AuthType);
            combine.Extend(AuthLevel);
            combine.Extend(AuthPadLen);
            combine.Extend(AuthReserved);
            combine.Extend(AuthContextID);
            combine.Extend(NTLMSSPVerifierVersionNumber);
            combine.Extend(NTLMSSPVerifierChecksum);
            combine.Extend(NTLMSSPVerifierSequenceNumber);
            return combine.Retrieve();
        }
    }
}
