using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WheresMyImplant
{
    class SVCCTLSCMCloseServiceHandle
    {
        private Byte[] ContextHandle;

        internal SVCCTLSCMCloseServiceHandle()
        {

        }
        
        internal void SetContextHandle(Byte[] ContextHandle)
        {
            this.ContextHandle = ContextHandle;
        }

        internal Byte[] GetRequest()
        {
            return ContextHandle;
        }
    }
}
