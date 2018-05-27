using System;
using System.Runtime.InteropServices;

using BOOL = System.Boolean;

using WORD = System.UInt16;
using DWORD = System.UInt32;
using QWORD = System.UInt64;

using LPVOID = System.IntPtr;
using DWORD_PTR = System.IntPtr;

namespace Unmanaged.Headers
{
    sealed class Winbase
    {
        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms682434(v=vs.85).aspx
        [Flags]
        public enum CREATION_FLAGS : uint
        {
            NONE = 0x0,
            CREATE_DEFAULT_ERROR_MODE = 0x04000000,
            CREATE_NEW_CONSOLE = 0x00000010,
            CREATE_NEW_PROCESS_GROUP = 0x00000200,
            CREATE_SEPARATE_WOW_VDM = 0x00000800,
            CREATE_SUSPENDED = 0x00000004,
            CREATE_UNICODE_ENVIRONMENT = 0x00000400,
            EXTENDED_STARTUPINFO_PRESENT = 0x00080000
        }

        [Flags]
        public enum INFO_PROCESSOR_ARCHITECTURE : ushort
        {
            PROCESSOR_ARCHITECTURE_INTEL = 0,
            PROCESSOR_ARCHITECTURE_ARM = 5,
            PROCESSOR_ARCHITECTURE_IA64 = 6,
            PROCESSOR_ARCHITECTURE_AMD64 = 9,
            PROCESSOR_ARCHITECTURE_ARM64 = 12,
            PROCESSOR_ARCHITECTURE_UNKNOWN = 0xffff
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms684873(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        public struct _PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public UInt32 dwProcessId;
            public UInt32 dwThreadId;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct _SECURITY_ATTRIBUTES
        {
            public DWORD nLength;
            public LPVOID lpSecurityDescriptor;
            public BOOL bInheritHandle;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct _STARTUPINFO
        {
            public UInt32 cb;
            public String lpReserved;
            public String lpDesktop;
            public String lpTitle;
            public UInt32 dwX;
            public UInt32 dwY;
            public UInt32 dwXSize;
            public UInt32 dwYSize;
            public UInt32 dwXCountChars;
            public UInt32 dwYCountChars;
            public UInt32 dwFillAttribute;
            public UInt32 dwFlags;
            public UInt16 wShowWindow;
            public UInt16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        };

        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms686331(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        public struct _STARTUPINFOEX
        {
            _STARTUPINFO StartupInfo;
            // PPROC_THREAD_ATTRIBUTE_LIST lpAttributeList;
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct _SYSTEM_INFO 
        {
            public INFO_PROCESSOR_ARCHITECTURE wProcessorArchitecture;
            public WORD wReserved;
            public DWORD dwPageSize;
            public LPVOID lpMinimumApplicationAddress;
            public LPVOID lpMaximumApplicationAddress;
            public DWORD_PTR dwActiveProcessorMask;
            public DWORD dwNumberOfProcessors;
            public DWORD dwProcessorType;
            public DWORD dwAllocationGranularity;
            public WORD wProcessorLevel;
            public WORD wProcessorRevision;
        }
    }
}