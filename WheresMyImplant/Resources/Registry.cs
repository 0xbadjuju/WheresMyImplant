using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WheresMyImplant
{
    class Reg
    {
        public static UIntPtr HKEY_LOCAL_MACHINE = new UIntPtr(0x80000002u);
        public static UIntPtr HKEY_CURRENT_USER = new UIntPtr(0x80000001u);

        public static Int32 KEY_QUERY_VALUE = 0x1;
        public static Int32 KEY_SET_VALUE = 0x2;
        public static Int32 KEY_CREATE_SUB_KEY = 0x4;
        public static Int32 KEY_ENUMERATE_SUB_KEYS = 0x8;
        public static Int32 KEY_NOTIFY = 0x10;
        public static Int32 KEY_CREATE_LINK = 0x20;
        public static Int32 KEY_WOW64_32KEY = 0x200;
        public static Int32 KEY_WOW64_64KEY = 0x100;
        public static Int32 KEY_WOW64_RES = 0x300;

        public enum RegWow64Options
        {
            None = 0,
            KEY_WOW64_64KEY = 0x0100,
            KEY_WOW64_32KEY = 0x0200
        }

        public enum RegistryRights
        {
            ReadKey = 131097,
            WriteKey = 131078
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public static object ReadRegKey(UIntPtr hive, String registryKey, String keyValue)
        {
            int size = 0;
            UIntPtr hKey = new UIntPtr();
            RegistryValueKind type = RegistryValueKind.Unknown;

            if (Advapi32.RegOpenKeyEx(HKEY_LOCAL_MACHINE, registryKey, 0, KEY_QUERY_VALUE, out hKey) == 0)
            {
                if (Advapi32.RegQueryValueEx(hKey, keyValue, 0, ref type, IntPtr.Zero, ref size) != 0)
                {
                    Console.WriteLine("[-] Error querying value '{0}\\{1}: 0x{2:x}", registryKey, "", Marshal.GetLastWin32Error());
                    return null;
                }

                IntPtr pResult = Marshal.AllocHGlobal(size);
                if (Advapi32.RegQueryValueEx(hKey, keyValue, 0, ref type, pResult, ref size) != 0)
                {
                    Console.WriteLine("[-] Error querying value '{0}\\{1}: 0x{2:x}", registryKey, "", Marshal.GetLastWin32Error());
                    return null;
                }

                else
                {
                    byte[] managedArray = new byte[size];
                    switch (type)
                    {
                        case RegistryValueKind.String:
                            return Marshal.PtrToStringAnsi(pResult);
                        case RegistryValueKind.ExpandString:
                            return Marshal.PtrToStringAnsi(pResult);
                        case RegistryValueKind.MultiString:
                            return Marshal.PtrToStringAnsi(pResult);
                        case RegistryValueKind.DWord:
                            return Marshal.ReadInt32(pResult);
                        case RegistryValueKind.QWord:
                            return Marshal.ReadInt64(pResult);
                        case RegistryValueKind.Unknown:
                            Marshal.Copy(pResult, managedArray, 0, size);
                            return managedArray;
                        case RegistryValueKind.Binary:
                            Marshal.Copy(pResult, managedArray, 0, size);
                            return managedArray;
                        default:
                            return null;
                    }
                }
            }
            else
            {
                Console.WriteLine("[-] Error opening registry key HKLM\\{0}: {1:x}", registryKey, Marshal.GetLastWin32Error());
                return null;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public static object ReadRegKeyInfo(UIntPtr hive, String registryKey)
        {
            int size = 0;
            UIntPtr hKey = new UIntPtr();
            
            if (Advapi32.RegOpenKeyEx(HKEY_LOCAL_MACHINE, registryKey, 0, KEY_QUERY_VALUE, out hKey) == 0)
            {
                UInt32 lpcSubkey;
                UInt32 lpcchMaxSubkeyLen;
                UInt32 lpcchMaxClassLen;
                UInt32 lpcValues;
                UInt32 lpcchMaxValueNameLen;
                UInt32 lpcbMaxValueLen;

                UInt32 lpcchClass = 0;
                Advapi32.RegQueryInfoKey(
                    hKey, 
                    null, 
                    ref lpcchClass, 
                    IntPtr.Zero, 
                    out lpcSubkey, 
                    out lpcchMaxSubkeyLen, 
                    out lpcchMaxClassLen, 
                    out lpcValues, 
                    out lpcchMaxValueNameLen, 
                    out lpcbMaxValueLen, 
                    IntPtr.Zero, 
                    IntPtr.Zero);
                if (lpcchClass == 0)
                {
                    Console.WriteLine("[-] Error querying value '{0}\\{1}: 0x{2:x}", registryKey, "", Marshal.GetLastWin32Error());
                    return null;
                }
                lpcchClass++; //I have no idea why this is nessessary... but it makes it work

                StringBuilder lpClass = new StringBuilder((Int32)lpcchClass);
                Advapi32.RegQueryInfoKey(
                    hKey, 
                    lpClass, 
                    ref lpcchClass, 
                    IntPtr.Zero, 
                    out lpcSubkey, 
                    out lpcchMaxSubkeyLen, 
                    out lpcchMaxClassLen, 
                    out lpcValues, 
                    out lpcchMaxValueNameLen, 
                    out lpcbMaxValueLen, 
                    IntPtr.Zero, 
                    IntPtr.Zero
                );
                IntPtr pResult = Marshal.AllocHGlobal(size);
                if (lpClass.ToString().Length == 0)
                {
                    Console.WriteLine("[-] Error querying value '{0}\\{1}: 0x{2:x}", registryKey, "", Marshal.GetLastWin32Error());
                    return null;
                }
                return lpClass.ToString();
            }
            else
            {
                Console.WriteLine("[-] Error opening registry key HKLM\\{0}: {1:x}", registryKey, Marshal.GetLastWin32Error());
                return null;
            }
        }
    }
}