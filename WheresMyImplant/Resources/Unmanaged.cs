using System;
using System.Runtime.InteropServices;
using System.Text;

using Unmanaged;

namespace Backup
{
    public class Unmanaged
    {
        ////////////////////////////////////////////////////////////////////////////////

        
        
                ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(UInt32 dwDesiredAccess, Boolean bInheritHandle, UInt32 dwProcessId);

        
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out UIntPtr lpNumberOfBytesWritten);

        
        ////////////////////////////////////////////////////////////////////////////////
        

        

        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern Boolean ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 nSize, ref UInt32 lpNumberOfBytesRead);

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        // Tokens
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentProcess();

        [DllImport("kernel32.dll")]
        public static extern Boolean OpenProcessToken(IntPtr hProcess, UInt32 dwDesiredAccess, out IntPtr hToken);

        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll")]
        public static extern IntPtr GetCurrentThread();

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenThread(UInt32 dwDesiredAccess, Boolean bInheritHandle, UInt32 dwThreadId);

        [DllImport("kernel32.dll")]
        public static extern Boolean OpenThreadToken(IntPtr ThreadHandle, UInt32 DesiredAccess, Boolean OpenAsSelf, ref IntPtr TokenHandle);

        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("kernel32.dll")]
        public static extern Boolean CloseHandle(IntPtr hProcess);

        
        
       
        [DllImport("advapi32.dll")]
        public static extern Boolean RevertToSelf();

        ////////////////////////////////////////////////////////////////////////////////
        
        ////////////////////////////////////////////////////////////////////////////////
        

        

        

        

        

        

        [DllImport("ntdll.dll")]
        public static extern int NtFilterToken(
            IntPtr TokenHandle,
            UInt32 Flags,
            IntPtr SidsToDisable,
            IntPtr PrivilegesToDelete,
            IntPtr RestrictedSids,
            ref IntPtr hToken
        );

        ////////////////////////////////////////////////////////////////////////////////
        
    }
}