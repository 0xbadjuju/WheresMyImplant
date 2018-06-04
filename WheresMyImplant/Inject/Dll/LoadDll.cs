using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

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
            WriteOutputNeutral("Attempting to allocate memory");
            IntPtr lpBaseAddress = kernel32.VirtualAlloc(lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.PAGE_READWRITE);
            if (IntPtr.Zero == lpBaseAddress)
            {
                WriteOutputBad("Unable to allocate memory");
                return;
            }
            WriteOutputGood(String.Format("Allocated {0} at 0x{1}", dwSize, lpBaseAddress.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            libraryPtr = Marshal.StringToHGlobalAnsi(library);
            WriteOutputNeutral("Attempting to write process memory");
            IntPtr[] lpBaseAddressArray = new IntPtr[] {lpBaseAddress};
            Marshal.Copy(libraryPtr, lpBaseAddressArray, 0, lpBaseAddressArray.Length);
            WriteOutputGood("Wrote bytes");

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            WriteOutputNeutral("Attempting to Alter Memory Protections to PAGE_EXECUTE_READ");
            if (!kernel32.VirtualProtect(lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect))
            {
                WriteOutputBad("Memory Protection Operation Failed");
                return;
            }
            WriteOutputGood("Set Memory Protection to PAGE_EXECUTE_READ");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            WriteOutputNeutral("Attempting to start thread");
            hThread = kernel32.CreateThread(lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            if (IntPtr.Zero == hThread)
            {
                WriteOutputBad("CreateThread Failed");
                return;
            }
            WriteOutputGood(String.Format("Started Thread: 0x{0}", hThread.ToString("X4")));

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