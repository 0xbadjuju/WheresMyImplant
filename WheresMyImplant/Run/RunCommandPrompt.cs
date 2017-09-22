using System;
using System.Diagnostics;
using System.Linq;
using System.Text;


using System.Reflection;

namespace WheresMyImplant
{
    class RunCommandPrompt : Base
    {
        private StringBuilder stringBuilder = new StringBuilder();

        public RunCommandPrompt(string command, string parameters)
        {
            Process process = new Process();
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.FileName = command;
            process.StartInfo.Arguments = parameters;
            process.Start();
            WriteOutput(process.StandardOutput.ReadToEnd());
            process.WaitForExit();
        }
    }
}