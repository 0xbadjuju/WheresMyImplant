using System;
using System.Linq;
using System.Security.Principal;

namespace WheresMyImplant
{
    class CheckPrivileges : Base
    {            
        public Boolean croak = false;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void GetSystem()
        {
            if (!WindowsIdentity.GetCurrent().IsSystem)
            {
                WriteOutputBad("Not running as SYSTEM, checking for Administrator access.");
                if ((new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    WriteOutputBad("Not running as Administrator, Exiting.");
                    croak = true;
                }
                else
                {
                    new Tokens().GetSystem();
                    if (!WindowsIdentity.GetCurrent().IsSystem)
                    {
                        WriteOutputBad("GetSystem Failed");
                        croak = true;
                        return;
                    }
                }
            }
            WriteOutputGood("Running as SYSTEM");
        }
    }
}