using System;

namespace WheresMyImplant
{
    class DCOMRemQueryInterface
    {
        private readonly Byte[] VersionMajor = { 0x05, 0x00 };
        private readonly Byte[] VersionMinor = { 0x07, 0x00 };
        private readonly Byte[] Flags = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Reserved = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] CausalityID;
        private readonly Byte[] Reserved2 = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] IPID;
        private readonly Byte[] Refs = { 0x05, 0x00, 0x00, 0x00 };
        private readonly Byte[] IIDs = { 0x01, 0x00 };
        private readonly Byte[] Unknown = { 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
        private Byte[] IID;

        internal DCOMRemQueryInterface()
        {

        }

        internal void SetCausalityID(Byte[] CausalityID)
        {
            this.CausalityID = CausalityID;
        }

        internal void SetIPID(Byte[] IPID)
        {
            this.IPID = IPID;
        }

        internal void SetIID(Byte[] IID)
        {
            this.IID = IID;
        }

        internal Byte[] GetRequest()
        {
            Combine combine = new Combine();
            combine.Extend(VersionMajor);
            combine.Extend(VersionMinor);
            combine.Extend(Flags);
            combine.Extend(Reserved);
            combine.Extend(CausalityID);
            combine.Extend(Reserved2);
            combine.Extend(IPID);
            combine.Extend(Refs);
            combine.Extend(IIDs);
            combine.Extend(Unknown);
            combine.Extend(IID);
            return combine.Retrieve();
        }
    }
}
