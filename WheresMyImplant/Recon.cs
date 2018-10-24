using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;

using DomainInfo;

using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class Recon
    {
        public static void DomainControllers(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainControllers dc = new DomainControllers(ip, domain  + @"\" + username, password))
                {
                    dc.Query();
                    dc.Print();
                }
            }
            else
            {
                using (DomainControllers dc = new DomainControllers(ip))
                {
                    dc.Query();
                    dc.Print();
                }
            }
        }

        public static void DomainComputers(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainComputers dc = new DomainComputers(ip))
                {
                    dc.Query();
                    dc.Print();
                }
            }
            else
            {
                using (DomainComputers dc = new DomainComputers(ip, username, password))
                {
                    dc.Query();
                    dc.Print();
                }
            }
        }

        public static void DomainGroups(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainGroups dg = new DomainGroups(ip))
                {
                    dg.Query();
                    dg.Print();
                }
            }
            else
            {
                using (DomainGroups dg = new DomainGroups(ip, username, password))
                {
                    dg.Query();
                    dg.Print();
                }
            }
        }

        public static void DomainGroupMembers(String ip, String domain, String username, String password, String group)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainGroups dg = new DomainGroups(ip))
                {
                    dg.QueryGroupMembers(group);
                }
            }
            else
            {
                using (DomainGroups dg = new DomainGroups(ip, username, password))
                {
                    dg.QueryGroupMembers(group);
                }
            }
        }

        public static void DomainUsers(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainUsers du = new DomainUsers(ip))
                {
                    du.Query();
                }
            }
            else
            {
                using (DomainUsers du = new DomainUsers(ip, username, password))
                {
                    du.Query();
                }
            }
        }

        public static void DomainUserGroups(String ip, String domain, String username, String password, String user)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainUsers du = new DomainUsers(ip))
                {
                    du.QueryUserGroups(user);
                }
            }
            else
            {
                using (DomainUsers du = new DomainUsers(ip, username, password))
                {
                    du.QueryUserGroups(user);
                }
            }
        }

        public static void DomainProtectedUsers(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (DomainUsers du = new DomainUsers(ip))
                {
                    du.QueryAdminCount();
                    du.Print();
                }
            }
            else
            {
                using (DomainUsers du = new DomainUsers(ip, username, password))
                {
                    du.QueryAdminCount();
                    du.Print();
                }
            }
        }

        public static void KerberosPreauthentication(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (KerberosPreauthentication kp = new KerberosPreauthentication(ip))
                {
                    kp.Query();
                    kp.Print();
                }
            }
            else
            {
                using (KerberosPreauthentication kp = new KerberosPreauthentication(ip, username, password))
                {
                    kp.Query();
                    kp.Print();
                }
            }
        }

        public static void PasswordNeverExpires(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (PasswordNeverExpires pne = new PasswordNeverExpires(ip))
                {
                    pne.Query();
                    pne.Print();
                }
            }
            else
            {
                using (PasswordNeverExpires pne = new PasswordNeverExpires(ip, username, password))
                {
                    pne.Query();
                    pne.Print();
                }
            }
        }

        public static void PasswordNotRequired(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (PasswordNotRequired pnr = new PasswordNotRequired(ip))
                {
                    pnr.Query();
                    pnr.Print();
                }
            }
            else
            {
                using (PasswordNotRequired pnr = new PasswordNotRequired(ip, username, password))
                {
                    pnr.Query();
                    pnr.Print();
                }
            }
        }

        public static void ServicePrincipalName(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (ServicePrincipalName spn = new ServicePrincipalName(ip))
                {
                    spn.QueryUsers();
                    spn.Print();
                }
            }
            else
            {
                using (ServicePrincipalName spn = new ServicePrincipalName(ip, username, password))
                {
                    spn.QueryUsers();
                    spn.Print();
                }
            }
        }

        public static void LAPS(String ip, String domain, String username, String password)
        {
            if (String.IsNullOrEmpty(username))
            {
                using (LAPS laps = new LAPS(ip))
                {
                    laps.Query();
                    laps.Print();
                }
            }
            else
            {
                using (LAPS laps = new LAPS(ip, username, password))
                {
                    laps.Query();
                    laps.Print();
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

        public static void LogonServer()
        {
            Console.WriteLine(Environment.GetEnvironmentVariable("logonserver"));
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
                wmi.GetResults();
            }
        }

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
                ManagementObjectCollection results = wmi.GetResults();
                if (null == results)
                {
                    Console.WriteLine("WMI Query Failed");
                    return;
                }
                ManagementObject result = results.OfType<ManagementObject>().FirstOrDefault();
                Console.WriteLine("OS Information");
                Console.WriteLine("--------------");
                Console.WriteLine("{0} {1} ({2})", result["Caption"], result["OSArchitecture"], result["BuildNumber"]);
                Console.WriteLine("Computer Name    : {0}", result["CSName"]);
                Console.WriteLine("Free Memory      : {0}/{1}", result["FreeVirtualMemory"], result["TotalVirtualMemorySize"]);
                Console.WriteLine("Country & Locale : {0} - {1}", result["CountryCode"], result["Locale"]);
                Console.WriteLine("System Device    : {0}", result["SystemDevice"]);
                Console.WriteLine("BitLocker Level  : {0}", result["EncryptionLevel"]);
                Console.WriteLine("InstallDate      : {0}", result["InstallDate"]);
                Console.WriteLine("LastBootUpTime   : {0}", result["LastBootUpTime"]);
                Console.WriteLine("LocalDateTime    : {0}", result["LocalDateTime"]);
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
                ManagementObjectCollection results = wmi.GetResults();
                if (null == results)
                {
                    Console.WriteLine("WMI Query Failed");
                    return;
                }
                try
                {
                    Console.WriteLine("{0,-9}  {1,-4}  {2,-15}  {3,-10} {4}", "Device ID", "Name", "VolumeName", "FileSystem", "FreeSpace");
                    Console.WriteLine("{0,-9}  {1,-4}  {2,-15}  {3,-10} {4}", "---------", "----", "----------", "----------", "---------");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
                
                foreach (ManagementObject result in results)
                {
                    try
                    {
                        Console.WriteLine("{0,-9}  {1,-4}  {2,-15}  {3,-10} {4}/{5} M     {6}", 
                            result["DeviceID"], 
                            result["Name"], 
                            result["VolumeName"], 
                            result["FileSystem"], 
                            (UInt64)result["FreeSpace"] / 1048576,
                            (UInt64)result["Size"] / 1048576, 
                            result["ProviderName"]);
                    }
                    catch(Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                }
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
