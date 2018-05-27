using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal class InjectDll : Base
    {
        InjectDll(string library)
        {
            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAlloc(lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.PAGE_READWRITE);
            WriteOutputGood("Allocated " + dwSize + " at " + lpBaseAddress);

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            IntPtr libraryPtr = Marshal.StringToHGlobalAnsi(library);
            WriteOutputNeutral("Attempting to write process memory");

            //Marshal.Copy(libraryPtr, 0, lpBaseAddress, dwSize);
            WriteOutputGood("Wrote " + dwSize + " bytes");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            WriteOutputNeutral("Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            Boolean virtualProtectExResult = kernel32.VirtualProtect(lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect);
            if (virtualProtectExResult)
            {
                WriteOutputGood("Set Memory Protection to PAGE_EXECUTE_READ");
            }
            else
            {
                WriteOutputBad("Memory Protection Operation Failed");
            }
            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutputNeutral("Attempting to start thread");
            //IntPtr hThread = Unmanaged.CreateThread(lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            //WriteOutputGood("Started Thread: " + hThread);

            ///////////////////////////////////////////////////////////////////////////////
            //Unmanaged.WaitForSingleObject(hThread, 0xFFFFFFFF);
        }
    }
}