using System;
using System.Linq;

namespace WheresMyImplant
{
    class SMB2CreateRequest
    {
        private Byte[] bAllocationSize;

        private readonly Byte[] StructureSize = { 0x39, 0x00 };
        private readonly Byte[] Flags = { 0x00 };
        private readonly Byte[] RequestedOplockLevel = { 0x00 };
        private readonly Byte[] Impersonation = { 0x02, 0x00, 0x00, 0x00 };
        private readonly Byte[] CreateFlags = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Reserved = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private Byte[] AccessMask = { 0x03, 0x00, 0x00, 0x00 };
        private Byte[] FileAttributes = { 0x80, 0x00, 0x00, 0x00 };
        private Byte[] ShareAccess = { 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] Disposition = { 0x01, 0x00, 0x00, 0x00 };
        private Byte[] CreateOptions = { 0x40, 0x00, 0x00, 0x00 };
        private readonly Byte[] FileNameBlobOffset = { 0x78, 0x00 };
        private Byte[] FileNameBlobLength = { 0x00, 0x00 };
        private Byte[] BlobOffset = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] BlobLength = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] Buffer = { 0x00, 0x00, 0x69, 0x00, 0x6e, 0x00, 0x64, 0x00 };//{ 0x00, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x28, 0x00 };//
        private Byte[] ExtraInfo = new Byte[0];

        internal SMB2CreateRequest()
        {

        }

        internal void SetFileName(String filename)
        {
            Buffer = System.Text.Encoding.Unicode.GetBytes(filename);
            FileNameBlobLength = BitConverter.GetBytes(System.Text.Encoding.Unicode.GetByteCount(filename)).Take(2).ToArray();

            Double paddingCheck = (Buffer.Length) / 8.0;
            if ((paddingCheck + 0.25) == Math.Ceiling(paddingCheck))
            {
                Buffer = Misc.Combine(Buffer, new Byte[] { 0x04, 0x00 });
            }
            else if ((paddingCheck + 0.50) == Math.Ceiling(paddingCheck))
            {
                Buffer = Misc.Combine(Buffer, new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            }
            else if ((paddingCheck + 0.75) == Math.Ceiling(paddingCheck))
            {
                Buffer = Misc.Combine(Buffer, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            }
            else
            {
                Console.WriteLine(paddingCheck);
            }
        }

        internal void SetExtraInfo(Int32 extraInfo, Int64 allocationSize)
        {
            AccessMask = new Byte[] { 0x80, 0x00, 0x10, 0x00 };
            FileAttributes = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
            ShareAccess = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
            CreateOptions = new Byte[] { 0x21, 0x00, 0x00, 0x00 };
            BlobOffset = BitConverter.GetBytes(FileNameBlobLength.Length);

            switch (extraInfo)
            {
                case 1:
                    BlobLength = new Byte[] { 0x58, 0x00, 0x00, 0x00 };
                    break;
                case 2:
                    BlobLength = new Byte[] { 0x90, 0x00, 0x00, 0x00 };
                    break;
                default:
                    BlobLength = new Byte[] { 0xb0, 0x00, 0x00, 0x00 };
                    bAllocationSize = BitConverter.GetBytes(allocationSize);
                    break;
            }
            BlobOffset = BitConverter.GetBytes(Buffer.Length + 120);

            Byte[] ExtraInfoDHnQ_ChainOffset = { 0x28, 0x00, 0x00, 0x00 };
            Byte[] ExtraInfoDHnQ_TagOffset = { 0x10, 0x00 };
            Byte[] ExtraInfoDHnQ_TagLength = { 0x04, 0x00, 0x00, 0x00 };
            Byte[] ExtraInfoDHnQ_DataOffset = { 0x18, 0x00};
            Byte[] ExtraInfoDHnQ_DataLength = { 0x10, 0x00, 0x00, 0x00 };
            Byte[] ExtraInfoDHnQ_Tag = { 0x44, 0x48, 0x6e, 0x51 };
            Byte[] ExtraInfoDHnQ_Unknown = { 0x00, 0x00, 0x00, 0x00 };
            Byte[] ExtraInfoDHnQ_DataGUIDHandle = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00};

            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_ChainOffset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_TagOffset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_TagLength);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_DataOffset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_DataLength);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_Tag);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_Unknown);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoDHnQ_DataGUIDHandle);

            if(extraInfo == 3)
            {
                Byte[] ExtraInfoAlSi_ChainOffset = { 0x20, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoAlSi_Tag_Offset = { 0x10, 0x00 };
                Byte[] ExtraInfoAlSi_Tag_Length = { 0x04, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoAlSi_Data_Offset = { 0x18, 0x00 };
                Byte[] ExtraInfoAlSi_Data_Length = { 0x08, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoAlSi_Tag = { 0x41, 0x6c, 0x53, 0x69 };
                Byte[] ExtraInfoAlSi_Unknown = { 0x00, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoAlSi_AllocationSize = bAllocationSize;

                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_ChainOffset);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_Tag_Offset);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_Tag_Length);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_Data_Offset);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_Data_Length);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_Tag);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_Unknown);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoAlSi_AllocationSize);
            }

            Byte[] ExtraInfoMxAc_ChainOffset = { 0x18, 0x00, 0x00, 0x00};
            Byte[] ExtraInfoMxAc_Tag_Offset = { 0x10, 0x00};
            Byte[] ExtraInfoMxAc_Tag_Length = { 0x04, 0x00, 0x00, 0x00};
            Byte[] ExtraInfoMxAc_Data_Offset = { 0x18, 0x00};
            Byte[] ExtraInfoMxAc_Data_Length = { 0x00, 0x00, 0x00, 0x00};
            Byte[] ExtraInfoMxAc_Tag = { 0x4d, 0x78, 0x41, 0x63};
            Byte[] ExtraInfoMxAc_Unknown = { 0x00, 0x00, 0x00, 0x00};
            
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoMxAc_ChainOffset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoMxAc_Tag_Offset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoMxAc_Tag_Length);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoMxAc_Data_Offset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoMxAc_Data_Length);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoMxAc_Tag);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoMxAc_Unknown);

            Byte[] ExtraInfoQFid_ChainOffset;
            if (extraInfo > 1)
            {
                ExtraInfoQFid_ChainOffset = new Byte[] { 0x18, 0x00, 0x00, 0x00 };
            }
            else
            {
                ExtraInfoQFid_ChainOffset = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
            }
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoQFid_ChainOffset);

            Byte[] ExtraInfoQFid_Tag_Offset = { 0x10, 0x00 };
            Byte[] ExtraInfoQFid_Tag_Length = { 0x04, 0x00, 0x00, 0x00 };
            Byte[] ExtraInfoQFid_Data_Offset = { 0x18, 0x00 };
            Byte[] ExtraInfoQFid_Data_Length = { 0x00, 0x00, 0x00, 0x00 };
            Byte[] ExtraInfoQFid_Tag = { 0x51, 0x46, 0x69, 0x64};
            Byte[] ExtraInfoQFid_Unknown = { 0x00, 0x00, 0x00, 0x00 };

            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoQFid_Tag_Offset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoQFid_Tag_Length);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoQFid_Data_Offset);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoQFid_Data_Length);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoQFid_Tag);
            ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoQFid_Unknown);

            if(extraInfo > 1)
            {
                Byte[] ExtraInfoRqLs_ChainOffset = { 0x00, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoRqLs_Tag_Offset = { 0x10, 0x00 };
                Byte[] ExtraInfoRqLs_Tag_Length = { 0x04, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoRqLs_Data_Offset = { 0x18, 0x00 };
                Byte[] ExtraInfoRqLs_Data_Length = { 0x20, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoRqLs_Tag = { 0x52, 0x71, 0x4c, 0x73};
                Byte[] ExtraInfoRqLs_Unknown = { 0x00, 0x00, 0x00, 0x00 };

                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_ChainOffset);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Tag_Offset);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Tag_Length);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Data_Offset);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Data_Length);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Tag);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Unknown);

                Byte[] ExtraInfoRqLs_DataLeaseKey;
                if(extraInfo == 2)
                {
                    ExtraInfoRqLs_DataLeaseKey = new Byte[] { 0x10, 0xb0, 0x1d, 0x02, 0xa0, 0xf8, 0xff, 0xff, 0x47, 0x78, 0x67, 0x02, 0x00, 0x00, 0x00, 0x00 };
                }
                else
                {
                    ExtraInfoRqLs_DataLeaseKey = new Byte[] { 0x10, 0x90, 0x64, 0x01, 0xa0, 0xf8, 0xff, 0xff, 0x47, 0x78, 0x67, 0x02, 0x00, 0x00, 0x00, 0x00 };
                }

                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_DataLeaseKey);

                Byte[] ExtraInfoRqLs_Data_Lease_State = { 0x07, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoRqLs_Data_Lease_Flags = { 0x00, 0x00, 0x00, 0x00 };
                Byte[] ExtraInfoRqLs_Data_Lease_Duration = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Data_Lease_State);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Data_Lease_Flags);
                ExtraInfo = Misc.Combine(ExtraInfo, ExtraInfoRqLs_Data_Lease_Duration);
            }
        }

        internal void SetAccessMask(Byte[] AccessMask)
        {
            if (AccessMask.Length == this.AccessMask.Length)
            {
                this.AccessMask = AccessMask;
            }
        }

        internal void SetShareAccess(Byte[] ShareAccess)
        {
            if (ShareAccess.Length == this.ShareAccess.Length)
            {
                this.ShareAccess = ShareAccess;
            }
        }

        internal void SetCreateOptions(Byte[] CreateOptions)
        {
            if (CreateOptions.Length == this.CreateOptions.Length)
            {
                this.CreateOptions = CreateOptions;
            }
        }

        internal void SetBlobOffSet(String filename)
        {
            BlobOffset = BitConverter.GetBytes(filename.Length + 120);
            BlobLength = new Byte[] { 0x40, 0x00, 0x00, 0x00 };
        }

        internal Byte[] GetRequest()
        {
            Byte[] request = Misc.Combine(StructureSize, Flags);
            request = Misc.Combine(request, RequestedOplockLevel);
            request = Misc.Combine(request, Impersonation);
            request = Misc.Combine(request, CreateFlags);
            request = Misc.Combine(request, Reserved);
            request = Misc.Combine(request, AccessMask);
            request = Misc.Combine(request, FileAttributes);
            request = Misc.Combine(request, ShareAccess);
            request = Misc.Combine(request, Disposition);
            request = Misc.Combine(request, CreateOptions);
            request = Misc.Combine(request, FileNameBlobOffset);
            request = Misc.Combine(request, FileNameBlobLength);
            request = Misc.Combine(request, BlobOffset);
            request = Misc.Combine(request, BlobLength);
            request = Misc.Combine(request, Buffer);
            request = Misc.Combine(request, ExtraInfo);
            return request;
        }
    }
}