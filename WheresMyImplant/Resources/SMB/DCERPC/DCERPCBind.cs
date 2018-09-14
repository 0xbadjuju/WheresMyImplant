using System;

namespace WheresMyImplant
{
    class DCERPCBind
    {
        private readonly Byte[] Version = { 0x05 };
        private readonly Byte[] VersionMinor = { 0x00 };
        private readonly Byte[] PacketType = { 0x0b };
        private readonly Byte[] PacketFlags = { 0x03 };
        private readonly Byte[] DataRepresentation = { 0x10, 0x00, 0x00, 0x00 };
        private Byte[] FragLength = new Byte[2];
        private Byte[] AuthLength = { 0x00, 0x00 };
        private Byte[] CallID = new Byte[2];
        private readonly Byte[] MaxXmitFrag = { 0xb8, 0x10 };
        private readonly Byte[] MaxRecvFrag = { 0xb8, 0x10 };
        private readonly Byte[] AssocGroup = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] NumCtxItems = new Byte[1];
        private readonly Byte[] Unknown = { 0x00, 0x00, 0x00 };
        private Byte[] ContextID = new Byte[2];
        private readonly Byte[] NumTransItems = { 0x01 };
        private readonly Byte[] Unknown2 = { 0x00 };
        private Byte[] Interface = new Byte[16];
        private Byte[] InterfaceVer = new Byte[2];
        private readonly Byte[] InterfaceVerMinor = { 0x00, 0x00 };
        private readonly Byte[] TransferSyntax = { 0x04, 0x5d, 0x88, 0x8a, 0xeb, 0x1c, 0xc9, 0x11, 0x9f, 0xe8, 0x08, 0x00, 0x2b, 0x10, 0x48, 0x60 };
        private readonly Byte[] TransferSyntaxVer = { 0x02, 0x00, 0x00, 0x00 };
        private Byte[] ExtraData = new Byte[0];

        internal DCERPCBind()
        {

        }

