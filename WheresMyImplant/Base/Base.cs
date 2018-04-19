using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace WheresMyImplant
{
    public class Base
    {
        protected StringBuilder stringBuilder = new StringBuilder();

        protected void WriteOutput(string output)
        {
            stringBuilder.Append(output + "\n");
        }

        protected void WriteOutputGood(string output)
        {
            stringBuilder.Append(String.Format("[+] {0}\n", output));
        }

        protected void WriteOutputNeutral(string output)
        {
            stringBuilder.Append(String.Format("[*] {0}\n", output));
        }

        protected void WriteOutputBad(string output)
        {
            stringBuilder.Append(String.Format("[-] {0} 0x{1:X}\n", output, Marshal.GetLastWin32Error()));
            string errorMessage = new Win32Exception(Marshal.GetLastWin32Error()).Message;
            stringBuilder.Append("[-] " + errorMessage + "\n");
        }

        public string GetOutput()
        {
            return stringBuilder.ToString().Trim();
        }
    }
}
