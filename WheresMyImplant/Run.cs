using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace WheresMyImplant
{
    public sealed class Run
    {
        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void RunCMD(string command, string parameters)
        {
            using (Process process = new Process())
            {
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.FileName = command;
                process.StartInfo.Arguments = parameters;
                process.Start();
                Console.WriteLine(process.StandardOutput.ReadToEnd());
                process.WaitForExit();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void RunPowerShell(string command)
        {
            using (Runspace runspace = RunspaceFactory.CreateRunspace())
            {
                runspace.Open();
                using (RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace))
                {
                    Pipeline pipeline = runspace.CreatePipeline();
                    pipeline.Commands.AddScript(command);
                    pipeline.Commands.Add("Out-String");
                    Collection<PSObject> results = pipeline.Invoke();

                    foreach (PSObject obj in results)
                    {
                        Console.WriteLine(obj.ToString());
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void RunXpCmdShell(string server, string database, string username, string password, string command)
        {
            using (RunXPCmdShell runXPCmdShell = new RunXPCmdShell(server, database, username, password))
            {
                runXPCmdShell.EnableXPCmdShell();
                runXPCmdShell.Execute(command);
            }
        }
    }
}
