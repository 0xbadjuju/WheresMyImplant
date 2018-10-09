using System;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    public sealed class Misc
    {
        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void GetFileBytes(String filePath, String base64)
        {
            if (String.Empty == base64)
                base64 = "false";

            if (!Boolean.TryParse(base64, out Boolean bBase64))
            {
                Console.WriteLine("Unable to parse wait parameter (true, false)");
                return;
            }

            Byte[] fileBytes;
            using (System.IO.FileStream fileStream = new System.IO.FileStream(System.IO.Path.GetFullPath(filePath), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                using (System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(fileStream))
                {
                    fileBytes = new Byte[binaryReader.BaseStream.Length];
                    binaryReader.Read(fileBytes, 0, (Int32)binaryReader.BaseStream.Length);
                }
            }

            String strBytes = "0x" + BitConverter.ToString(fileBytes).Replace("-", ",0x");
            if (bBase64)
            {
                Convert.ToBase64String(fileBytes);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void GenerateNTLMString(String password)
        {
            try
            {
                Byte[] bPassword = Encoding.Unicode.GetBytes(password);
                Org.BouncyCastle.Crypto.Digests.MD4Digest md4Digest = new Org.BouncyCastle.Crypto.Digests.MD4Digest();
                md4Digest.BlockUpdate(bPassword, 0, bPassword.Length);
                Byte[] result = new Byte[md4Digest.GetDigestSize()];
                md4Digest.DoFinal(result, 0);
                Console.WriteLine(BitConverter.ToString(result).Replace("-", ""));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] Unhandled Exception Occured");
                Console.WriteLine("[-] {0}", ex.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] RC4Encrypt(byte[] RC4Key, byte[] data)
        {
            byte[] output = new byte[data.Length];
            byte[] s = new byte[256];
            for (Int32 x = 0; x < 256; x++)
            {
                s[x] = Convert.ToByte(x);
            }

            Int32 j = 0;
            for (Int32 x = 0; x < 256; x++)
            {
                j = (j + s[x] + RC4Key[x % RC4Key.Length]) % 256;
                byte hold = s[x];
                s[x] = s[j];
                s[j] = hold;
            }
            Int32 i = j = 0;

            //run this in order in a for loop
            int k = 0;
            foreach (byte entry in data)
            {
                i = (i + 1) % 256;
                j = (j + s[i]) % 256;
                byte hold = s[i];
                s[i] = s[j];
                s[j] = hold;

                output[k++] = Convert.ToByte(entry ^ s[(s[i] + s[j]) % 256]);
            }
            return output;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads the Imagef File Headers from the Image Base Address
        ////////////////////////////////////////////////////////////////////////////////
        public static Boolean Is64BitProcess(IntPtr hProcess)
        {
            kernel32.GetNativeSystemInfo(out Winbase._SYSTEM_INFO systemInfo);
            if (Winbase.INFO_PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_INTEL == systemInfo.wProcessorArchitecture)
            {
                return false;
            }
            else if (Winbase.INFO_PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_AMD64 == systemInfo.wProcessorArchitecture)
            {
                if (!kernel32.IsWow64Process(hProcess, out Boolean isWOW64))
                {
                    throw new Exception("IsWow64Process");
                }
                return !isWOW64;
            }
            else
            {
                throw new Exception("INFO_PROCESSOR_ARCHITECTURE");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static double GetOSVersion()
        {
            double version = 0.0;
            System.OperatingSystem system = Environment.OSVersion;
            String versionString = String.Format("{0}.{1}", system.Version.Major, system.Version.Minor);
            double.TryParse(versionString, out version);
            return version;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static string Base64Decode(String encodedString)
        {
            byte[] data = Convert.FromBase64String(encodedString);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c
        ////////////////////////////////////////////////////////////////////////////////
        internal static String GenerateUuid(int length)
        {
            Random random = new Random();
            const String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new String(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static void PrintStruct<T>(T imageDosHeader)
        {
            System.Reflection.FieldInfo[] fields = imageDosHeader.GetType().GetFields();
            Console.WriteLine("==========");
            foreach (var xInfo in fields)
            {
                Console.WriteLine("Field {0,-20}", xInfo.GetValue(imageDosHeader).ToString());
            }
            Console.WriteLine("==========");
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static string GetError()
        {
            return new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error()).Message;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static Byte[] QueryWMIFS(String wmiClass, String fileName)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Impersonation = System.Management.ImpersonationLevel.Impersonate;
            ManagementScope scope = new ManagementScope(@"\\.\root\cimv2", options);
            scope.Connect();

            String query = String.Format("SELECT Index FROM {0} WHERE FileName = \'{1}\'", wmiClass, fileName);
            ObjectQuery queryIndexCount = new ObjectQuery(query);
            ManagementObjectSearcher searcherIndexCount = new ManagementObjectSearcher(scope, queryIndexCount);
            ManagementObjectCollection queryIndexCollection = searcherIndexCount.Get();
            Int32 indexCount = queryIndexCollection.Count;

            String EncodedText = "";
            for (Int32 i = 0; i < indexCount; i++)
            {
                String query2 = String.Format("SELECT FileStore FROM {0} WHERE FileName = \'{1}\' AND Index = \'{1}\'", wmiClass, fileName, i);
                ObjectQuery queryFilePart = new ObjectQuery();
                ManagementObjectSearcher searcherFilePart = new ManagementObjectSearcher(scope, queryFilePart);
                ManagementObjectCollection queryCollection = searcherFilePart.Get();
                foreach (ManagementObject filePart in queryCollection)
                {
                    EncodedText += filePart["FileStore"].ToString();
                }
            }
            return System.Convert.FromBase64String(EncodedText);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static Byte[] QueryWMIFS(String wmiClass, String fileName, String system, String username, String password)
        {
            ConnectionOptions options = new ConnectionOptions();
            options.Username = username;
            options.Password = password;
            options.Impersonation = System.Management.ImpersonationLevel.Impersonate;
            ManagementScope scope = new ManagementScope(@"\\.\root\cimv2", options);
            scope.Connect();

            String query = String.Format("SELECT Index FROM {0} WHERE FileName = \'{1}\'", wmiClass, fileName);
            ObjectQuery queryIndexCount = new ObjectQuery(query);
            ManagementObjectSearcher searcherIndexCount = new ManagementObjectSearcher(scope, queryIndexCount);
            ManagementObjectCollection queryIndexCollection = searcherIndexCount.Get();
            Int32 indexCount = queryIndexCollection.Count;

            String EncodedText = "";
            for (Int32 i = 0; i < indexCount; i++)
            {
                String query2 = String.Format("SELECT FileStore FROM {0} WHERE FileName = \'{1}\' AND Index = \'{1}\'", wmiClass, fileName, i);
                ObjectQuery queryFilePart = new ObjectQuery();
                ManagementObjectSearcher searcherFilePart = new ManagementObjectSearcher(scope, queryFilePart);
                ManagementObjectCollection queryCollection = searcherFilePart.Get();
                foreach (ManagementObject filePart in queryCollection)
                {
                    EncodedText += filePart["FileStore"].ToString();
                }
            }
            return System.Convert.FromBase64String(EncodedText);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal static IntPtr GetModuleAddress(String module, UInt32 processId, ref UInt32 dwSize)
        {
            IntPtr hModule = IntPtr.Zero;
            IntPtr hSnapshot = IntPtr.Zero;
            try
            {
                hSnapshot = kernel32.CreateToolhelp32Snapshot(TiHelp32.TH32CS_SNAPMODULE | TiHelp32.TH32CS_SNAPMODULE32, processId);
                if (IntPtr.Zero == hSnapshot)
                {
                    return hModule;
                }

                TiHelp32.tagMODULEENTRY32 moduleEntry = new TiHelp32.tagMODULEENTRY32();
                moduleEntry.dwSize = (UInt32)Marshal.SizeOf(moduleEntry);
                if (!kernel32.Module32First(hSnapshot, ref moduleEntry))
                {
                    return hModule;
                }

                do
                {
                    if (moduleEntry.szModule == module)
                    {
                        hModule = moduleEntry.modBaseAddr;
                        dwSize = moduleEntry.modBaseSize;
                    }

                    moduleEntry = new TiHelp32.tagMODULEENTRY32();
                    moduleEntry.dwSize = (UInt32)Marshal.SizeOf(moduleEntry);
                }
                while (kernel32.Module32Next(hSnapshot, ref moduleEntry));
            }
            catch (Exception)
            {
                return hModule;
            }
            finally
            {
                kernel32.CloseHandle(hSnapshot);
            }
            return hModule;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal static UInt32 GetMainThread(UInt32 processId)
        {
            IntPtr hThread = IntPtr.Zero;
            IntPtr hSnapshot = IntPtr.Zero;

            try
            {
                hSnapshot = kernel32.CreateToolhelp32Snapshot(TiHelp32.TH32CS_SNAPTHREAD, processId);
                if (IntPtr.Zero == hSnapshot)
                {
                    return 0;
                }

                TiHelp32.tagTHREADENTRY32 threadEntry = new TiHelp32.tagTHREADENTRY32();
                threadEntry.dwSize = (UInt32)Marshal.SizeOf(threadEntry);
                if (!kernel32.Thread32First(hSnapshot, ref threadEntry))
                {
                    return 0;
                }

                do
                {
                    Console.WriteLine(threadEntry.th32ThreadID);
                    hThread = kernel32.OpenThread(ntpsapi.PROCESS_ALL_ACCESS, false, threadEntry.th32ThreadID);
                    threadEntry = new TiHelp32.tagTHREADENTRY32();
                    threadEntry.dwSize = (UInt32)Marshal.SizeOf(threadEntry);
                }
                while (kernel32.Thread32Next(hSnapshot, ref threadEntry));
            }
            catch (Exception)
            {
                return 0;
            }
            finally
            {
                kernel32.CloseHandle(hSnapshot);
            }
            return 0;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal static Byte[] GenerateNTLM(String password)
        {
            Byte[] bPassword = Encoding.Unicode.GetBytes(password);
            Org.BouncyCastle.Crypto.Digests.MD4Digest md4Digest = new Org.BouncyCastle.Crypto.Digests.MD4Digest();
            md4Digest.BlockUpdate(bPassword, 0, bPassword.Length);
            Byte[] result = new Byte[md4Digest.GetDigestSize()];
            md4Digest.DoFinal(result, 0);
            return result;
        }
    }
}