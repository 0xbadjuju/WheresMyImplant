using System;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    public class InjectDll : Base
    {
        InjectDll(string library)
        {
            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = Unmanaged.VirtualAlloc(lpAddress, dwSize, Unmanaged.MEM_COMMIT | Unmanaged.MEM_RESERVE, Winnt.PAGE_READWRITE);
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
            Boolean virtualProtectExResult = Unmanaged.VirtualProtect(lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect);
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