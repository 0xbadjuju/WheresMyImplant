using System;
using System.Linq;

namespace WheresMyImplant
{
    sealed class SMB2SessionSetupRequest
    {
        private readonly Byte[] StructureSize = { 0x19, 0x00 };
        private readonly Byte[] Flags = { 0x00 };
        private readonly Byte[] SecurityMode = { 0x01 };
        private readonly Byte[] Capabilities = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Channel = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] BlobOffset = { 0x58, 0x00 };
        private Byte[] BlobLength = new Byte[2];
        private readonly Byte[] PreviousSessionID = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private Byte[] SecurityBlob;

        internal SMB2SessionSetupRequest()
        {
        }

        internal void SetSecurityBlob(Byte[] securityBlob)
        {
            BlobLength = BitConverter.GetBytes(securityBlob.Length).Take(2).ToArray();
            this.SecurityBlob = securityBlob;
        }

        internal Byte[] GetSMB2SessionSetupRequest()
        {
            Byte[] request = Misc.Combine(StructureSize, Flags);
            request = Misc.Combine(request, SecurityMode);
            request = Misc.Combine(request, Capabilities);
            request = Misc.Combine(request, Channel);
            request = Misc.Combine(request, BlobOffset);
            request = Misc.Combine(request, BlobLength);
            request = Misc.Combine(request, PreviousSessionID);
            request = Misc.Combine(request, SecurityBlob);
            return request;
        }
    }
}