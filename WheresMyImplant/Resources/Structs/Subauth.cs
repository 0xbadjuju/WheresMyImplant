using System.Runtime.InteropServices;

using USHORT = System.UInt16;

using PWSTR = System.IntPtr;

namespace WheresMyImplant
{
    class Subauth
    {
        [StructLayout(LayoutKind.Sequential)]
        internal struct _LSA_UNICODE_STRING
        {
            USHORT Length;
            USHORT MaximumLength;
            PWSTR Buffer;
        }
    }
}