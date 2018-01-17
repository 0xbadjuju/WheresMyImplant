using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32;

namespace WheresMyImplant
{
    class Advapi32
    {
        [DllImport("advapi32.dll", CharSet = CharSet.Auto)]
        public static extern int RegOpenKeyEx(
            UIntPtr hKey,
            String subKey,
            Int32 ulOptions,
            Int32 samDesired,
            out UIntPtr hkResult
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern uint RegQueryValueEx(
            UIntPtr hKey,
            String lpValueName,
            Int32 lpReserved,
            ref RegistryValueKind lpType,
            IntPtr lpData,
            ref Int32 lpcbData);

        [DllImport("advapi32.dll")]
        public static extern Int32 RegQueryInfoKey(
            UIntPtr hKey,
            StringBuilder lpClass,
            ref UInt32 lpcchClass,
            IntPtr lpReserved,
            out UInt32 lpcSubkey,
            out UInt32 lpcchMaxSubkeyLen,
            out UInt32 lpcchMaxClassLen,
            out UInt32 lpcValues,
            out UInt32 lpcchMaxValueNameLen,
            out UInt32 lpcbMaxValueLen,
            IntPtr lpSecurityDescriptor,
            IntPtr lpftLastWriteTime
        );
    }
}