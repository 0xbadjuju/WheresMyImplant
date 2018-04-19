using System.Runtime.InteropServices;

using WORD = System.UInt16;
using DWORD = System.UInt32;
using QWORD = System.UInt64;

using HANDLE = System.IntPtr;
using PVOID = System.IntPtr;
using LPVOID = System.IntPtr;
using DWORD_PTR = System.IntPtr;

using ULONG = System.UInt32;
using ULONG32 = System.UInt32;
using ULONG64 = System.UInt64;

using BOOL = System.Boolean;

namespace WheresMyImplant
{
    class dbghelp
    {
        [DllImport("dbghelp", SetLastError = true)]
        public static extern bool MiniDumpCallback(
            PVOID CallbackParam,
            System.IntPtr CallbackInput,
            System.IntPtr CallbackOutput
        );

        [DllImport("dbghelp", SetLastError = true)]
        public static extern bool MiniDumpWriteDump(
            HANDLE hProcess,
            DWORD ProcessId,
            HANDLE hFile,
            minidumpapiset._MINIDUMP_TYPE DumpType,
            System.IntPtr ExceptionParam,
            System.IntPtr UserStreamParam,
            System.IntPtr CallbackParam
        );
    }
}