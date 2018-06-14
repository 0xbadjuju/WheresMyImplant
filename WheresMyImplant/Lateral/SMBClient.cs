using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace WheresMyImplant
{
    class SMBClient : Base, IDisposable
    {
        Byte[] flags;
        UInt32 messageId = 0x1;
        Byte[] treeId = { 0x00, 0x00, 0x00, 0x00 };
        Byte[] sessionId = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        Byte[] sessionKeyLength = { 0x00, 0x00 };
        Boolean signing = false;

        String version = "SMB2";
        String stage;
        String system;

        Byte[] processId;
        TcpClient smbClient;
        NetworkStream streamSocket;

        Byte[] recieve = new Byte[81920];

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public SMBClient()
        {
            smbClient = new TcpClient();
            Int32 dwProcessId = Process.GetCurrentProcess().Id;
            String strProcessId = BitConverter.ToString(BitConverter.GetBytes(dwProcessId));
            processId = strProcessId.Split('-').Select(i => (Byte)Convert.ToInt16(i,16)).ToArray();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create Network Layer Connection
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean Connect(String system)
        {
            this.system = system;
            smbClient.Client.ReceiveTimeout = 30000;

            try
            {
                smbClient.Connect(system, 445);
            }
            catch (Exception)
            {
                return false;
            }

            if (!smbClient.Connected)
            {
                return false;
            }
            streamSocket = smbClient.GetStream();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void NegotiateSMB()
        {
            OrderedDictionary header = GetSMBPacketHeader(
                new Byte[] { 0x72 }, 
                new Byte[] { 0x18 }, 
                new Byte[] { 0x01, 0x48 },
                new Byte[] { 0xff, 0xff }, 
                processId.Take(2).ToArray(),
                new Byte[] { 0x00, 0x00 }
            );
            Byte[] bHeader = OrderedDictionaryToBytes(header);

            OrderedDictionary data = SMBNegotiateProtocolRequest();
            Byte[] bData = OrderedDictionaryToBytes(data);

            OrderedDictionary sessionService = NetBIOSSessionService(bHeader.Length, bData.Length);
            Byte[] bSessionService = OrderedDictionaryToBytes(sessionService);

            Byte[] send = Misc.Combine(bSessionService, bHeader);
            send = Misc.Combine(send, bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            Byte[] recieve = new Byte[81920];
            streamSocket.Read(recieve, 0, recieve.Length);

            Byte[] response = recieve.Skip(5).Take(4).ToArray();
            if (response == new Byte[] { 0xff, 0x53, 0x4d, 0x42 })
            {
                WriteOutputBad("SMB1 is not supported");
                return;
            }

            version = "SMB2";

            Byte[] keyLength = { 0x00, 0x00 };

            if (recieve.Skip(71).Take(1).ToArray() == new Byte[] { 0x03 })
            {
                signing = true;
                flags = new Byte[] { 0x15, 0x82, 0x08, 0xa0 };
            }
            else
            {
                signing = false;
                flags = new Byte[] { 0x05, 0x80, 0x08, 0xa0 };
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void NegotiateSMB2()
        {   
            OrderedDictionary header = GetSMB2PacketHeader(new Byte[] { 0x00,0x00 }, new Byte[] { 0x00, 0x00 }, BitConverter.GetBytes(messageId), treeId, sessionId);
            header["ProcessID"] = processId;
            Byte[] bHeader = OrderedDictionaryToBytes(header);
            
            OrderedDictionary data = SMB2NegotiateProtocolRequest();
            Byte[] bData = OrderedDictionaryToBytes(data);

            OrderedDictionary session = NetBIOSSessionService(bHeader.Length, bData.Length);
            Byte[] bSession = OrderedDictionaryToBytes(session);

            Byte[] send = Misc.Combine(bSession, bHeader);
            send = Misc.Combine(send, bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);
        }

        internal void NTLMSSPNegotiate()
        {
            messageId++;
            OrderedDictionary header = GetSMB2PacketHeader(
                new Byte[] { 0x01, 0x00 }, 
                new Byte[] { 0x1f, 0x00 }, 
                BitConverter.GetBytes(messageId), 
                treeId, 
                sessionId);
            header["ProcessID"] = processId;
            Byte[] bHeader = OrderedDictionaryToBytes(header);

            OrderedDictionary negotiate = GetNTLMSSPNegotiate();
            Byte[] bNegotiate = OrderedDictionaryToBytes(negotiate);

            OrderedDictionary data = GetSMB2SessionSetupRequest(bNegotiate);
            Byte[] bData = OrderedDictionaryToBytes(data);

            OrderedDictionary session = NetBIOSSessionService(bHeader.Length, bData.Length);
            Byte[] bSession = OrderedDictionaryToBytes(session);

            Byte[] send = Misc.Combine(bSession, bHeader);
            send = Misc.Combine(send, bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);
        }

        internal Boolean Authenticate(String domain, String username, String hash)
        {
            String NTLMSSP = BitConverter.ToString(recieve).Replace("-", "");
            Int32 index = NTLMSSP.IndexOf("4E544C4D53535000") / 2;

            UInt16 wDomain = DataLength(index + 12, recieve);
            UInt16 wtarget = DataLength(index + 40, recieve);

            Byte[] sessionId = recieve.Skip(44).Take(8).ToArray();
            Byte[] bServerChallenge = recieve.Skip(index + 24).Take(8).ToArray();
            Int32 start = index + 56 + wDomain;
            Int32 end = index + 55 + wDomain + wtarget;
            Byte[] details = recieve.Skip(start).Take(end - start + 1).ToArray();
            Byte[] bTime = details.Skip(details.Length - 12).Take(8).ToArray();

            Int32 j = 0;
            Byte[] bHash = new Byte[hash.Length / 2];
            for (Int32 i = 0; i < hash.Length; i += 2)
            {
                bHash[j++] = (Byte)((Char)Convert.ToInt16(hash.Substring(i, 2),16));
            }

            Byte[] bHostname = Encoding.Unicode.GetBytes(Environment.MachineName);
            Byte[] hostnameLength = BitConverter.GetBytes(bHostname.Length).Take(2).ToArray();

            Byte[] bDomain = Encoding.Unicode.GetBytes(domain);
            Byte[] domainLength = BitConverter.GetBytes(bDomain.Length).Take(2).ToArray();

            Byte[] bUsername = Encoding.Unicode.GetBytes(username);
            Byte[] usernameLength = BitConverter.GetBytes(bUsername.Length).Take(2).ToArray();

            Byte[] domainOffset = { 0x40, 0x00, 0x00, 0x00 };
            Byte[] usernameOffset = BitConverter.GetBytes(bDomain.Length + 64);
            Byte[] hostnameOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + 64);
            Byte[] lmOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + bHostname.Length + 64);
            Byte[] ntOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + bHostname.Length + 88);

            String usernameTarget = username.ToUpper();
            Byte[] bUsernameTarget = Encoding.Unicode.GetBytes(usernameTarget);
            bUsernameTarget = Misc.Combine(bUsernameTarget, bDomain);

            Byte[] NetNTLMv2Hash;
            using (HMACMD5 hmac = new HMACMD5())
            {
                hmac.Key = bHash;
                NetNTLMv2Hash = hmac.ComputeHash(bUsernameTarget);
            }

            Byte[] bClientChallenge = new Byte[8];
            Random random = new Random();
            for (Int32 i = 0; i < 8; i++)
            {
                bClientChallenge[i] = (Byte)random.Next(0, 255);
            }

            Byte[] blob = Misc.Combine(new Byte[] { 0x01, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }, bTime);
            blob = Misc.Combine(blob, bClientChallenge);
            blob = Misc.Combine(blob, new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            blob = Misc.Combine(blob, details);
            blob = Misc.Combine(blob, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            Byte[] bServerChallengeAndBlob = Misc.Combine(bServerChallenge, blob);
            Byte[] NetNTLMv2Response;
            using (HMACMD5 hmac = new HMACMD5())
            {
                hmac.Key = NetNTLMv2Hash;
                NetNTLMv2Response = hmac.ComputeHash(bServerChallengeAndBlob);
            }

            NetNTLMv2Response = Misc.Combine(NetNTLMv2Response, blob);
            Byte[] NetNTLMv2ResponseLength = BitConverter.GetBytes(NetNTLMv2Response.Length).Take(2).ToArray();

            Byte[] sessionKeyOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + bHostname.Length + NetNTLMv2Response.Length + 88);

            Byte[] NetNTLMSSPResponse = { 0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00, 0x03, 0x00, 0x00, 0x00 };
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, new Byte[] { 0x18, 0x00 });
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, new Byte[] { 0x18, 0x00 });
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, lmOffset);

            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, NetNTLMv2ResponseLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, NetNTLMv2ResponseLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, ntOffset);

            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, domainLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, domainLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, domainOffset);

            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, usernameLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, usernameLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, usernameOffset);

            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, hostnameLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, hostnameLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, hostnameOffset);

            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, sessionKeyLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, sessionKeyLength);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, sessionKeyOffset);

            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, flags);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, bDomain);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, bUsername);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, bHostname);
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            NetNTLMSSPResponse = Misc.Combine(NetNTLMSSPResponse, NetNTLMv2Response);
            messageId += 1;

            OrderedDictionary header = GetSMB2PacketHeader(new Byte[] { 0x01, 0x00 }, new Byte[] { 0x1f, 0x00 }, BitConverter.GetBytes(messageId), treeId, sessionId);
            header["ProcessID"] = processId;
            Byte[] bHeader = OrderedDictionaryToBytes(header);

            Byte[] bNTLMSSPAuth = OrderedDictionaryToBytes(GetNTLMSSPAuth(NetNTLMSSPResponse));
            Byte[] bData = OrderedDictionaryToBytes(GetSMB2SessionSetupRequest(bNTLMSSPAuth));
            Byte[] bSession = OrderedDictionaryToBytes(NetBIOSSessionService(bHeader.Length, bData.Length));

            Byte[] send = Misc.Combine(Misc.Combine(bSession, bHeader), bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (recieve.Skip(12).Take(4).ToArray().SequenceEqual(new Byte[] { 0x00, 0x00, 0x00, 0x00 }))
            {
                WriteOutputGood(String.Format("{0} Login Successful to {1}", username, system));
                return true;
            }

            WriteOutputBad(String.Format("{0} Login Failed to {1}", username, system));
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private UInt16 DataLength(Int32 start, Byte[] extract_data)
        {
            return BitConverter.ToUInt16(extract_data.Skip(start).Take(2).ToArray(), 0);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private Byte[] OrderedDictionaryToBytes(OrderedDictionary dictionary)
        {
            Byte[] output = new Byte[0];
            
                foreach (Object field in dictionary.Values)
                {
                    try
                    {
                        output = Misc.Combine(output, (Byte[])field);
                    }
                    catch (InvalidCastException error)
                    {
                        Console.WriteLine(error);
                        Console.WriteLine(field);
                        return null;
                    }
                }
            return output;
        }

        private OrderedDictionary GetSMBPacketHeader(
            Byte[]command, 
            Byte[]flags,
            Byte[]flags2,
            Byte[]tree_ID,
            Byte[]process_ID,
            Byte[]user_ID
            ){
            OrderedDictionary SMBHeader = new OrderedDictionary();
            SMBHeader.Add("Protocol", new Byte[] { 0xff, 0x53, 0x4d, 0x42 });
            SMBHeader.Add("Command", command);
            SMBHeader.Add("ErrorClass", new Byte[] { 0x00 });
            SMBHeader.Add("Reserved", new Byte[] { 0x00 });
            SMBHeader.Add("ErrorCode",new Byte[] { 0x00, 0x00 });
            SMBHeader.Add("Flags", flags);
            SMBHeader.Add("Flags2", flags2);
            SMBHeader.Add("ProcessIDHigh", new Byte[] { 0x00, 0x00 });
            SMBHeader.Add("Signature",new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            SMBHeader.Add("Reserved2",new Byte[] { 0x00, 0x00 });
            SMBHeader.Add("TreeID", tree_ID);
            SMBHeader.Add("ProcessID", process_ID);
            SMBHeader.Add("UserID", user_ID);
            SMBHeader.Add("MultiplexID", new Byte[] {0x00, 0x00});
            return SMBHeader;
        }

        private OrderedDictionary SMBNegotiateProtocolRequest()
        {
            
            Byte[] lmBytes = new Byte[] { 0x4e, 0x54, 0x20, 0x4c, 0x4d, 0x20, 0x30, 0x2e, 0x31, 0x32, 0x00 };
            Byte[] twoBytes = new Byte[] { 0x53, 0x4d, 0x42, 0x20, 0x32, 0x2e, 0x30, 0x30, 0x32, 0x00 };
            Byte[] threeBytes = new Byte[] { 0x53, 0x4d, 0x42, 0x20, 0x32, 0x2e, 0x3f, 0x3f, 0x3f, 0x00 };

            Byte[] byte_count = BitConverter.GetBytes((short)(lmBytes.Length + twoBytes.Length + threeBytes.Length + 3));
            OrderedDictionary SMBNegotiateProtocolRequest = new OrderedDictionary();
            SMBNegotiateProtocolRequest.Add("WordCount", new Byte[] { 0x00 });
            SMBNegotiateProtocolRequest.Add("ByteCount", byte_count);
            SMBNegotiateProtocolRequest.Add("RequestedDialects_Dialect_BufferFormatLM", new Byte[] { 0x02 });
            SMBNegotiateProtocolRequest.Add("RequestedDialects_Dialect_Name", lmBytes);
            SMBNegotiateProtocolRequest.Add("RequestedDialects_Dialect_BufferFormat2",new Byte[] { 0x02 });
            SMBNegotiateProtocolRequest.Add("RequestedDialects_Dialect_Name2", twoBytes);
            SMBNegotiateProtocolRequest.Add("RequestedDialects_Dialect_BufferFormat3", new Byte[] {0x02 });
            SMBNegotiateProtocolRequest.Add("RequestedDialects_Dialect_Name3", threeBytes);

            return SMBNegotiateProtocolRequest;
        }

        private OrderedDictionary NetBIOSSessionService(Int32 header_length, Int32 data_length)
        {
            Byte[] netbios_session_service_length = BitConverter.GetBytes(header_length + data_length);
            netbios_session_service_length = netbios_session_service_length.Take(3).ToArray();
            Array.Reverse(netbios_session_service_length);
            Console.WriteLine(BitConverter.ToString(netbios_session_service_length));
            OrderedDictionary NetBIOSSessionService = new OrderedDictionary();
            NetBIOSSessionService.Add("NetBIOSSessionService_Message_Type", new Byte[] { 0x00 });
            NetBIOSSessionService.Add("NetBIOSSessionService_Length", netbios_session_service_length);

            return NetBIOSSessionService;
        }

        private OrderedDictionary GetSMB2PacketHeader(
            Byte[] packetCommand, 
            Byte[] creditRequest, 
            Byte[] messageID, 
            Byte[] treeID, 
            Byte[] sessionID)
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

        private OrderedDictionary SMB2NegotiateProtocolRequest()
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

        private OrderedDictionary GetSMB2SessionSetupRequest(Byte[] security_blob)
        {
            Byte[] security_blob_length = System.BitConverter.GetBytes(security_blob.Length);
            security_blob_length = security_blob_length.Take(2).ToArray();

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

        private OrderedDictionary SMB2TreeConnectRequest(Byte[] path)
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
            SMB2IoctlRequest.Add("InData_Length", file_name_length);
            SMB2IoctlRequest.Add("MaxIoctlInSize", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("OutData_Offset", new Byte[] { 0x78, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("OutData_Length", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("MaxIoctlOutSize", new Byte[] { 0x00, 0x10, 0x00, 0x00 });
            SMB2IoctlRequest.Add("Flags", new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("Unknown", new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            SMB2IoctlRequest.Add("InData_MaxReferralLevel", new Byte[] { 0x04, 0x00 });
            SMB2IoctlRequest.Add("InData_FileName", file_name);

            return SMB2IoctlRequest;
        }

        private OrderedDictionary SMB2CreateRequest(Byte[] file_name, Int32 extra_info, Int64 allocation_size)
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

        public OrderedDictionary GetNTLMSSPNegotiate()
        {
            Byte[] NTLMSSP_length = BitConverter.GetBytes(32 + version.Length);
            NTLMSSP_length = new Byte[] { NTLMSSP_length[0] };
            Byte[] ASN_length_1 = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSP_length[0]) + 32) };
            Byte[] ASN_length_2 = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSP_length[0]) + 22) };
            Byte[] ASN_length_3 = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSP_length[0]) + 20) };
            Byte[] ASN_length_4 = new Byte[] { (Byte)(Convert.ToInt16(NTLMSSP_length[0]) + 2) };

            OrderedDictionary NTLMSSPNegotiate = new OrderedDictionary();
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_InitialContextTokenID", new Byte[] { 0x60 }); // the ASN.1 key names are likely not all correct
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_InitialcontextTokenLength", ASN_length_1);
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_ThisMechID", new Byte[] { 0x06 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_ThisMechLength", new Byte[] { 0x06 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_OID", new Byte[] { 0x2b, 0x06, 0x01, 0x05, 0x05, 0x02 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_InnerContextTokenID", new Byte[] { 0xa0 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_InnerContextTokenLength", ASN_length_2);
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_InnerContextTokenID2", new Byte[] { 0x30 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_InnerContextTokenLength2", ASN_length_3);
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTypesID", new Byte[] { 0xa0 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTypesLength", new Byte[] { 0x0e });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTypesID2", new Byte[] { 0x30 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTypesLength2", new Byte[] { 0x0c });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTypesID3", new Byte[] { 0x06 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTypesLength3", new Byte[] { 0x0a });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechType", new Byte[] { 0x2b, 0x06, 0x01, 0x04, 0x01, 0x82, 0x37, 0x02, 0x02, 0x0a });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTokenID", new Byte[] { 0xa2 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MechTokenLength", ASN_length_4);
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_NTLMSSPID", new Byte[] { 0x04 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_NTLMSSPLength", NTLMSSP_length);
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_Identifier", new Byte[] { 0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_MessageType", new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_NegotiateFlags", flags);
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_CallingWorkstationDomain", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            NTLMSSPNegotiate.Add("NTLMSSPNegotiate_CallingWorkstationName", new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });

            if(version.Length > 0)
            {
                NTLMSSPNegotiate.Add("NTLMSSPNegotiate_Version", Encoding.ASCII.GetBytes(version));
            }

            return NTLMSSPNegotiate;
        }

        private OrderedDictionary GetNTLMSSPAuth(Byte[] NetNTLMResponse)
        {
            Byte[] NTLMSSP_length = BitConverter.GetBytes(NetNTLMResponse.Length);
            NTLMSSP_length = NTLMSSP_length.Take(2).ToArray(); 
            Array.Reverse(NTLMSSP_length);

            Byte[] ASN_length_1 = BitConverter.GetBytes(NetNTLMResponse.Length + 12);
            ASN_length_1 = ASN_length_1.Take(2).ToArray();
            Array.Reverse(ASN_length_1);

            Byte[] ASN_length_2 = BitConverter.GetBytes(NetNTLMResponse.Length + 8);
            ASN_length_2 = ASN_length_2.Take(2).ToArray();
            Array.Reverse(ASN_length_2);

            Byte[] ASN_length_3 = BitConverter.GetBytes(NetNTLMResponse.Length + 4);
            ASN_length_3 = ASN_length_3.Take(2).ToArray();
            Array.Reverse(ASN_length_3);

            OrderedDictionary NTLMSSPAuth = new OrderedDictionary();
            NTLMSSPAuth.Add("NTLMSSPAuth_ASNID", new Byte[] { 0xa1, 0x82 });
            NTLMSSPAuth.Add("NTLMSSPAuth_ASNLength", ASN_length_1);
            NTLMSSPAuth.Add("NTLMSSPAuth_ASNID2", new Byte[] { 0x30, 0x82 });
            NTLMSSPAuth.Add("NTLMSSPAuth_ASNLength2", ASN_length_2);
            NTLMSSPAuth.Add("NTLMSSPAuth_ASNID3", new Byte[] {0xa2, 0x82 });
            NTLMSSPAuth.Add("NTLMSSPAuth_ASNLength3", ASN_length_3);
            NTLMSSPAuth.Add("NTLMSSPAuth_NTLMSSPID", new Byte[] {0x04, 0x82} );
            NTLMSSPAuth.Add("NTLMSSPAuth_NTLMSSPLength", NTLMSSP_length);
            NTLMSSPAuth.Add("NTLMSSPAuth_NTLMResponse", NetNTLMResponse);

            return NTLMSSPAuth;
        }

        public void Dispose()
        {
            smbClient.Close();
            streamSocket.Close();
        }

        ~SMBClient()
        {
        }
    }
}