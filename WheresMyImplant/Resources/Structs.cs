using System;
using System.Runtime.InteropServices;

using word = System.UInt16;
using dword = System.UInt32;
using qword = System.UInt64;

namespace Backup
{
    internal class Structs
    {
        

        

        

        ////////////////////////////////////////////////////////////////////////////////
        // Tokens
        ////////////////////////////////////////////////////////////////////////////////
        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms686331(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        internal struct _STARTUPINFO
        {
            internal UInt32 cb;
            internal String lpReserved;
            internal String lpDesktop;
            internal String lpTitle;
            internal UInt32 dwX;
            internal UInt32 dwY;
            internal UInt32 dwXSize;
            internal UInt32 dwYSize;
            internal UInt32 dwXCountChars;
            internal UInt32 dwYCountChars;
            internal UInt32 dwFillAttribute;
            internal UInt32 dwFlags;
            internal UInt16 wShowWindow;
            internal UInt16 cbReserved2;
            internal IntPtr lpReserved2;
            internal IntPtr hStdInput;
            internal IntPtr hStdOutput;
            internal IntPtr hStdError;
        };

        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms686331(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        internal struct _STARTUPINFOEX
        {
            _STARTUPINFO StartupInfo;
            // PPROC_THREAD_ATTRIBUTE_LIST lpAttributeList;
        };

        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms684873(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        internal struct _PROCESS_INFORMATION
        {
            internal IntPtr hProcess;
            internal IntPtr hThread;
            internal UInt32 dwProcessId;
            internal UInt32 dwThreadId;
        };

        //lpTokenAttributes
        [StructLayout(LayoutKind.Sequential)]
        internal struct _SECURITY_ATTRIBUTES
        {
            UInt32 nLength;
            IntPtr lpSecurityDescriptor;
            Boolean bInheritHandle;
        };

        



        

        

        

        

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _GUID {
            internal Int32 Data1;
            internal Int16 Data2;
            internal Int16 Data3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal Byte[] Data4;
        }

        

        internal struct _CREDENTIAL_ATTRIBUTE
        {
            String Keyword;
            Int32 Flags;
            Int32 ValueSize;
            IntPtr Value;
        }
    }
}