using System;
using System.Collections.Generic;
using System.Management;

using DomainInfo;

using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class Recon
    {
        public static void DomainControllers(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainControllers dc = new DomainControllers(ip, domain  + @"\" + username, password))
                {
                    dc.Query();
                    if (!String.IsNullOrEmpty(filename))
                        dc.OutCsv(filename);
                }
            }
            else
            {
                using (DomainControllers dc = new DomainControllers(ip))
                {
                    dc.Query();
                    if (!String.IsNullOrEmpty(filename))
                        dc.OutCsv(filename);
                }
            }
        }

        public static void DomainComputers(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainComputers dc = new DomainComputers(ip))
                {
                    dc.Query();
                    if (!String.IsNullOrEmpty(filename))
                        dc.OutCsv(filename);
                }
            }
            else
            {
                using (DomainComputers dc = new DomainComputers(ip, username, password))
                {
                    dc.Query();
                    if (!String.IsNullOrEmpty(filename))
                        dc.OutCsv(filename);
                }
            }
        }

        public static void DomainGroups(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainGroups dg = new DomainGroups(ip))
                {
                    dg.Query();
                    if (!String.IsNullOrEmpty(filename))
                        dg.OutCsv(filename);
                    dg.QueryGroupMembers();
                }
            }
            else
            {
                using (DomainGroups dg = new DomainGroups(ip, username, password))
                {
                    dg.Query();
                    if (!String.IsNullOrEmpty(filename))
                        dg.OutCsv(filename);
                    dg.QueryGroupMembers();
                }
            }
        }

        public static void DomainUsers(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainUsers du = new DomainUsers(ip))
                {
                    du.Query();
                    if (!String.IsNullOrEmpty(filename))
                        du.OutCsv(filename);
                    du.QueryAdminCount();
                    if (!String.IsNullOrEmpty(filename))
                        du.OutCsv(filename + "_protected.csv");
                }
            }
            else
            {
                using (DomainUsers du = new DomainUsers(ip, username, password))
                {
                    du.Query();
                    if (!String.IsNullOrEmpty(filename))
                        du.OutCsv(filename);
                    du.QueryAdminCount();
                    if (!String.IsNullOrEmpty(filename))
                        du.OutCsv(filename + "_protected.csv");
                }
            }
        }

        public static void KerberosPreauthentication(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (KerberosPreauthentication kp = new KerberosPreauthentication(ip))
                {
                    kp.Query();
                    if (!String.IsNullOrEmpty(filename))
                        kp.OutCsv(filename);
                    kp.QueryAdminCount();
                    if (!String.IsNullOrEmpty(filename))
                        kp.OutCsv(filename + "_protected.csv");
                }
            }
            else
            {
                using (KerberosPreauthentication kp = new KerberosPreauthentication(ip, username, password))
                {
                    kp.Query();
                    if (!String.IsNullOrEmpty(filename))
                        kp.OutCsv(filename);
                    kp.QueryAdminCount();
                    if (!String.IsNullOrEmpty(filename))
                        kp.OutCsv(filename + "_protected.csv");
                }
            }
        }

        public static void PasswordNeverExpires(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (PasswordNeverExpires pne = new PasswordNeverExpires(ip))
                {
                    pne.Query();
                    if (!String.IsNullOrEmpty(filename))
                        pne.OutCsv(filename);
                    pne.QueryProtectedUsers();
                    if (!String.IsNullOrEmpty(filename))
                        pne.OutCsv(filename + "_protected.csv");
                }
            }
            else
            {
                using (PasswordNeverExpires pne = new PasswordNeverExpires(ip, username, password))
                {
                    pne.Query();
                    if (!String.IsNullOrEmpty(filename))
                        pne.OutCsv(filename);
                    pne.QueryProtectedUsers();
                    if (!String.IsNullOrEmpty(filename))
                        pne.OutCsv(filename + "_protected.csv");
                }
            }
        }

        public static void PasswordNotRequired(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (PasswordNotRequired pnr = new PasswordNotRequired(ip))
                {
                    pnr.Query();
                    if (!String.IsNullOrEmpty(filename))
                        pnr.OutCsv(filename);
                    pnr.QueryProtectedUsers();
                    if (!String.IsNullOrEmpty(filename))
                        pnr.OutCsv(filename + "_protected.csv");
                }
            }
            else
            {
                using (PasswordNotRequired pnr = new PasswordNotRequired(ip, username, password))
                {
                    pnr.Query();
                    if (!String.IsNullOrEmpty(filename))
                        pnr.OutCsv(filename);
                    pnr.QueryProtectedUsers();
                    if (!String.IsNullOrEmpty(filename))
                        pnr.OutCsv(filename + "_protected.csv");
                }
            }
        }

        public static void ServicePrincipalName(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (ServicePrincipalName spn = new ServicePrincipalName(ip))
                {
                    spn.QueryUsers();
                    if (!String.IsNullOrEmpty(filename))
                        spn.OutCsv(filename);
                    spn.QueryComputers();
                    if (!String.IsNullOrEmpty(filename))
                        spn.OutCsv(filename + "_protected.csv");
                }
            }
            else
            {
                using (ServicePrincipalName spn = new ServicePrincipalName(ip, username, password))
                {
                    spn.QueryUsers();
                    if (!String.IsNullOrEmpty(filename))
                        spn.OutCsv(filename);
                    spn.QueryComputers();
                    if (!String.IsNullOrEmpty(filename))
                        spn.OutCsv(filename + "_protected.csv");
                }
            }
        }

        public static void LAPS(String ip, String domain, String username, String password, String filename)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (LAPS laps = new LAPS(ip))
                {
                    laps.Query();
                    if (!String.IsNullOrEmpty(filename))
                        laps.OutCsv(filename);
                }
            }
            else
            {
                using (LAPS laps = new LAPS(ip, username, password))
                {
                    laps.Query();
                    if (!String.IsNullOrEmpty(filename))
                        laps.OutCsv(filename);
                }
            }
        }

        public static void ComputerName()
        {
            Console.WriteLine(Environment.MachineName);
        }

        public static void DomainName()
        {
            Console.WriteLine(Environment.UserDomainName);
        }

        public static void AntivirusProduct()
        {
            using (WMI wmi = new WMI(".", @"root\SecurityCenter2"))
            {
                if (!wmi.Connect())
                {
                    Console.WriteLine("Unable to Connect");
                    return;
                }

                wmi.ExecuteQuery("Select * FROM AntivirusProduct");
            }
        }

        //This needs to be refined
        public static void OSInfo()
        {
            using (WMI wmi = new WMI("."))
            {
                if (!wmi.Connect())
                {
                    Console.WriteLine("Unable to Connect");
                    return;
                }

                wmi.ExecuteQuery("Select * FROM Win32_OperatingSystem");
            }
        }

        public static void MappedDrives()
        {
            using (WMI wmi = new WMI("."))
            {
                if (!wmi.Connect())
                {
                    Console.WriteLine("Unable to Connect");
                    return;
                }

                wmi.ExecuteQuery("Select * FROM Win32_MappedLogicalDisk");
            }
        }

        public static String Tasklist()
        {
            ManagementScope scope = new ManagementScope(@"\\.\root\cimv2");
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Process");
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection objectCollection = objectSearcher.Get();

            Dictionary<Int32, String> owners = new Dictionary<Int32, String>();
            foreach (ManagementObject managementObject in objectCollection)
            {
                String[] owner = new String[2];
                managementObject.InvokeMethod("GetOwner", (object[])owner);
                String name = owner[0] != null ? owner[1] + "\\" + owner[0] : "N/A";
                owners[Convert.ToInt32(managementObject["Handle"])] = name;
            }

            List<String[]> lines = new List<String[]>();
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process process in processes)
            {
                String architecture;
                try
                {
                    architecture = Misc.Is64BitProcess(process.Handle) ? "x64" : "x86";
                }
                catch (Exception)
                {
                    architecture = "N/A";
                }

                Int64 workingSet = (Int64)(process.WorkingSet64 / 1000000);
                String userName = "";
                try
                {
                    if (!owners.TryGetValue(process.Id, out userName))
                        userName = String.Empty;
                }
                catch (ArgumentNullException)
                {
                    userName = String.Empty;
                }

                lines.Add(new String[] { process.ProcessName, process.Id.ToString(), architecture, userName, Convert.ToString(workingSet) });
            }
            String[][] linesArray = lines.ToArray();

            //https://stackoverflow.com/questions/232395/how-do-i-sort-a-two-dimensional-array-in-c
            Comparer<Int32> comparer = Comparer<Int32>.Default;
            Array.Sort<String[]>(linesArray, (x, y) => comparer.Compare(Convert.ToInt32(x[1]), Convert.ToInt32(y[1])));

            List<String> sortedLines = new List<String>();
            String[] headerArray1 = { "ProcessName", "PID", "Arch", "UserName", "MemUsage" };
            String[] headerArray2 = { "-----------", "---", "----", "--------", "--------" };
            sortedLines.Add(String.Format("{0,-30} {1,-8} {2,-6} {3,-28} {4,8}", headerArray1));
            sortedLines.Add(String.Format("{0,-30} {1,-8} {2,-6} {3,-28} {4,8}", headerArray2));
            foreach (String[] line in linesArray)
                sortedLines.Add(String.Format("{0,-30} {1,-8} {2,-6} {3,-28} {4,8}", line));

            return String.Join("\n", sortedLines.ToArray());
        }
    }
}
