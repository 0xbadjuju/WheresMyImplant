using System;
using System.IO;

namespace WheresMyImplant
{
    public class Misc
    {
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
    }
}