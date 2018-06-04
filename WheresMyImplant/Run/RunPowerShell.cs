using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Text;


using System.Reflection;

namespace WheresMyImplant
{
    sealed class RunPowerShell : Base
    {
        //Because why not
        //https://github.com/jaredcatkinson/EvilNetConnectionWMIProvider/blob/master/EvilNetConnectionWMIProvider/EvilNetConnectionWMIProvider.cs
        internal RunPowerShell(string command)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);
            
            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(command);
            pipeline.Commands.Add("Out-String");
            Collection<PSObject> results = pipeline.Invoke();

            runspace.Close();
            
            foreach (PSObject obj in results)
            {
                WriteOutput(obj.ToString());
            }
        }
    }
}