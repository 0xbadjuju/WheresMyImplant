using System.Runtime.InteropServices;

using USHORT = System.UInt16;

using PWSTR = System.IntPtr;

namespace Unmanaged
{
    sealed class Subauth
    {
        [StructLayout(LayoutKind.Sequential)]
        public struct _LSA_UNICODE_STRING
        {
            USHORT Length;
            USHORT MaximumLength;
            PWSTR Buffer;
        }
    }
}