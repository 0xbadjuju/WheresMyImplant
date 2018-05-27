using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Text;
using System.ServiceProcess;

namespace WheresMyImplant
{
    class Services : Base
    {
        private ServiceController service;
        private String serviceName;
        private UInt32 ProcessId;

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Services(String serviceName)
        {
            this.serviceName = serviceName;
            service = new ServiceController(serviceName);
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean StartService()
        {
            WriteOutputNeutral("Starting Service " + serviceName);
            if (service.Status == ServiceControllerStatus.Running)
            {
                return true;
            }

            service.Start();
            while (service.Status == ServiceControllerStatus.StartPending || service.Status == ServiceControllerStatus.Stopped)
            {
                System.Threading.Thread.Sleep(1000);
                WriteOutputNeutral("*");
                service.Refresh();
            }

            if (service.Status == ServiceControllerStatus.Running)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean StopService()
        {
            WriteOutputGood("Stopping Service " + serviceName);
            if (service.CanStop)
            {
                service.Stop();
                while (service.Status == ServiceControllerStatus.StopPending)
                {
                    System.Threading.Thread.Sleep(1000);
                    WriteOutputNeutral("-");
                    service.Refresh();
                }

                if (service.Status == ServiceControllerStatus.Stopped)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (service.CanPauseAndContinue)
            {
                service.Pause();
                while (service.Status == ServiceControllerStatus.PausePending)
                {
                    System.Threading.Thread.Sleep(1000);
                    WriteOutputNeutral("*");
                    service.Refresh();
                }

                if (service.Status == ServiceControllerStatus.Paused)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                WriteOutputBad("Unable to stop service");
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal UInt32 GetServiceProcessId()
        {
            List<ManagementObject> systemProcesses = new List<ManagementObject>();
            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
            scope.Connect();
            if (!scope.IsConnected)
            {
                WriteOutputBad("Failed to connect to WMI");
            }

            WriteOutputNeutral("Querying for service: " + serviceName);
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Service WHERE Name = \'" + serviceName + "\'");
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection objectCollection = objectSearcher.Get();
            if (objectCollection == null)
            {
                WriteOutputNeutral("ManagementObjectCollection");
            }
            foreach (ManagementObject managementObject in objectCollection)
            {
                ProcessId = (UInt32)managementObject["ProcessId"];
            }
            WriteOutputGood("Returned PID: " + ProcessId);
            return ProcessId;
        }
    }
}
