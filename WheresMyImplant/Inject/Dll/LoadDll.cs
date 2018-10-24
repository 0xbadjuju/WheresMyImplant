using System;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal sealed class LoadDll : Base, IDisposable
    {
        private String library;
        private IntPtr hThread;
        private IntPtr libraryPtr;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal LoadDll(String library)
        {
            this.library = library;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Execute()
        {
            IntPtr loadLibraryAddr = kernel32.LoadLibrary(library);

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            Console.WriteLine("[*] Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAlloc(lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE);
            if (IntPtr.Zero == lpBaseAddress)
            {
                Console.WriteLine("[-] Unable to allocate memory");
                return;
            }
            Console.WriteLine("[+] Allocated {0} at 0x{1}", dwSize, lpBaseAddress.ToString("X4"));

            ////////////////////////////////////////////////////////////////////////////////
            libraryPtr = Marshal.StringToHGlobalAnsi(library);
            Console.WriteLine("[*] Attempting to write process memory");
            IntPtr[] lpBaseAddressArray = new IntPtr[] {lpBaseAddress};
            Marshal.Copy(libraryPtr, lpBaseAddressArray, 0, lpBaseAddressArray.Length);
            Console.WriteLine("[+] Wrote bytes");

            ////////////////////////////////////////////////////////////////////////////////
            Winnt.MEMORY_PROTECTION_CONSTANTS lpflOldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_NOACCESS;
            Console.WriteLine("[*] Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            if (!kernel32.VirtualProtect(lpBaseAddress, dwSize, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref lpflOldProtect))
            {
                Console.WriteLine("[-] Memory Protection Operation Failed");
                return;
            }
            Console.WriteLine("[+] Set Memory Protection to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            Console.WriteLine("[*] Attempting to start thread");
            hThread = kernel32.CreateThread(lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            if (IntPtr.Zero == hThread)
            {
                Console.WriteLine("[-] CreateThread Failed");
                return;
            }
            Console.WriteLine("[+] Started Thread: 0x{0}", hThread.ToString("X4"));

            ///////////////////////////////////////////////////////////////////////////////
            //Unmanaged.WaitForSingleObject(hThread, 0xFFFFFFFF);
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        ~LoadDll()
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

            if (IntPtr.Zero != libraryPtr)
            {
                Marshal.FreeHGlobal(libraryPtr);
            }
        }
    }
}