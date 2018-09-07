using System;
using System.Text;

namespace WheresMyImplant
{
    class SVCCTLSCMOpenSCManagerW
    {
        private Byte[] MachineName_ReferentID;
        private Byte[] MachineName_MaxCount;
        private readonly Byte[] MachineName_Offset = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] MachineName_ActualCount;
        private Byte[] MachineName;
        private Byte[] Database_ReferentID;
        private readonly Byte[] Database_NameMaxCount = { 0x0f, 0x00, 0x00, 0x00 };
        private readonly Byte[] Database_NameOffset = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Database_NameActualCount = { 0x0f, 0x00, 0x00, 0x00 };
        private readonly Byte[] Database = { 0x53, 0x00, 0x65, 0x00, 0x72, 0x00, 0x76, 0x00, 0x69, 0x00, 0x63, 0x00, 0x65, 0x00, 0x73, 0x00, 0x41, 0x00, 0x63, 0x00, 0x74, 0x00, 0x69, 0x00, 0x76, 0x00, 0x65, 0x00, 0x00, 0x00 };
        private readonly Byte[] Unknown = { 0xbf, 0xbf };
        private readonly Byte[] AccessMask = { 0x3f, 0x00, 0x00, 0x00 };

        internal SVCCTLSCMOpenSCManagerW()
        {
            String strMachineName = Misc.GenerateUuidAlpha(16);
            MachineName = Encoding.Unicode.GetBytes(strMachineName);

            if (0 == MachineName.Length % 2)
                MachineName = Misc.Combine(MachineName, new Byte[] { 0x00, 0x00 });
            else
                MachineName = Misc.Combine(MachineName, new Byte[] { 0x00, 0x00, 0x00, 0x00 });

            MachineName_ActualCount = MachineName_MaxCount = BitConverter.GetBytes(strMachineName.Length + 1);

            MachineName_ReferentID = Misc.Combine(BitConverter.GetBytes(Misc.GenerateUuidNumeric(2)), new Byte[] { 0x00, 0x00 });
            Database_ReferentID = Misc.Combine(BitConverter.GetBytes(Misc.GenerateUuidNumeric(2)), new Byte[] { 0x00, 0x00 });
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(MachineName_ReferentID, MachineName_MaxCount);
            request = Misc.Combine(request, MachineName_Offset);
            request = Misc.Combine(request, MachineName_ActualCount);
            request = Misc.Combine(request, MachineName);
            request = Misc.Combine(request, Database_ReferentID);
            request = Misc.Combine(request, Database_NameMaxCount);
            request = Misc.Combine(request, Database_NameOffset);
            request = Misc.Combine(request, Database_NameActualCount);
            request = Misc.Combine(request, Database);
            request = Misc.Combine(request, Unknown);
            return Misc.Combine(request, AccessMask);
        }
    }
}
