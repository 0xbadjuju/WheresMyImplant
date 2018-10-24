using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;

namespace WheresMyImplant
{
    class WMI : Base, IDisposable
    {
        private String scope = "\\\\.\\ROOT\\CIMV2";
        private ManagementScope managementScope;
        private ManagementObjectCollection results = null;

        internal WMI()
        {
        }

        internal WMI(String system)
        {
            scope = String.Format("\\\\{0}\\ROOT\\CIMV2", system);
        }

        internal WMI(String system, String wmiNamespace)
        {
            scope = String.Format("\\\\{0}\\{1}", system, wmiNamespace);
        }

        ~WMI()
        {
            Dispose();
        }

        internal ManagementObjectCollection GetResults()
        {
            return results;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Connect to the system specified by the management scope
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean Connect()
        {
            managementScope = new ManagementScope(scope);
            try
            {
                managementScope.Connect();
            }
            catch(Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
                
                return false;
            }

            if (!managementScope.IsConnected)
            {
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Connect to the system specified by the management scope with explict creds
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean Connect(String username, String password)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Username = username;
            options.Password = password;
            managementScope = new ManagementScope(scope, options);
            try
            {
                managementScope.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
                return false;
            }

            if (!managementScope.IsConnected)
            {
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Connect to the system specified by the management scope with explict creds
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean Connect(String username, System.Security.SecureString password)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Username = username;
            options.SecurePassword = password;
            managementScope = new ManagementScope(scope, options);
            try
            {
                managementScope.Connect();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
                return false;
            }

            if (!managementScope.IsConnected)
            {
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Executes a standard WMI method
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ExecuteMethod(String wmiClass, String method, Object[] args)
        {
            ManagementPath managementPath = new ManagementPath(wmiClass);
            ObjectGetOptions options = new ObjectGetOptions();
            try
            {
                ManagementClass managementClass = new ManagementClass(managementScope, managementPath, options);
                Object output = managementClass.InvokeMethod(method, args);
                Console.WriteLine("[+] Return Value: {0}", output);
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Executes a standard WMI method
        ////////////////////////////////////////////////////////////////////////////////
        internal Object ExecuteMethod2(String wmiClass, String method, Object[] args)
        {
            ManagementPath managementPath = new ManagementPath(wmiClass);
            ObjectGetOptions options = new ObjectGetOptions();
            Object output;
            try
            {
                ManagementClass managementClass = new ManagementClass(managementScope, managementPath, options);
                output = managementClass.InvokeMethod(method, args);
                Console.WriteLine("[+] Return Value: {0}", output);
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
                return null;
            }
            return output;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Instantiates a WMI class
        ////////////////////////////////////////////////////////////////////////////////
        internal ManagementObject CreateInstance(String wmiClass)
        {
            ManagementPath managementPath = new ManagementPath(wmiClass);
            ObjectGetOptions options = new ObjectGetOptions();
            ManagementObject managementInstance = null;
            try
            {
                ManagementClass managementClass = new ManagementClass(managementScope, managementPath, options);
                managementInstance = managementClass.CreateInstance();
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
            return managementInstance;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Executes a standard WMI query
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ExecuteQuery(String query)
        {
            try
            {
                ObjectQuery objectQuery = new ObjectQuery(query);
                ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(managementScope, objectQuery);
                results = managementObjectSearcher.Get();
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
                return false;
            }
            return true;
        }

        [Flags]
        public enum AccessMask
        {
            WBEM_ENABLE = 0x01,
            WBEM_METHOD_EXECUTE = 0x02,
            WBEM_FULL_WRITE_REP = 0x04,
            WBEM_PARTIAL_WRITE_REP = 0x8,
            WBEM_WRITE_PROVIDER = 0x10,
            WBEM_REMOTE_ACCESS = 0x20,
            WBEM_RIGHT_SUBSCRIBE = 0x40,
            WBEM_RIGHT_PUBLISH = 0x80,
            READ_CONTROL = 0x20000,
            WRITE_DAC = 0x40000
        }

        [Flags]
        public enum AceFlags
        {
            OBJECT_INHERIT_ACE_FLAG = 0x1,
            CONTAINER_INHERIT_ACE_FLAG = 0x2
        }
        
        [Flags]
        public enum AceType
        {
            ACCESS_ALLOWED_ACE_TYPE = 0x0,
            ACCESS_DENIED_ACE_TYPE = 0x1
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Get the Dynamic instances in WMI, e.g. Win32_Process, Win32_Services
        ////////////////////////////////////////////////////////////////////////////////
        internal void GetInstances(String path)
        {
            ManagementPath managementPath = new ManagementPath(path);
            ObjectGetOptions options = new ObjectGetOptions(null, TimeSpan.MaxValue, true);
            ManagementClass managementClass = new ManagementClass(managementScope, managementPath, options);
            results = managementClass.GetInstances();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Prints a management object collection
        ////////////////////////////////////////////////////////////////////////////////
        public void PrintResults()
        {
            List<String> properties = new List<String>();
            if (null == results || 0 == results.Count)
            {
                return;
            }

            Int32 length = 0;
            ManagementObject propertiesObject = results.OfType<ManagementObject>().FirstOrDefault();
            foreach (PropertyData property in propertiesObject.Properties)
            {
                properties.Add(property.Name);
                if (property.Name.Length > length)
                    length = property.Name.Length;
            }

            foreach (ManagementObject managementObject in results)
            {
                foreach (String property in properties)
                {
                    Console.WriteLine("{0,-" + length + "} {1}", property, managementObject[property]);
                }
                Console.WriteLine("");
            }
        }

        public void Dispose()
        {
            managementScope = null;
            GC.Collect();
        }
    }
}