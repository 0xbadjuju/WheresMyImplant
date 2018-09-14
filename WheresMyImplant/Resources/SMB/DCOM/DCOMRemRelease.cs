using System;

namespace WheresMyImplant
{
    class DCOMRemRelease
    {
        private readonly Byte[] VersionMajor = { 0x05, 0x00 };
        private readonly Byte[] VersionMinor = { 0x07, 0x00 };
        private readonly Byte[] Flags = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Reserved = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] CausalityID;
        private readonly Byte[] Reserved2 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Unknown = { 0x02, 0x00, 0x00, 0x00 };
        private readonly Byte[] InterfaceRefs = { 0x02, 0x00, 0x00, 0x00 };
        private Byte[] IPID;
        private readonly Byte[] PublicRefs = { 0x05, 0x00, 0x00, 0x00 };
        private readonly Byte[] PrivateRefs = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] IPID2;
        private readonly Byte[] PublicRefs2 = { 0x05, 0x00, 0x00, 0x00 };
        private readonly Byte[] PrivateRefs2 = { 0x00, 0x00, 0x00, 0x00 };

        internal DCOMRemRelease()
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

        internal void SetIPID2(Byte[] IPID2)
        {
            this.IPID2 = IPID2;
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
            combine.Extend(Unknown);
            combine.Extend(InterfaceRefs);
            combine.Extend(IPID);
            combine.Extend(PublicRefs);
            combine.Extend(PrivateRefs);
            combine.Extend(IPID2);
            combine.Extend(PublicRefs2);
            combine.Extend(PrivateRefs2);
            return combine.Retrieve();
        }
    }
}
