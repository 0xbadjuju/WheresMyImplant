using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WheresMyImplant
{
    class SVCCTLSCMCreateServiceW
    {
        private Byte[] ContextHandle;
        private Byte[] ServiceName_MaxCount;
        private Byte[] ServiceName_Offset = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] ServiceName_ActualCount;
        private Byte[] ServiceName;
        private Byte[] DisplayName_ReferentID;
        private Byte[] DisplayName_MaxCount;
        private readonly Byte[] DisplayName_Offset = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] DisplayName_ActualCount;
        private Byte[] DisplayName;
        private readonly Byte[] AccessMask = { 0xff, 0x01, 0x0f, 0x00 };
        private readonly Byte[] ServiceType = { 0x10, 0x00, 0x00, 0x00 };
        private readonly Byte[] ServiceStartType = { 0x03, 0x00, 0x00, 0x00 };
        private readonly Byte[] ServiceErrorControl = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] BinaryPathName_MaxCount;
        private readonly Byte[] BinaryPathName_Offset = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] BinaryPathName_ActualCount;
        private Byte[] BinaryPathName;
        private readonly Byte[] NULLPointer = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] TagID = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] NULLPointer2 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] DependSize = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] NULLPointer3 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] NULLPointer4 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] PasswordSize = { 0x00, 0x00, 0x00, 0x00 };

        internal SVCCTLSCMCreateServiceW()
        {
            DisplayName_ReferentID = Misc.Combine(BitConverter.GetBytes(Misc.GenerateUuidNumeric(2)), new Byte[] { 0x00, 0x00 });
        }
        
        internal void SetContextHandle(Byte[] ContextHandle)
        {
            this.ContextHandle = ContextHandle;
        }

    }
}
