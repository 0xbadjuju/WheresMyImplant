using System;
using System.IO;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class MiniDumpWriteDump : Base
    {
        internal MiniDumpWriteDump() : base()
        {
        }

        internal void CreateMiniDump(UInt32 dwProcessId, String fileName)
        {
            using (System.Diagnostics.Process proc = System.Diagnostics.Process.GetProcessById((Int32)dwProcessId))
            {
                Console.WriteLine("[+] Received Handle: {0}", proc.Handle.ToString("X4"));
                try
                {
                    using (FileStream file = new FileStream(fileName, FileMode.Create))
                    {
                        if (!dbghelp.MiniDumpWriteDump(
                            proc.Handle,
                            dwProcessId,
                            file.SafeFileHandle.DangerousGetHandle(),
                            Minidumpapiset._MINIDUMP_TYPE.MiniDumpWithFullMemory,
                            IntPtr.Zero,
                            IntPtr.Zero,
                            IntPtr.Zero))
                        {
                            Console.WriteLine("[-] MiniDump Failed");
                        }
                        Console.WriteLine("[+] Dump File Created");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] {0}", ex.Message);
                }
            }
        }
    }
}