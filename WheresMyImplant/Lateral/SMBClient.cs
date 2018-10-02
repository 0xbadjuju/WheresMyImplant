using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace WheresMyImplant
{
    class SMBClient : SMB
    {
        protected Byte[] flags;
        protected UInt32 messageId = 0x1;
        protected Byte[] treeId = { 0x00, 0x00, 0x00, 0x00 };
        protected Byte[] sessionId = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        protected Byte[] sessionKeyLength = { 0x00, 0x00 };
        protected Boolean signing = false;
        protected Byte[] sessionKey;

        protected String version;//= "SMB2";

        protected Byte[] processId;

        protected Byte[] guidFileHandle = new Byte[16];

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public SMBClient() : base()
        {
            Int32 dwProcessId = Process.GetCurrentProcess().Id;
            String strProcessId = BitConverter.ToString(BitConverter.GetBytes(dwProcessId));
            processId = strProcessId.Split('-').Select(i => (Byte)Convert.ToInt16(i,16)).ToArray();
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

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
                return true;
            else
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

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void IoctlRequest(String share)
        {
            treeId = new Byte[] { 0x01, 0x00, 0x00, 0x00 };

            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x0b, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2IoctlRequest ioctlRequest = new SMB2IoctlRequest();
            ioctlRequest.SetFileName(share);
            Byte[] bData = ioctlRequest.GetRequest();

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
            treeId = new Byte[] { 0x00, 0x00, 0x00, 0x00 };
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal virtual Boolean CreateRequest(String folder)
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
            if (!String.IsNullOrEmpty(folder))
                createRequest.SetFileName(folder);
            createRequest.SetExtraInfo(1, 0);
            Byte[] bData = createRequest.GetRequest();

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

            Boolean result = GetStatus(recieve.Skip(12).Take(4).ToArray());
            if (result)
            { 
                guidFileHandle = recieve.Skip(0x0084).Take(16).ToArray();
                return true;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal virtual Boolean InfoRequest()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x10, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2GetInfo getInfo = new SMB2GetInfo();
            getInfo.SetClass(new Byte[] { 0x02 });
            getInfo.SetInfoLevel(new Byte[] { 0x05 });
            getInfo.SetMaxResponseSize(new Byte[] { 0x50, 0x00, 0x00, 0x00 });
            getInfo.SetGUIDHandleFile(recieve.Skip(132).Take(16).ToArray());
            Byte[] bData = getInfo.GetRequest();

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

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal virtual Boolean FindRequest(String folder)
        {
            treeId = recieve.Skip(40).Take(4).ToArray();

            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x05, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2CreateRequest createRequest = new SMB2CreateRequest();
            if (!String.IsNullOrEmpty(folder))
                createRequest.SetFileName(folder);
            createRequest.SetExtraInfo(1, 0);
            createRequest.SetAccessMask(new Byte[] { 0x81, 0x00, 0x10, 0x00 });
            createRequest.SetShareAccess(new Byte[] { 0x07, 0x00, 0x00, 0x00 });
            
            Byte[] bData = createRequest.GetRequest();

            header.SetChainOffset(bData.Length);
            if (signing)
            {
                header.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            else
            {
                header.SetFlags(new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader = header.GetHeader();
            

            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header2 = new SMB2Header();
            header2.SetCommand(new Byte[] { 0x0e, 0x00 });
            header2.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header2.SetMessageID(++messageId);
            header2.SetProcessID(processId);
            header2.SetTreeId(treeId);
            header2.SetSessionID(sessionId);
            header2.SetChainOffset(new Byte[] { 0x68, 0x00, 0x00, 0x00 });

            SMB2FindFileRequestFile requestFile = new SMB2FindFileRequestFile();
            requestFile.SetPadding(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            Byte[] bData2 = requestFile.GetRequest();

            if (signing)
            {
                header2.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header2.SetSignature(sessionKey, ref bData2);
            }
            else
            {
                header2.SetFlags(new Byte[] { 0x04, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader2 = header2.GetHeader();


            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header3 = new SMB2Header();
            header3.SetCommand(new Byte[] { 0x0e, 0x00 });
            header3.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header3.SetMessageID(++messageId);
            header3.SetProcessID(processId);
            header3.SetTreeId(treeId);
            header3.SetSessionID(sessionId);

            SMB2FindFileRequestFile requestFile2 = new SMB2FindFileRequestFile();
            requestFile2.SetOutputBufferLength(new Byte[] { 0x80, 0x00, 0x00, 0x00 });
            Byte[] bData3 = requestFile2.GetRequest();

            if (signing)
            {
                header3.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header3.SetSignature(sessionKey, ref bData3);
            }
            else
            {
                header3.SetFlags(new Byte[] { 0x04, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader3 = header3.GetHeader();


            ////////////////////////////////////////////////////////////////////////////////
            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length + bHeader2.Length + bHeader3.Length);
            sessionService.SetDataLength(bData.Length + bData2.Length + bData3.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            bSend = Misc.Combine(bSend, Misc.Combine(bHeader2, bData2));
            bSend = Misc.Combine(bSend, Misc.Combine(bHeader3, bData3));
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
                return true;
            else
                return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal virtual Boolean ReadRequest()
        {
            treeId = recieve.Skip(40).Take(4).ToArray();

            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x05, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2ReadRequest readRequest = new SMB2ReadRequest();
            readRequest.SetGuidHandleFile(guidFileHandle);

            Byte[] bData = readRequest.GetRequest();

            header.SetChainOffset(bData.Length);
            if (signing)
            {
                header.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            else
            {
                header.SetFlags(new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader = header.GetHeader();


            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header2 = new SMB2Header();
            header2.SetCommand(new Byte[] { 0x0e, 0x00 });
            header2.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header2.SetMessageID(++messageId);
            header2.SetProcessID(processId);
            header2.SetTreeId(treeId);
            header2.SetSessionID(sessionId);
            header2.SetChainOffset(new Byte[] { 0x68, 0x00, 0x00, 0x00 });

            SMB2FindFileRequestFile requestFile = new SMB2FindFileRequestFile();
            requestFile.SetPadding(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            Byte[] bData2 = requestFile.GetRequest();

            if (signing)
            {
                header2.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header2.SetSignature(sessionKey, ref bData2);
            }
            else
            {
                header2.SetFlags(new Byte[] { 0x04, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader2 = header2.GetHeader();


            ////////////////////////////////////////////////////////////////////////////////
            SMB2Header header3 = new SMB2Header();
            header3.SetCommand(new Byte[] { 0x0e, 0x00 });
            header3.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header3.SetMessageID(++messageId);
            header3.SetProcessID(processId);
            header3.SetTreeId(treeId);
            header3.SetSessionID(sessionId);

            SMB2FindFileRequestFile requestFile2 = new SMB2FindFileRequestFile();
            requestFile2.SetOutputBufferLength(new Byte[] { 0x80, 0x00, 0x00, 0x00 });
            Byte[] bData3 = requestFile2.GetRequest();

            if (signing)
            {
                header3.SetFlags(new Byte[] { 0x0c, 0x00, 0x00, 0x00 });
                header3.SetSignature(sessionKey, ref bData3);
            }
            else
            {
                header3.SetFlags(new Byte[] { 0x04, 0x00, 0x00, 0x00 });
            }
            Byte[] bHeader3 = header3.GetHeader();


            ////////////////////////////////////////////////////////////////////////////////
            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length + bHeader2.Length + bHeader3.Length);
            sessionService.SetDataLength(bData.Length + bData2.Length + bData3.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            bSend = Misc.Combine(bSend, Misc.Combine(bHeader2, bData2));
            bSend = Misc.Combine(bSend, Misc.Combine(bHeader3, bData3));
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
                return true;
            else
                return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void ParseDirectoryContents()
        {
            String directory = BitConverter.ToString(recieve).Replace("-", "");
            Int32 index = directory.Substring(10).IndexOf("FE534D42") + 154;
            Int32 offset = 0;
            Int32 nextOffset = 0;
            WriteOutput("");
            WriteOutput(String.Format("{0,5}{1,15}{2,20}{3,20}\t{4}", "Mode", "File Size", "Created", "Modified", "File Name"));
            WriteOutput(String.Format("{0,5}{1,15}{2,20}{3,20}\t{4}", "----", "---------", "-------", "--------", "---------"));
            do
            {
                Int32 start = (index / 2 + offset);
                nextOffset = BitConverter.ToInt32(recieve.Skip(start).Take(4).ToArray(), 0);
                UInt32 dwFileLength = BitConverter.ToUInt32(recieve.Skip(start + 40).Take(7).ToArray(), 0);
                String fileLength = 0 == dwFileLength ? String.Empty : dwFileLength.ToString();

                String attributes = Convert.ToString(recieve[start + 56], 2).PadLeft(16, '0');
                String d = attributes.Substring(11, 1) == "1" ? "d" : "-";
                String a = attributes.Substring(10, 1) == "1" ? "a" : "-";
                String r = attributes.Substring(15, 1) == "1" ? "r" : "-";
                String h = attributes.Substring(14, 1) == "1" ? "h" : "-";
                String s = attributes.Substring(13, 1) == "1" ? "s" : "-";

                DateTime create = DateTime.FromFileTime(BitConverter.ToInt64(recieve.Skip(start + 8).Take(8).ToArray(), 0));
                DateTime modify = DateTime.FromFileTime(BitConverter.ToInt64(recieve.Skip(start + 24).Take(8).ToArray(), 0));
                Int32 filenameLength = BitConverter.ToInt32(recieve.Skip(start + 60).Take(4).ToArray(), 0);
                Byte[] filename_unicode = recieve.Skip(start + 104).Take(filenameLength).ToArray();
                String filename = Encoding.Unicode.GetString(filename_unicode);
                WriteOutput(String.Format(
                    "{0,5}{1,15}{2,20}{3,20}\t{4}", 
                    d + a + r + h + s,  
                    fileLength, 
                    create.ToString("MM/dd/yyyy HH:mm"), 
                    modify.ToString("MM/dd/yyyy HH:mm"), 
                    filename));
                offset += nextOffset;
            }
            while (nextOffset != 0);
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

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean LogoffRequest()
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

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        ~SMBClient()
        {
            Dispose();
        }
    }
}