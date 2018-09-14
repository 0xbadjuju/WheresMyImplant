using System;

namespace WheresMyImplant
{
    class DCERPCAlterContext
    {
        private readonly Byte[] Version = { 0x05 };
        private readonly Byte[] VersionMinor = { 0x00 };
        private readonly Byte[] PacketType = { 0x0e };
        private readonly Byte[] PacketFlags = { 0x03 };
        private readonly Byte[] DataRepresentation = { 0x10, 0x00, 0x00, 0x00 };
        private readonly Byte[] FragLength = { 0x48, 0x00 };
        private readonly Byte[] AuthLength = { 0x00, 0x00 };
        private Byte[] CallID;
        private readonly Byte[] MaxXmitFrag = { 0xd0, 0x16 };
        private readonly Byte[] MaxRecvFrag = { 0xd0, 0x16 };
        private Byte[] AssocGroup;
        private readonly Byte[] NumCtxItems = { 0x01 };
        private readonly Byte[] Unknown = { 0x00, 0x00, 0x00 };
        private Byte[] ContextID;
        private readonly Byte[] NumTransItems = { 0x01 };
        private readonly Byte[] Unknown2 = { 0x00 };
        private Byte[] Interface;
        private readonly Byte[] InterfaceVer = { 0x00, 0x00 };
        private readonly Byte[] InterfaceVerMinor = { 0x00, 0x00 };
        private readonly Byte[] TransferSyntax = { 0x04, 0x5d, 0x88, 0x8a, 0xeb, 0x1c, 0xc9, 0x11, 0x9f, 0xe8, 0x08, 0x00, 0x2b, 0x10, 0x48, 0x60 };
        private readonly Byte[] TransferSyntaxVer = { 0x02, 0x00, 0x00, 0x00 };

        internal DCERPCAlterContext()
        {

        }

        internal void SetCallID(Byte[] CallID)
        {
            this.CallID = CallID;
        }

        internal void SetAssocGroup(Byte[] AssocGroup)
        {
            this.AssocGroup = AssocGroup;
        }

        internal void SetContextID(Byte[] ContextID)
        {
            this.ContextID = ContextID;
        }
        internal void SetInterface(Byte[] Interface)
        {
            this.Interface = Interface;
        }

        internal Byte[] GetRequest()
        {
            Combine combine = new Combine();
            combine.Extend(Version);
            combine.Extend(VersionMinor);
            combine.Extend(PacketType);
            combine.Extend(PacketFlags);
            combine.Extend(DataRepresentation);
            combine.Extend(FragLength);
            combine.Extend(AuthLength);
            combine.Extend(CallID);
            combine.Extend(MaxXmitFrag);
            combine.Extend(MaxRecvFrag);
            combine.Extend(AssocGroup);
            combine.Extend(NumCtxItems);
            combine.Extend(Unknown);
            combine.Extend(ContextID);
            combine.Extend(NumTransItems);
            combine.Extend(Unknown2);
            combine.Extend(Interface);
            combine.Extend(InterfaceVer);
            combine.Extend(InterfaceVerMinor);
            combine.Extend(TransferSyntax);
            combine.Extend(TransferSyntaxVer);
            return combine.Retrieve();
        }
    }
}
