using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WheresMyImplant
{
    class SMBTreeDisconnectRequest
    {
        private readonly Byte[] WordCount = { 0x00 };
        private readonly Byte[] ByteCount = { 0x00, 0x00 };

        internal SMBTreeDisconnectRequest()
        {

        }

        internal Byte[] GetRequest()
        {
            return Misc.Combine(WordCount, ByteCount);
        }
    }
}
