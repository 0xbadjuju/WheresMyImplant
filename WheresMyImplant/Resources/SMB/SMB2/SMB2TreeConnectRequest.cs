using System;
using System.Linq;

namespace WheresMyImplant
{
    sealed class SMB2TreeConnectRequest
    {
        private readonly Byte[] StructureSize = { 0x09, 0x00 };
        private readonly Byte[] Reserved = { 0x00, 0x00 };
        private readonly Byte[] PathOffset = { 0x48, 0x00 };
        private Byte[] PathLength;
        private Byte[] Buffer;

        internal void SetPath(String share)
        {
            this.Buffer = System.Text.Encoding.Unicode.GetBytes(share);
            this.PathLength = BitConverter.GetBytes(Buffer.Length).Take(2).ToArray();
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(StructureSize, Reserved);
            request = Misc.Combine(request, PathOffset);
            request = Misc.Combine(request, PathLength);
            request = Misc.Combine(request, Buffer);
            return request;
        }
    }
}