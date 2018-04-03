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
        ////////////////////////////////////////////////////////////////////////////////
        // Token Functions
        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("Advapi32", SetLastError = true)]
        public static extern IntPtr OpenSCManager(String lpMachineName, String lpDatabaseName, Winsvc.dwSCManagerDesiredAccess dwDesiredAccess);

        [DllImport("Advapi32")]
        public static extern IntPtr CreateService(
            IntPtr hSCManager,
            String lpServiceName,
            String lpDisplayName,
            Winsvc.dwDesiredAccess dwDesiredAccess,
            Winsvc.dwServiceType dwServiceType,
            Winsvc.dwStartType dwStartType,
            Winsvc.dwErrorControl dwErrorControl,
            String lpBinaryPathName,
            String lpLoadOrderGroup,
            String lpdwTagId,
            String lpDependencies,
            String lpServiceStartName,
            String lpPassword
        );

        [DllImport("advapi32", SetLastError = true)]
        public static extern Boolean CloseServiceHandle(IntPtr hSCObject);

        [DllImport("advapi32", SetLastError = true)]
        public static extern IntPtr ControlService(IntPtr hService, Winsvc.dwControl dwControl, out Winsvc._SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32", SetLastError = true)]
        public static extern IntPtr ControlServiceEx(IntPtr hService, Winsvc.dwControl dwControl, Int32 dwInfoLevel, out Winsvc._SERVICE_STATUS lpServiceStatus);

        [DllImport("advapi32", SetLastError = true)]
        public static extern Boolean DeleteService(IntPtr hService);

        [DllImport("advapi32", SetLastError = true)]
        public static extern IntPtr OpenService(IntPtr hSCManager, String lpServiceName, Winsvc.dwDesiredAccess dwDesiredAccess);

        [DllImport("advapi32", SetLastError = true)]
        public static extern Boolean StartService(IntPtr hService, Int32 dwNumServiceArgs, String[] lpServiceArgVectors);

        ////////////////////////////////////////////////////////////////////////////////
        // Registry Functions
        ////////////////////////////////////////////////////////////////////////////////
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
            ref Int32 lpcbData
        );

        [DllImport("advapi32.dll", SetLastError = true)]
        public static extern UInt32 RegQueryValueEx(
            UIntPtr hKey,
            string lpValueName,
            int lpReserved,
            ref Int32 lpType,
            IntPtr lpData,
            ref int lpcbData
        );

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

        ////////////////////////////////////////////////////////////////////////////////
        // Vault Functions
        ////////////////////////////////////////////////////////////////////////////////
        [DllImport("advapi32.dll")]
        public static extern Boolean CredEnumerateW(
            String Filter,
            Int32 Flags,
            out Int32 Count,
            out IntPtr Credentials
        );

        [DllImport("advapi32.dll")]
        public static extern Boolean CredReadW(
            String target,
            Enums.CRED_TYPE type, 
            Int32 reservedFlag, 
            out IntPtr credentialPtr
        );

        [DllImport("advapi32.dll")]
        public static extern Boolean CredWriteW(
            ref Structs._CREDENTIAL userCredential, 
            UInt32 flags
        );

        [DllImport("advapi32.dll")]
        public static extern Boolean CredFree(
            IntPtr Buffer
        );
    }
} 