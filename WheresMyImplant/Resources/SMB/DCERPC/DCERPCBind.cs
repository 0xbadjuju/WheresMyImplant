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
        private readonly Byte[] AuthLength = { 0x00, 0x00 };
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

        internal void SetCallID(Int32 dwCallID)
        {
            CallID = BitConverter.GetBytes(dwCallID);
        }

        internal void SetNumCtxItems(Byte[] NumCtxItems)
        {
            if (this.NumCtxItems.Length == NumCtxItems.Length)
            {
                this.NumCtxItems = NumCtxItems;
                return;
            }
            throw new IndexOutOfRangeException();
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
            Byte[] request = Misc.Combine(Version, VersionMinor);
            request = Misc.Combine(request, PacketType);
            request = Misc.Combine(request, PacketFlags);
            request = Misc.Combine(request, DataRepresentation);
            request = Misc.Combine(request, FragLength);
            request = Misc.Combine(request, AuthLength);
            request = Misc.Combine(request, CallID);
            request = Misc.Combine(request, MaxXmitFrag);
            request = Misc.Combine(request, MaxRecvFrag);
            request = Misc.Combine(request, AssocGroup);
            request = Misc.Combine(request, NumCtxItems);
            request = Misc.Combine(request, Unknown);
            request = Misc.Combine(request, ContextID);
            request = Misc.Combine(request, NumTransItems);
            request = Misc.Combine(request, Unknown2);
            request = Misc.Combine(request, Interface);
            request = Misc.Combine(request, InterfaceVer);
            request = Misc.Combine(request, InterfaceVerMinor);
            request = Misc.Combine(request, TransferSyntax);
            return Misc.Combine(request, TransferSyntaxVer);

        }
    }
}
