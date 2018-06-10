using System;
using System.Collections.Specialized;
using System.Linq;
using System.Text.RegularExpressions;

namespace WheresMyImplant
{
    class SMBClient
    {
        SMBClient()
        {
        }

        private OrderedDictionary GetSMB2PacketHeader(Byte[] packetCommand, Byte[] creditRequest, Byte[] messageID, Byte[] treeID, Byte[] sessionID)
        {
            OrderedDictionary header = new OrderedDictionary();
            header.Add("ProtocolID", new Byte[]{ 0xfe, 0x53, 0x4d, 0x42 });
            header.Add("StructureSize", new Byte[] { 0x40, 0x00 });
            header.Add("CreditCharge", new Byte[] { 0x01, 0x00 });
            header.Add("ChannelSequence", new Byte[] { 0x00, 0x00 });
            header.Add("Reserved", new Byte[] { 0x00, 0x00 });
            header.Add("Command", packetCommand);
            header.Add("CreditRequest", creditRequest);
            header.Add("Flags", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            header.Add("NextCommand", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            messageID = Misc.Combine(messageID, new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            header.Add("MessageID", messageID);
            header.Add("ProcessID", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            header.Add("TreeID", treeID);
            header.Add("SessionID", sessionID);
            header.Add("Signature", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            return header;
        }   

        private OrderedDictionary PacketSMB2NegotiateProtocolRequest()
        {
            OrderedDictionary SMB2NegotiateProtocolRequest = new OrderedDictionary();
            SMB2NegotiateProtocolRequest.Add("StructureSize", new Byte[] { 0x24, 0x00 });
            SMB2NegotiateProtocolRequest.Add("DialectCount", new Byte[] { 0x02, 0x00 });
            SMB2NegotiateProtocolRequest.Add("SecurityMode", new Byte[] { 0x01, 0x00 });
            SMB2NegotiateProtocolRequest.Add("Reserved", new Byte[] { 0x00, 0x00 });
            SMB2NegotiateProtocolRequest.Add("Capabilities", new Byte[] { 0x40, 0x00, 0x00, 0x00 });
            SMB2NegotiateProtocolRequest.Add("ClientGUID", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            SMB2NegotiateProtocolRequest.Add("NegotiateContextOffset", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2NegotiateProtocolRequest.Add("NegotiateContextCount", new Byte[] { 0x00, 0x00 });
            SMB2NegotiateProtocolRequest.Add("Reserved2", new Byte[] { 0x00, 0x00 });
            SMB2NegotiateProtocolRequest.Add("Dialect", new Byte[] { 0x02, 0x02 });
            SMB2NegotiateProtocolRequest.Add("Dialect2", new Byte[] { 0x10, 0x02 });

            return SMB2NegotiateProtocolRequest;
        }

        private OrderedDictionary PacketSMB2SessionSetupRequest(Byte[] security_blob)
        {
            Byte[] security_blob_length = System.BitConverter.GetBytes(security_blob.Length);
            security_blob_length = security_blob_length.Take(1).ToArray();

            OrderedDictionary SMB2SessionSetupRequest = new OrderedDictionary();
            SMB2SessionSetupRequest.Add("StructureSize", new Byte[] { 0x19, 0x00 });
            SMB2SessionSetupRequest.Add("Flags", new Byte[] { 0x00 });
            SMB2SessionSetupRequest.Add("SecurityMode", new Byte[] { 0x01});
            SMB2SessionSetupRequest.Add("Capabilities", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2SessionSetupRequest.Add("Channel", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2SessionSetupRequest.Add("SecurityBufferOffset", new Byte[] { 0x58, 0x00 });
            SMB2SessionSetupRequest.Add("SecurityBufferLength", security_blob_length);
            SMB2SessionSetupRequest.Add("PreviousSessionID", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            SMB2SessionSetupRequest.Add("Buffer", security_blob);

            return SMB2SessionSetupRequest;
        }

        private OrderedDictionary PacketSMB2TreeConnectRequest(Byte[] path)
        {
            Byte[] path_length = System.BitConverter.GetBytes(path.Length);
            path_length = path_length.Take(1).ToArray();

            OrderedDictionary SMB2TreeConnectRequest = new OrderedDictionary();
            SMB2TreeConnectRequest.Add("StructureSize", new Byte[] { 0x09, 0x00 });
            SMB2TreeConnectRequest.Add("Reserved", new Byte[] { 0x00, 0x00 });
            SMB2TreeConnectRequest.Add("PathOffset", new Byte[] { 0x48, 0x00 });
            SMB2TreeConnectRequest.Add("PathLength",path_length);
            SMB2TreeConnectRequest.Add("Buffer",path);

            return SMB2TreeConnectRequest;
        }

        private OrderedDictionary PacketSMB2IoctlRequest(Byte[] file_name)
        {
            Byte[] file_name_length = BitConverter.GetBytes(file_name.Length + 2);

            OrderedDictionary SMB2IoctlRequest = new OrderedDictionary();
            SMB2IoctlRequest.Add("StructureSize", new Byte[] { 0x39, 0x00 });
            SMB2IoctlRequest.Add("Reserved", new Byte[] { 0x00, 0x00 });
            SMB2IoctlRequest.Add("Function", new Byte[] { 0x94, 0x01, 0x06, 0x00 });
            SMB2IoctlRequest.Add("GUIDHandle", new Byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff});
            SMB2IoctlRequest.Add("InData_Offset", new Byte[] { 0x78, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("InData_Length",file_name_length);
            SMB2IoctlRequest.Add("MaxIoctlInSize", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("OutData_Offset", new Byte[] { 0x78, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("OutData_Length", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("MaxIoctlOutSize", new Byte[] { 0x00, 0x10, 0x00, 0x00 });
            SMB2IoctlRequest.Add("Flags", new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("Unknown", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("InData_MaxReferralLevel", new Byte[] { 0x04, 0x00 });
            SMB2IoctlRequest.Add("InData_FileName",file_name);

            return SMB2IoctlRequest;
        }

        private OrderedDictionary PacketSMB2CreateRequest(Byte[] file_name, Int32 extra_info, Int64 allocation_size)
        {
            Byte[] file_name_length;
            if (file_name.Length > 0)
            {
                file_name_length = System.BitConverter.GetBytes(file_name.Length);
                file_name_length = file_name_length.Take(1).ToArray();
            }
            else
            {
                file_name = new Byte[] { 0x00, 0x00, 0x69, 0x00, 0x6e, 0x00, 0x64, 0x00 };
                file_name_length = new Byte[] { 0x00, 0x00 };
            }

            Byte[] desired_access = { 0x03, 0x00, 0x00, 0x00 };
            Byte[] file_attributes = { 0x80, 0x00, 0x00, 0x00 };
            Byte[] share_access = { 0x01, 0x00, 0x00, 0x00 };
            Byte[] create_options = { 0x40, 0x00, 0x00, 0x00 };
            Byte[] create_contexts_offset = { 0x00, 0x00, 0x00, 0x00 };
            Byte[] create_contexts_length = { 0x00, 0x00, 0x00, 0x00 };
            Byte[] allocation_size_bytes = new Byte[0];

            if(extra_info > 0)
            {
                desired_access = new Byte[] { 0x80, 0x00, 0x10, 0x00 };
                file_attributes = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
                share_access = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
                create_options =  new Byte[] { 0x21, 0x00, 0x00, 0x00 };
                create_contexts_offset = System.BitConverter.GetBytes(file_name.Length);

                if (extra_info == 1)
                {
                    create_contexts_length = new Byte[] { 0x58, 0x00, 0x00, 0x00 };
                }
                else if (extra_info == 2)
                {
                    create_contexts_length = new Byte[] { 0x90, 0x00, 0x00, 0x00 };
                }
                else
                {
                    create_contexts_length = new Byte[] { 0xb0, 0x00, 0x00, 0x00 };
                    allocation_size_bytes = System.BitConverter.GetBytes(allocation_size);
                }

                if(file_name.Length > 0)
                {
                    String file_name_padding_check = Convert.ToString(file_name.Length / 8);

                    if (Regex.Match(file_name_padding_check, "*.75").Success)
                    {
                        file_name = Misc.Combine(file_name, new Byte[] { 0x04, 0x00 });
                    }
                    else if (Regex.Match(file_name_padding_check, "*.5").Success)
                    {
                        file_name = Misc.Combine(file_name, new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                    }
                    else if (Regex.Match(file_name_padding_check, "*.25").Success)
                    {
                       file_name = Misc.Combine(file_name, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                    }
                }

                create_contexts_offset = System.BitConverter.GetBytes(file_name.Length + 120);
            }

            OrderedDictionary SMB2CreateRequest = new OrderedDictionary();
            SMB2CreateRequest.Add("StructureSize", new Byte[] { 0x39, 0x00 });
            SMB2CreateRequest.Add("Flags", new Byte[] { 0x00 });
            SMB2CreateRequest.Add("RequestedOplockLevel", new Byte[] { 0x00 });
            SMB2CreateRequest.Add("Impersonation", new Byte[] { 0x02, 0x00, 0x00, 0x00 });
            SMB2CreateRequest.Add("SMBCreateFlags", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            SMB2CreateRequest.Add("Reserved", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            SMB2CreateRequest.Add("DesiredAccess", desired_access);
            SMB2CreateRequest.Add("FileAttributes", file_attributes);
            SMB2CreateRequest.Add("ShareAccess", share_access);
            SMB2CreateRequest.Add("CreateDisposition", new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            SMB2CreateRequest.Add("CreateOptions", create_options);
            SMB2CreateRequest.Add("NameOffset", new Byte[] { 0x78, 0x00 });
            SMB2CreateRequest.Add("NameLength",file_name_length);
            SMB2CreateRequest.Add("CreateContextsOffset", create_contexts_offset);
            SMB2CreateRequest.Add("CreateContextsLength", create_contexts_length);
            SMB2CreateRequest.Add("Buffer", file_name);

            if(extra_info > 0)
            {
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_ChainOffset", new Byte[] { 0x28, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_Tag_Offset", new Byte[] { 0x10, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_Tag_Length", new Byte[] { 0x04, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_Data_Offset", new Byte[] { 0x18, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_Data_Length", new Byte[] { 0x10, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_Tag", new Byte[] { 0x44, 0x48, 0x6e, 0x51 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_Unknown", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementDHnQ_Data_GUIDHandle", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

                if(extra_info > 3)
                {
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_ChainOffset", new Byte[] { 0x20, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_Tag_Offset", new Byte[] { 0x10, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_Tag_Length", new Byte[] { 0x04, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_Data_Offset", new Byte[] { 0x18, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_Data_Length", new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_Tag", new Byte[] { 0x41, 0x6c, 0x53, 0x69 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_Unknown", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementAlSi_AllocationSize", allocation_size_bytes);
                }

                SMB2CreateRequest.Add("ExtraInfo_ChainElementMxAc_ChainOffset", new Byte[] { 0x18, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementMxAc_Tag_Offset", new Byte[] { 0x10, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementMxAc_Tag_Length", new Byte[] { 0x04, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementMxAc_Data_Offset", new Byte[] { 0x18, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementMxAc_Data_Length", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementMxAc_Tag", new Byte[] { 0x4d, 0x78, 0x41, 0x63 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementMxAc_Unknown", new Byte[] { 0x00, 0x00, 0x00, 0x00 });

                if(extra_info > 1)
                {
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_ChainOffset", new Byte[] { 0x18, 0x00, 0x00, 0x00 });
                }
                else
                {
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_ChainOffset", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                }
                
                SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_Tag_Offset", new Byte[] { 0x10, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_Tag_Length", new Byte[] { 0x04, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_Data_Offset", new Byte[] { 0x18, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_Data_Length", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_Tag", new Byte[] { 0x51, 0x46, 0x69, 0x64 });
                SMB2CreateRequest.Add("ExtraInfo_ChainElementQFid_Unknown", new Byte[] { 0x00, 0x00, 0x00, 0x00 });

                if(extra_info > 1)
                {
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_ChainOffset", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Tag_Offset", new Byte[] { 0x10, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Tag_Length", new Byte[] { 0x04, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Data_Offset", new Byte[] { 0x18, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Data_Length", new Byte[] { 0x20, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Tag", new Byte[] { 0x52, 0x71, 0x4c, 0x73 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Unknown", new Byte[] { 0x00, 0x00, 0x00, 0x00 });

                    if(extra_info == 2)
                    {
                        SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Data_Lease_Key", new Byte[] { 0x10, 0xb0, 0x1d, 0x02, 0xa0, 0xf8, 0xff, 0xff, 0x47, 0x78, 0x67, 0x02, 0x00, 0x00, 0x00, 0x00 });
                    }
                    else
                    {
                        SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Data_Lease_Key", new Byte[] { 0x10, 0x90, 0x64, 0x01, 0xa0, 0xf8, 0xff, 0xff, 0x47, 0x78, 0x67, 0x02, 0x00, 0x00, 0x00, 0x00 });
                    }

                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Data_Lease_State", new Byte[] { 0x07, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Data_Lease_Flags", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                    SMB2CreateRequest.Add("ExtraInfo_ChainElementRqLs_Data_Lease_Duration", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                }
            }
            return SMB2CreateRequest;
        }

        ~SMBClient()
        {
        }
    }
}