        internal void SetFragLength(Byte[] FragLength)
        {
            if (this.FragLength.Length == FragLength.Length)
            {
                this.FragLength = FragLength;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetAuthLength(Byte[] AuthLength)
        {
            if (this.AuthLength.Length == AuthLength.Length)
            {
                this.AuthLength = AuthLength;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetCallID(Int32 dwCallID)
        {
            SetCallID(dwCallID, new Byte[] { 0x97, 0x82, 0x08, 0xe2 });
        }

        internal void SetCallID(Int32 dwCallID, Byte[] NegotiateFlags)
        {
            SetCallID(dwCallID, new Byte[] { 0x02 }, NegotiateFlags);
        }

        internal void SetCallID(Int32 dwCallID, Byte[] AuthLevel, Byte[] NegotiateFlags)
        {
            CallID = BitConverter.GetBytes(dwCallID);

            if (3 == dwCallID)
            {
                Byte[] AuthType = { 0x0a };
                //Byte[] AuthLevel = {  };
                Byte[] AuthPadLength = { 0x00 };
                Byte[] AuthReserved = { 0x00 };
                Byte[] ContextID3 = { 0x00, 0x00, 0x00, 0x00};
                Byte[] Identifier = { 0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00 };
                Byte[] MessageType = { 0x01, 0x00, 0x00, 0x00 };
                //Byte[] NegotiateFlags = { 0x97, 0x82, 0x08, 0xe2 };
                Byte[] CallingWorkstationDomain = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Byte[] CallingWorkstationName = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Byte[] OSVersion = { 0x06, 0x01, 0xb1, 0x1d, 0x00, 0x00, 0x00, 0x0f };

                Combine combine = new Combine();
                combine.Extend(AuthType);
                combine.Extend(AuthLevel);
                combine.Extend(AuthPadLength);
                combine.Extend(AuthReserved);
                combine.Extend(ContextID3);
                combine.Extend(Identifier);
                combine.Extend(MessageType);
                combine.Extend(NegotiateFlags);
                combine.Extend(CallingWorkstationDomain);
                combine.Extend(CallingWorkstationName);
                combine.Extend(OSVersion);
                ExtraData = Misc.Combine(ExtraData, combine.Retrieve());
            }
        }

        internal void SetNumCtxItems(Byte[] NumCtxItems)
        {
            SetNumCtxItems(NumCtxItems, new Byte[] { 0x97, 0x82, 0x08, 0xe2 });
        }

        internal void SetNumCtxItems(Byte[] NumCtxItems, Byte[] NegotiateFlags)
        {
            if (this.NumCtxItems.Length == NumCtxItems.Length)
            {
                this.NumCtxItems = NumCtxItems;
            }

            if (2 == NumCtxItems[0])
            {
                Byte[] ContextID2 = { 0x01, 0x00 };
                Byte[] NumTransItems2 = { 0x01 };
                Byte[] Unknown3 = { 0x00 };
                Byte[] Interface2 = { 0xc4, 0xfe, 0xfc, 0x99, 0x60, 0x52, 0x1b, 0x10, 0xbb, 0xcb, 0x00, 0xaa, 0x00, 0x21, 0x34, 0x7a };
                Byte[] InterfaceVer2 = { 0x00, 0x00 };
                Byte[] InterfaceVerMinor2 = { 0x00, 0x00 };
                Byte[] TransferSyntax2 = { 0x2c, 0x1c, 0xb7, 0x6c, 0x12, 0x98, 0x40, 0x45, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Byte[] TransferSyntaxVer2 = { 0x01, 0x00, 0x00, 0x00 };

                Combine combine = new Combine();
                combine.Extend(ContextID2);
                combine.Extend(NumTransItems2);
                combine.Extend(Unknown3);
                combine.Extend(Interface2);
                combine.Extend(InterfaceVer2);
                combine.Extend(InterfaceVerMinor2);
                combine.Extend(TransferSyntax2);
                combine.Extend(TransferSyntaxVer2);
                ExtraData = Misc.Combine(ExtraData, combine.Retrieve());
            }
            else if(3 == NumCtxItems[0])
            {
                Byte[] ContextID2 = { 0x01, 0x00 };
                Byte[] NumTransItems2 = { 0x01 };
                Byte[] Unknown3 = { 0x00 };
                Byte[] Interface2 = { 0x43, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
                Byte[] InterfaceVer2 = { 0x00, 0x00 };
                Byte[] InterfaceVerMinor2 = { 0x00, 0x00 };
                Byte[] TransferSyntax2 = { 0x33, 0x05, 0x71, 0x71, 0xba, 0xbe, 0x37, 0x49, 0x83, 0x19, 0xb5, 0xdb, 0xef, 0x9c, 0xcc, 0x36 };
                Byte[] TransferSyntaxVer2 = { 0x01, 0x00, 0x00, 0x00 };

                Byte[] ContextID3 = { 0x02, 0x00 };
                Byte[] NumTransItems3 = { 0x01 };
                Byte[] Unknown4 = { 0x00 };
                Byte[] Interface3 = { 0x43, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
                Byte[] InterfaceVer3 = { 0x00, 0x00 };
                Byte[] InterfaceVerMinor3 = { 0x00, 0x00 };
                Byte[] TransferSyntax3 = { 0x2c, 0x1c, 0xb7, 0x6c, 0x12, 0x98, 0x40, 0x45, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Byte[] TransferSyntaxVer3 = { 0x01, 0x00, 0x00, 0x00 };

                Byte[] AuthType = { 0x0a };
                Byte[] AuthLevel = { 0x04 };
                Byte[] AuthPadLength = { 0x00 };
                Byte[] AuthReserved = { 0x00 };
                Byte[] ContextID4 = { 0x00, 0x00, 0x00, 0x00 };
                Byte[] Identifier = { 0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00 };
                Byte[] MessageType = { 0x01, 0x00, 0x00, 0x00 };
                //Byte[] NegotiateFlags = { 0x97, 0x82, 0x08, 0xe2 };
                Byte[] CallingWorkstationDomain = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Byte[] CallingWorkstationName = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
                Byte[] OSVersion = { 0x06, 0x01, 0xb1, 0x1d, 0x00, 0x00, 0x00, 0x0f };

                Combine combine = new Combine();
                combine.Extend(ContextID2);
                combine.Extend(NumTransItems2);
                combine.Extend(Unknown3);
                combine.Extend(Interface2);
                combine.Extend(InterfaceVer2);
                combine.Extend(InterfaceVerMinor2);
                combine.Extend(TransferSyntax2);
                combine.Extend(TransferSyntaxVer2);

                combine.Extend(ContextID3);
                combine.Extend(NumTransItems3);
                combine.Extend(Unknown4);
                combine.Extend(Interface3);
                combine.Extend(InterfaceVer3);
                combine.Extend(InterfaceVerMinor3);
                combine.Extend(TransferSyntax3);
                combine.Extend(TransferSyntaxVer3);

                combine.Extend(AuthType);
                combine.Extend(AuthLevel);
                combine.Extend(AuthPadLength);
                combine.Extend(AuthReserved);
                combine.Extend(ContextID4);
                combine.Extend(Identifier);
                combine.Extend(MessageType);
                combine.Extend(NegotiateFlags);
                combine.Extend(CallingWorkstationDomain);
                combine.Extend(CallingWorkstationName);
                combine.Extend(OSVersion);
                ExtraData = Misc.Combine(ExtraData, combine.Retrieve());
            }

        }

        internal void SetContextID(Byte[] ContextID)
        {
            if (this.ContextID.Length == ContextID.Length)
            {
                this.ContextID = ContextID;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetInterface(Byte[] Interface)
        {
            if (this.Interface.Length == Interface.Length)
            {
                this.Interface = Interface;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetInterfaceVer(Byte[] InterfaceVer)
        {
            if (this.InterfaceVer.Length == InterfaceVer.Length)
            {
                this.InterfaceVer = InterfaceVer;
                return;
            }
            throw new IndexOutOfRangeException();
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
            combine.Extend(ExtraData);
            return combine.Retrieve();
        }
    }
}
