using System;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;

namespace WheresMyImplant
{
    sealed class RunPowerShell : Base
    {
        //Because why not
        //https://github.com/jaredcatkinson/EvilNetConnectionWMIProvider/blob/master/EvilNetConnectionWMIProvider/EvilNetConnectionWMIProvider.cs
        internal RunPowerShell(string command)
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
    }
}