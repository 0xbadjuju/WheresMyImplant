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

        WMI()
        {
        }

        WMI(String system)
        {
            scope = String.Format("\\\\{0}\\ROOT\\CIMV2", system);
        }

        WMI(String system, String wmiNamespace)
        {
            scope = String.Format("\\\\{0}\\{1}", system, wmiNamespace);
        }

        ~WMI()
        {
            Dispose();
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
            catch(Exception error)
            {
                WriteOutputBad(error.ToString());
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
            catch (Exception error)
            {
                WriteOutputBad(error.ToString());
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
            catch (Exception error)
            {
                WriteOutputBad(error.ToString());
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
            try
            {
                ManagementClass managementClass = new ManagementClass(wmiClass);
                Object output = managementClass.InvokeMethod(method, args);
                WriteOutputGood(String.Format("Return Value: {0}", output));
            }
            catch (ManagementException error)
            {
                WriteOutputBad(error.ToString());
                return false;
            }
            return true;
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
                PrintResults(managementObjectSearcher.Get());
            }
            catch (ManagementException error)
            {
                WriteOutputBad(error.ToString());
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Get the Dynamic instances in WMI, e.g. Win32_Process, Win32_Services
        ////////////////////////////////////////////////////////////////////////////////
        internal void GetInstances(String path)
        {
            ManagementPath managementPath = new ManagementPath(path);
            ObjectGetOptions options = new ObjectGetOptions(null, TimeSpan.MaxValue, true);
            ManagementClass managementClass = new ManagementClass(managementScope, managementPath, options);
            PrintResults(managementClass.GetInstances());
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Prints a management object collection
        ////////////////////////////////////////////////////////////////////////////////
        private void PrintResults(ManagementObjectCollection results)
        {
            List<String> properties = new List<String>();
            if (null == results || 0 == results.Count)
            {
                return;
            }

            ManagementObject propertiesObject = results.OfType<ManagementObject>().FirstOrDefault();
            foreach (PropertyData property in propertiesObject.Properties)
            {
                properties.Add(property.Name);
            }

            foreach (ManagementObject managementObject in results)
            {
                StringBuilder output = new StringBuilder();
                foreach (String property in properties)
                {
                    output.Append(String.Format("{0,-10}", managementObject[property]));
                }
                WriteOutput(output.ToString());
            }
        }

        public void Dispose()
        {
            managementScope = null;
            GC.Collect();
        }
    }
}