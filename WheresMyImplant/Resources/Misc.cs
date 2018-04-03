using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    public class Misc
    {
        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static byte[] Combine(byte[] byte1, byte[] byte2)
        {
            Int32 dwSize = byte1.Length + byte2.Length;
            MemoryStream memoryStream = new MemoryStream(new byte[dwSize], 0, dwSize, true, true);
            memoryStream.Write(byte1, 0, byte1.Length);
            memoryStream.Write(byte2, 0, byte2.Length);
            byte[] combinedBytes = memoryStream.GetBuffer();
            return combinedBytes;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static byte[] RC4Encrypt(byte[] RC4Key, byte[] data)
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
        //
        ////////////////////////////////////////////////////////////////////////////////
        public static Boolean Is64BitOs()
        {
            if (Directory.Exists(@"C:\Program Files (x86)\"))
            {
                Console.WriteLine("[*] 64 Bit OS");
                return true;
            }
            else
            {
                Console.WriteLine("[*] 32 Bit OS");
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public static double GetOSVersion()
        {
            double version = 0.0;
            System.OperatingSystem system = Environment.OSVersion;
            String versionString = String.Format("{0}.{1}", system.Version.Major, system.Version.Minor);
            double.TryParse(versionString, out version);
            return version;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Decrypts an input string via the DPAPI
        ////////////////////////////////////////////////////////////////////////////////
        public static String DPAPIDecrypt(String input)
        {
            Byte[] outputBytes = new Byte[0];
            Byte[] inputBytes = System.Text.Encoding.Unicode.GetBytes(input);
            IntPtr lpBuffer = Marshal.AllocHGlobal(inputBytes.Length);
            Marshal.Copy(inputBytes, 0, lpBuffer, inputBytes.Length);

            Wincrypt._CRYPTOAPI_BLOB pDataIn = new Wincrypt._CRYPTOAPI_BLOB();
            pDataIn.cbData = (UInt32)inputBytes.Length;
            pDataIn.pbData = lpBuffer;

            Wincrypt._CRYPTOAPI_BLOB pOptionalEntropy = new Wincrypt._CRYPTOAPI_BLOB();

            Wincrypt._CRYPTPROTECT_PROMPTSTRUCT pPromptStruct = new Wincrypt._CRYPTPROTECT_PROMPTSTRUCT();
            pPromptStruct.cbSize = (UInt32)Marshal.SizeOf(typeof(Wincrypt._CRYPTPROTECT_PROMPTSTRUCT));
            pPromptStruct.dwPromptFlags = 0;
            pPromptStruct.hwndApp = IntPtr.Zero;
            pPromptStruct.szPrompt = String.Empty;

            Wincrypt._CRYPTOAPI_BLOB pDataOut = new Wincrypt._CRYPTOAPI_BLOB();
            if (crypt32.CryptUnprotectData(ref pDataIn, null, ref pOptionalEntropy, IntPtr.Zero, ref pPromptStruct, 0, ref pDataOut))
            {
                outputBytes = new Byte[pDataOut.cbData];
                Marshal.Copy(pDataOut.pbData, outputBytes, 0, (Int32)pDataOut.cbData);
            }
            else
            {
                Console.WriteLine(Marshal.GetLastWin32Error());
            }
            Marshal.FreeHGlobal(lpBuffer);
            return System.Text.Encoding.Unicode.GetString(outputBytes);
        }
    }
}