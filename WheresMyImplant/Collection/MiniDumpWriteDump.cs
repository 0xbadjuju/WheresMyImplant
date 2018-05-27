using System;
using System.IO;

using Unmanaged.Headers;
using Unmanaged.Libraries;

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
                WriteOutputGood(String.Format("Received Handle: {0}", proc.Handle.ToInt64()));
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
                            WriteOutputBad("MiniDump Failed");
                        }
                        WriteOutputGood("Dump File Created");
                    }
                }
                catch (Exception error)
                {
                    WriteOutputBad(error.ToString());
                }
            }
        }
    }
}