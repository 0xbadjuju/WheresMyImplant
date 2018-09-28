using System;
using System.Linq;
using System.Security.Cryptography;

namespace WheresMyImplant
{
    class SMB2Header
    {
        private readonly Byte[] ServerComponent = { 0xfe, 0x53, 0x4d, 0x42 };
        private readonly Byte[] HeaderLength = { 0x40, 0x00 };
        private readonly Byte[] CreditCharge = { 0x01, 0x00 };
        private Byte[] ChannelSequence = new Byte[2];
        private readonly Byte[] Reserved = { 0x00, 0x00 };
        private Byte[] Command = new Byte[2];
        private Byte[] CreditsRequested = new Byte[2];
        private Byte[] Flags = new Byte[4];
        private Byte[] ChainOffset = new Byte[4];
        private Byte[] MessageID = new Byte[8];
        private Byte[] ProcessId = new Byte[4];
        private Byte[] TreeId = new Byte[4];
        private Byte[] SessionId = new Byte[8];
        private Byte[] Signature = new Byte[16];

        internal SMB2Header()
        {
            ChannelSequence = new Byte[] { 0x00, 0x00 };
            Flags = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
            ChainOffset = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
        }

        internal void SetCommand(Byte[] Command)
        {
            if (Command.Length == this.Command.Length)
            {
                this.Command = Command;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetCreditsRequested(Byte[] creditsRequested)
        {
            if (creditsRequested.Length == this.CreditsRequested.Length)
            {
                this.CreditsRequested = creditsRequested;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetFlags(Byte[] Flags)
        {
            if (Flags.Length == this.Flags.Length)
            {
                this.Flags = Flags;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetChainOffset(Int32 dataLength)
        {
            ChainOffset = BitConverter.GetBytes(GetHeader().Length + dataLength);
        }

        internal void SetChainOffset(Byte[] ChainOffset)
        {
            if (ChainOffset.Length == this.ChainOffset.Length)
            {
                this.ChainOffset = ChainOffset;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetMessageID(UInt32 MessageID)
        {
            this.MessageID = Misc.Combine(BitConverter.GetBytes(MessageID), new Byte[] { 0x00, 0x00, 0x00, 0x00 });
        }

        internal void SetProcessID(Byte[] ProcessId)
        {
            if (ProcessId.Length == this.ProcessId.Length)
            {
                this.ProcessId = ProcessId;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetTreeId(Byte[] TreeId)
        {
            if (TreeId.Length == this.TreeId.Length)
            {
                this.TreeId = TreeId;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetSessionID(Byte[] SessionId)
        {
            if (SessionId.Length == this.SessionId.Length)
            {
                this.SessionId = SessionId;
                return;
            }
            throw new IndexOutOfRangeException();
        }

        internal void SetSignature(Byte[] sessionKey, ref Byte[] data)
        {
            using (HMACSHA256 sha256 = new HMACSHA256())
            {
                sha256.Key = sessionKey;
                this.Signature = sha256.ComputeHash(Misc.Combine(GetHeader(), data)).Take(16).ToArray();
            }
        }

        internal Byte[] GetHeader()
        {
            Combine combine = new Combine();
            combine.Extend(ServerComponent);
            combine.Extend(HeaderLength);
            combine.Extend(CreditCharge);
            combine.Extend(ChannelSequence);
            combine.Extend(Reserved);
            combine.Extend(Command);
            combine.Extend(CreditsRequested);
            combine.Extend(Flags);
            combine.Extend(ChainOffset);
            combine.Extend(MessageID);
            combine.Extend(ProcessId);
            combine.Extend(TreeId);
            combine.Extend(SessionId);
            combine.Extend(Signature);
            return combine.Retrieve();
        }
    }
}