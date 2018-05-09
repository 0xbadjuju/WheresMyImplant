using System;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

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

        public static string Base64Decode(String encodedString)
        {
            byte[] data = Convert.FromBase64String(encodedString);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // https://stackoverflow.com/questions/1344221/how-can-i-generate-random-alphanumeric-strings-in-c
        ////////////////////////////////////////////////////////////////////////////////
        internal String GenerateUuid(int length)
        {
            Random random = new Random();
            const String chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new String(Enumerable.Repeat(chars, length).Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static void PrintStruct<T>(T imageDosHeader)
        {
            System.Reflection.FieldInfo[] fields = imageDosHeader.GetType().GetFields();
            Console.WriteLine("==========");
            foreach (var xInfo in fields)
            {
                Console.WriteLine("Field {0,-20}", xInfo.GetValue(imageDosHeader).ToString());
            }
            Console.WriteLine("==========");
        }
    }
}