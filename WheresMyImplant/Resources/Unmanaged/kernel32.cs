using System;
using System.Runtime.InteropServices;
using System.Text;

namespace WheresMyImplant
{
    internal class kernel32
    {
        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern Boolean CloseHandle(IntPtr hProcess);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern Boolean CreateProcess(
            String lpApplicationName,
            String lpCommandLine, 
            ref Winbase._SECURITY_ATTRIBUTES lpProcessAttributes,
            ref Winbase._SECURITY_ATTRIBUTES lpThreadAttributes,
            Boolean bInheritHandles,
            Winbase.CREATION_FLAGS dwCreationFlags,
            IntPtr lpEnvironment,
            String lpCurrentDirectory,
            ref Winbase._STARTUPINFO lpStartupInfo,
            out Winbase._PROCESS_INFORMATION lpProcessInformation
            );

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern void GetNativeSystemInfo(out Winbase._SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern bool GetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern void GetSystemInfo(out Winbase._SYSTEM_INFO lpSystemInfo);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean IsWow64Process(IntPtr hProcess, out Boolean Wow64Process);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, UInt32 dwProcessId);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern Boolean OpenProcessToken(IntPtr hProcess, UInt32 dwDesiredAccess, out IntPtr hToken);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern IntPtr OpenThread(uint dwDesiredAccess, bool bInheritHandle, uint dwThreadId);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern Boolean OpenThreadToken(IntPtr ThreadHandle, UInt32 DesiredAccess, Boolean OpenAsSelf, ref IntPtr TokenHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError=true, EntryPoint = "ReadProcessMemory")]
        internal static extern Boolean ReadProcessMemory64(IntPtr hProcess, UInt64 lpBaseAddress, IntPtr lpBuffer, UInt64 nSize, ref UInt32 lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern UInt32 ResumeThread(IntPtr hThread);

        [DllImport("kernel32.dll", SetLastError=true)]
        internal static extern Boolean SetThreadContext(IntPtr hThread, IntPtr lpContext);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAlloc(IntPtr lpAddress, UInt32 dwSize, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern IntPtr VirtualAllocEx(IntPtr hHandle, IntPtr lpAddress, UInt32 dwSize, UInt32 flAllocationType, UInt32 flProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean VirtualProtectEx(IntPtr hHandle, IntPtr lpAddress, UInt32 dwSize, UInt32 flNewProtect, ref UInt32 lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError=true, EntryPoint="VirtualQueryEx")]
        internal static extern Int32 VirtualQueryEx32(IntPtr hProcess, IntPtr lpAddress, out Winnt._MEMORY_BASIC_INFORMATION32 lpBuffer, UInt32 dwLength);

        [DllImport("kernel32.dll", SetLastError=true, EntryPoint="VirtualQueryEx")]
        internal static extern Int32 VirtualQueryEx64(IntPtr hProcess, IntPtr lpAddress, out Winnt._MEMORY_BASIC_INFORMATION64 lpBuffer, UInt32 dwLength);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean WaitForSingleObject(IntPtr hProcess, UInt32 nSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        internal static extern Boolean WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, ref UInt64 lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesWritten);

    }
}