using System;

namespace WheresMyImplant
{
    class SVCCTLSCMStartServiceW
    {
        private Byte[] ContextHandle;
        private readonly Byte[] Unknown = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        internal SVCCTLSCMStartServiceW()
        {

        }

        internal void SetContextHandle(Byte[] ContextHandle)
        {
            this.ContextHandle = ContextHandle;
        }

        internal Byte[] GetRequest()
        {
            return Misc.Combine(ContextHandle, Unknown);
        }
    }
}
