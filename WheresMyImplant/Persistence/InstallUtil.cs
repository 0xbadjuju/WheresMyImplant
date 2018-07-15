using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Configuration.Install;
using System.Management;
using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.InteropServices;

[assembly: WmiConfiguration(@"root\cimv2", HostingModel = ManagementHostingModel.LocalSystem), AssemblyKeyFileAttribute("sgKey.snk")]
namespace WheresMyImplant
{
    class InstallUtil
    {
        [System.ComponentModel.RunInstaller(true)]
        public class MyInstall : DefaultManagementInstaller
        {
            public override void Install(IDictionary stateSaver)
            {
                try
                {
                    new System.EnterpriseServices.Internal.Publish().GacInstall("WhereMyImplant.dll");
                    base.Install(stateSaver);
                    RegistrationServices registrationServices = new RegistrationServices();
                }
                catch (Exception error)
                {
                    Console.WriteLine(error.ToString());
                }
            }

            public override void Uninstall(IDictionary savedState)
            {
                try
                {
                    new System.EnterpriseServices.Internal.Publish().GacRemove("WhereMyImplant.dll");
                    ManagementClass managementClass = new ManagementClass(@"root\cimv2:Win32_Implant");
                    managementClass.Delete();
                }
                catch { }

                try
                {
                    base.Uninstall(savedState);
                }
                catch (Exception error)
                {
                    Console.WriteLine(error.ToString());
                }
            }
        }
    }
}