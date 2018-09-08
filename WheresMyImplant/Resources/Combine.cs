using System;
using System.IO;

namespace WheresMyImplant
{
    sealed class Combine
    {
        private Byte[] combined = new Byte[0];

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public Combine()
        {

        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public void Extend(Byte[] nextPart)
        {
            Int32 dwSize = combined.Length + nextPart.Length;
            using (MemoryStream memoryStream = new MemoryStream(new Byte[dwSize], 0, dwSize, true, true))
            {
                memoryStream.Write(combined, 0, combined.Length);
                memoryStream.Write(nextPart, 0, nextPart.Length);
                combined = memoryStream.GetBuffer();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public Byte[] Retrieve()
        {
            return combined;
        }
    }
}
