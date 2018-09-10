using System;
using System.Linq;
using System.Security.Cryptography;

namespace WheresMyImplant
{
    class NTLMSSPAuth
    {
        private readonly Byte[]ASNID = { 0xa1, 0x82 };
        private Byte[]ASNLength;
        private readonly Byte[]ASNID2 = { 0x30, 0x82 };
        private Byte[]ASNLength2;
        private readonly Byte[]ASNID3 = { 0xa2, 0x82 };
        private Byte[]ASNLength3;
        private readonly Byte[]NTLMSSPID = { 0x04, 0x82};
        private Byte[]NTLMSSPLength;
        private Byte[]NTLMResponse;

        internal NTLMSSPAuth()
        {
        }

        internal void SetNetNTLMResponse(Byte[] netNTLMResponse)
        {
            this.NTLMResponse = netNTLMResponse;
            NTLMSSPLength = BitConverter.GetBytes(netNTLMResponse.Length).Take(2).ToArray();
            Array.Reverse(NTLMSSPLength);

            ASNLength = BitConverter.GetBytes(netNTLMResponse.Length + 12).Take(2).ToArray();
            Array.Reverse(ASNLength);

            ASNLength2 = BitConverter.GetBytes(netNTLMResponse.Length + 8).Take(2).ToArray();
            Array.Reverse(ASNLength2);

            ASNLength3 = BitConverter.GetBytes(netNTLMResponse.Length + 4).Take(2).ToArray();
            Array.Reverse(ASNLength3);
        }

        internal Byte[] GetNTLMSSPAuth()
        {
            Byte[] request = Misc.Combine(ASNID, ASNLength);
            request = Misc.Combine(request, ASNID2);
            request = Misc.Combine(request, ASNLength2);
            request = Misc.Combine(request, ASNID3);
            request = Misc.Combine(request, ASNLength3);
            request = Misc.Combine(request, NTLMSSPID);
            request = Misc.Combine(request, NTLMSSPLength);
            request = Misc.Combine(request, NTLMResponse);
            return request;
        }
    }
}