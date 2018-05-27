using System.Runtime.InteropServices;

using BOOLEAN = System.Boolean;

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

namespace Unmanaged
{
    sealed class dbghelp
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _LOADED_IMAGE {
            public string ModuleName;
            public HANDLE hFile;
            public System.IntPtr MappedAddress;
            public Winnt._IMAGE_NT_HEADERS FileHeader;
            public Winnt._IMAGE_SECTION_HEADER LastRvaSection;
            public ULONG NumberOfSections;
            public Winnt._IMAGE_SECTION_HEADER Sections;
            public ULONG Characteristics;
            public BOOLEAN fSystemImage;
            public BOOLEAN fDOSImage;
            public BOOLEAN fReadOnly;
            public System.IntPtr Version;
            public Winternl._LIST_ENTRY Links;
            public ULONG SizeOfImage;
        }

        [DllImport("dbghelp.dll", SetLastError=true)]
        public static extern bool MiniDumpCallback(
            PVOID CallbackParam,
            System.IntPtr CallbackInput,
            System.IntPtr CallbackOutput
        );

        [DllImport("dbghelp.dll", SetLastError=true)]
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