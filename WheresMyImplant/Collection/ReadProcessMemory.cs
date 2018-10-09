using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class ReadProcessMemory : Base
    {
        private const Int32 MEM_COMMIT = 0x1000;
        private const Int32 MEM_FREE = 0x10000;
        private const Int32 MEM_RESERVE = 0x2000;

        private Int32 processID;
        private IntPtr hProcess;
        private StringBuilder memoryOutput = new StringBuilder();

        ////////////////////////////////////////////////////////////////////////////////
        // Default Constructor
        ////////////////////////////////////////////////////////////////////////////////
        internal ReadProcessMemory(Int32 processID)
        {
            this.processID = processID;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Open the process
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean OpenProcess()
        {
            hProcess = kernel32.OpenProcess(kernel32.PROCESS_QUERY_INFORMATION | kernel32.PROCESS_VM_READ, false, (UInt32)processID);
            if (IntPtr.Zero == hProcess)
            {
                Console.WriteLine("[-] Unable to OpenProcess {0}", processID);
                return false;
            }
            Console.WriteLine("[+] Handle Received 0x{0}", hProcess.ToString("X4"));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Scrape the processes memory for printable characters
        // Todo: Check if 32 or 64 bit system
        // Todo: Check per region of memory
        ////////////////////////////////////////////////////////////////////////////////
        internal void ReadProcesMemory()
        {
            Winbase._SYSTEM_INFO systemInfo;
            kernel32.GetSystemInfo(out systemInfo);
            UInt64 bottom = (UInt64)systemInfo.lpMinimumApplicationAddress.ToInt64();
            UInt64 top = (UInt64)systemInfo.lpMaximumApplicationAddress.ToInt64();
            IntPtr currentAddress = systemInfo.lpMinimumApplicationAddress;
            Console.WriteLine("[*] Current: " + currentAddress.ToInt64());
            Console.WriteLine("[*] Bottom:  " + bottom);
            Console.WriteLine("[*] Top:     " + top);

            StringBuilder regions = new StringBuilder();
            Console.WriteLine("[+] Scraping Regions");
            while (bottom < top)
            {
                Winnt._MEMORY_BASIC_INFORMATION64 memoryBasicInformation;
                kernel32.VirtualQueryEx64(hProcess, currentAddress, out memoryBasicInformation, (UInt32)Marshal.SizeOf(typeof(Winnt._MEMORY_BASIC_INFORMATION64)));
                regions.Append(".");    
                //Only need areas that are going to be written and read
                if ((memoryBasicInformation.Protect == Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE 
                    || memoryBasicInformation.Protect == Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE) 
                    && memoryBasicInformation.State == MEM_COMMIT)
                {
                    Byte[] buffer = new Byte[memoryBasicInformation.RegionSize];
                    IntPtr lpBuffer = Marshal.AllocHGlobal(buffer.Length);
                    Marshal.Copy(buffer, 0, lpBuffer, buffer.Length);
                    UInt32 bytesRead = 0;
                    if (kernel32.ReadProcessMemory64(hProcess, memoryBasicInformation.BaseAddress, lpBuffer, (UInt64)memoryBasicInformation.RegionSize, ref bytesRead))
                    {
                        Marshal.Copy(lpBuffer, buffer, 0, buffer.Length);
                        for (UInt64 i = 0; i < memoryBasicInformation.RegionSize; i++)
                        {
                            Char instance = (Char)buffer[i];
                            if (!Char.IsControl(instance))
                            {
                                memoryOutput.Append(instance);
                            }
                        }
                    }
                    Marshal.FreeHGlobal(lpBuffer);
                }
                bottom += memoryBasicInformation.RegionSize;
                currentAddress = new IntPtr((Int64)bottom);
            }
            Console.WriteLine(regions.ToString());
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Return the printable characters that were discovered
        ////////////////////////////////////////////////////////////////////////////////
        internal String GetPrintableMemory()
        {
            return memoryOutput.ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Default Deconstructor
        ////////////////////////////////////////////////////////////////////////////////
        ~ReadProcessMemory()
        {

        }
    }
}