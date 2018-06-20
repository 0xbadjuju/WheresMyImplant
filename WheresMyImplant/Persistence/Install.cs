using System;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    class Install
    {
        private ManagementScope managementScope;
        private ManagementClass managementClass;

        private String runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
        private Assembly assembly = Assembly.GetExecutingAssembly();
        private AssemblyName systemManagementName;

        internal Install(String system, String namespaceName, String providerDisplayName)
        {
            systemManagementName = AssemblyName.GetAssemblyName(String.Format("{0}{1}", runtimePath, "System.Management.dll"));

            ConnectionOptions connectionOptions = new ConnectionOptions();
            connectionOptions.Impersonation = ImpersonationLevel.Impersonate;

            String connectionString = String.Format(@"\\{0}\{1}", system, namespaceName);
            managementScope = new ManagementScope(connectionString, connectionOptions);

            ManagementPath managementPath = new ManagementPath();
            ObjectGetOptions options = new ObjectGetOptions();
            managementClass = new ManagementClass(managementScope, managementPath, options);

            managementClass["__class"] = providerDisplayName;
            managementClass.Qualifiers.Add("dynamic", true, false, true, false, true);
            managementClass.Qualifiers.Add("provider", assembly.FullName, false, false, false, true);
        }

        internal void Connect()
        {
            managementScope.Connect();
        }

        internal void GetMethods()
        {
            
            Type className = assembly.GetTypes()
                .Where(t => t.Namespace == "WheresMyImplant").Distinct()
                .Where(t => t.Name == "Implant").First();

            Console.WriteLine(className.Name);

            foreach (MethodInfo methodInfo in className.GetMethods())
            {
                String methodName = methodInfo.Name;
                if (methodName == "GetType" | 
                    methodName == "Equals" | 
                    methodName == "ToString" | 
                    methodName == "GetHashCode" | 
                    methodName == "Install")
                {
                    continue;
                }
                Console.WriteLine(methodName);
                ParameterInfo[] inputParameterInfo = methodInfo.GetParameters();

                ManagementClass inProperties = NewParameter("In");

                Int32 i = 0;
                foreach (ParameterInfo info in inputParameterInfo)
                {
                    inProperties.Properties.Add(info.Name, CimType.String, false);
                    inProperties.Properties[info.Name].Qualifiers.Add("ID", i);
                    Console.WriteLine("\t{0}", info.Name);
                    i++;
                }
                
                ManagementClass outProperties = NewParameter("Out");
                outProperties.Properties.Add("ReturnValue", CimType.String, false);
                outProperties.Properties["ReturnValue"].Qualifiers.Add("Out", true);

                


                IntPtr inPtr = (IntPtr)inProperties;
                IntPtr outPtr = (IntPtr)outProperties;

                managementClass.Methods.Add(methodName, NewManagementBaseObject(inPtr), NewManagementBaseObject(outPtr));
                managementClass.Methods[methodName].Qualifiers.Add("Static", true);
                managementClass.Methods[methodName].Qualifiers.Add("Implemented", true);
            }
            managementClass.Put();
        }

        private ManagementBaseObject NewManagementBaseObject(IntPtr tempPtr)
        {
            Type IWbemClassObjectFreeThreaded = Type.GetType("System.Management.IWbemClassObjectFreeThreaded, " + systemManagementName.FullName);
            ConstructorInfo IWbemClassObjectFreeThreaded_ctor = IWbemClassObjectFreeThreaded.GetConstructors().First();
            Object IWbemClassObjectFreeThreadedInstance = IWbemClassObjectFreeThreaded_ctor.Invoke(new Object[] { tempPtr });

            Type ManagementBaseObject = Type.GetType("System.Management.ManagementBaseObject, " + systemManagementName.FullName);
            ConstructorInfo[] ManagementBaseObject_ctor = ManagementBaseObject.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance);
            return (ManagementBaseObject)ManagementBaseObject_ctor.Last().Invoke(new Object[] { IWbemClassObjectFreeThreadedInstance });
        }

        private ManagementClass NewParameter(String direction)
        {
            ObjectGetOptions options = new ObjectGetOptions();
            ManagementClass __PARAMETERS = new ManagementClass(@"ROOT\cimv2", "__PARAMETERS", null);
            ManagementClass parameters = (ManagementClass)__PARAMETERS.Clone();
            parameters.Qualifiers.Add(direction, true);
            return parameters;
        }
    }
}