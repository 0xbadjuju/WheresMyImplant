using System;

namespace WheresMyImplant
{
    sealed class SMB2SessionLogoffRequest
    {
        private readonly Byte[] StructureSize = { 0x04, 0x00 };
        private readonly Byte[] Reserved = { 0x00, 0x00 };

        internal SMB2SessionLogoffRequest()
        {

        }

        internal Byte[] GetRequest()
        {
            return Misc.Combine(StructureSize, Reserved);
        }
    }
}
