using System;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    public class Unmanaged
    {
        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32")]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, UInt32 dwSize, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern Boolean VirtualProtect(IntPtr lpAddress, UInt32 dwSize, UInt32 flNewProtect, ref UInt32 lpflOldProtect);

        [DllImport("kernel32")]
        public static extern IntPtr CreateThread(IntPtr lpThreadAttributes, UInt32 dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, UInt32 dwCreationFlags, ref UInt32 lpThreadId);

        [DllImport("kernel32")]
        public static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);

        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, UInt32 dwProcessId);

        [DllImport("kernel32")]
        public static extern IntPtr VirtualAllocEx(IntPtr hHandle, IntPtr lpAddress, UInt32 dwSize, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32")]
        public static extern Boolean WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern Boolean VirtualProtectEx(IntPtr hHandle, IntPtr lpAddress, UInt32 dwSize, UInt32 flNewProtect, ref UInt32 lpflOldProtect);

        [DllImport("kernel32")]
        public static extern IntPtr CreateRemoteThread(IntPtr hHandle, IntPtr lpThreadAttributes, UInt32 dwStackSize, IntPtr lpStartAddress, IntPtr lpParameter, UInt32 dwCreationFlags, ref UInt32 lpThreadId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        [DllImport("kernel32")]
        public static extern UInt32 WaitForSingleObjectEx(IntPtr hProcess, IntPtr hHandle, UInt32 dwMilliseconds);

        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        public static extern IntPtr LoadLibrary(string lpFileName);

        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern Boolean ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesRead);

        ////////////////////////////////////////////////////////////////////////////////
        public const UInt32 PROCESS_CREATE_THREAD = 0x0002;
        public const UInt32 PROCESS_QUERY_INFORMATION = 0x0400;
        public const UInt32 PROCESS_VM_OPERATION = 0x0008;
        public const UInt32 PROCESS_VM_WRITE = 0x0020;
        public const UInt32 PROCESS_VM_READ = 0x0010;

        public const UInt32 PROCESS_ALL_ACCESS = 0x1F0FFF;

        public const UInt32 MEM_COMMIT = 0x00001000;
        public const UInt32 MEM_RESERVE = 0x00002000;

        public const UInt32 PAGE_READWRITE = 0x04;
        public const UInt32 PAGE_EXECUTE_READ = 0x20;
        public const UInt32 PAGE_EXECUTE_READWRITE = 0x40;

    }
}