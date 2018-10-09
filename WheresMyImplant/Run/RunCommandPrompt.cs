using System;
using System.Diagnostics;
using System.Linq;
using System.Text;


using System.Reflection;

namespace WheresMyImplant
{
    class RunCommandPrompt : Base
    {
        internal RunCommandPrompt(String command, String parameters)
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = parameters;
            process.Start();
            Console.WriteLine(process.StandardOutput.ReadToEnd());
            process.WaitForExit();
        }
    }
}