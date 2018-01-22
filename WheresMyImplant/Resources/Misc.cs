using System;
using System.IO;

namespace WheresMyImplant
{
    public class Misc
    {
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public static byte[] combine(byte[] byte1, byte[] byte2)
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
        public static byte[] rc4Encrypt(byte[] RC4Key, byte[] data)
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
                /*
                s[x] ^= s[j];
                s[j] ^= s[x];
                s[x] ^= s[j];
                */
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
                /*
                 * if i = j - very bad juju
                s[i] ^= s[j];
                s[j] ^= s[i];
                s[i] ^= s[j];
                */
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
        public static Boolean is64BitOs()
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
    }
}