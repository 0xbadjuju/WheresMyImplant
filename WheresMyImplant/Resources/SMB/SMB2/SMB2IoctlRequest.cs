using System;
using System.Linq;
using System.Security.Cryptography;

namespace WheresMyImplant
{
    class SMB2IoctlRequest
    {
        private readonly Byte[] StructureSize = { 0x39, 0x00 };
        private readonly Byte[] Reserved = { 0x00, 0x00 };
        private readonly Byte[] Function = { 0x94, 0x01, 0x06, 0x00 };
        private readonly Byte[] GUIDHandle = { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff};
        private readonly Byte[] InDataBlobOffset = { 0x78, 0x00, 0x00, 0x00 };
        private Byte[] InDataBlobLength;
        private readonly Byte[] MaxIoctlInSize = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] OutDataBlobOffset = { 0x78, 0x00, 0x00, 0x00 };
        private readonly Byte[] OutDataBlobLength = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] MaxIoctlOutSize = { 0x00, 0x10, 0x00, 0x00 };
        private readonly Byte[] Flags = { 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] Reserved2 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] InDataMaxReferralLevel = { 0x04, 0x00 };
        private Byte[] InDataFileName;


        internal void SetFileName(String fileName)
        {
            this.InDataFileName = System.Text.Encoding.Unicode.GetBytes(fileName);
            this.InDataBlobLength = BitConverter.GetBytes(InDataFileName.Length + 2);
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(StructureSize, Reserved);
            request = Misc.Combine(request, Function);
            request = Misc.Combine(request, GUIDHandle);
            request = Misc.Combine(request, InDataBlobOffset);
            request = Misc.Combine(request, InDataBlobLength);
            request = Misc.Combine(request, MaxIoctlInSize);
            request = Misc.Combine(request, OutDataBlobOffset);
            request = Misc.Combine(request, OutDataBlobLength);
            request = Misc.Combine(request, MaxIoctlOutSize);
            request = Misc.Combine(request, Flags);
            request = Misc.Combine(request, Reserved2);
            request = Misc.Combine(request, InDataMaxReferralLevel);
            request = Misc.Combine(request, InDataFileName);
            return request;
        }
    }
}