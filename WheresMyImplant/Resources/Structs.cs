using System;
using System.Runtime.InteropServices;

using word = System.UInt16;
using dword = System.UInt32;
using qword = System.UInt64;

namespace WheresMyImplant
{
    public class Structs
    {
        //https://msdn.microsoft.com/en-us/library/ms809762.aspx
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _IMAGE_IMPORT_DIRECTORY
        {
            public dword RvaImportLookupTable;
            public dword TimeDateStamp;
            public dword ForwarderChain;
            public dword RvaModuleName;
            public dword RvaImportAddressTable;
        }

        [Flags]
        public enum IMAGE_DATA_DIRECTORY_OPTIONS : int
        {
            ExportTable = 0,
            ImportTable = 1,
            ResourceTable = 2,
            ExceptionTable = 3,
            CertificateTable = 4,
            BaseRelocationTable = 5,
            Debug = 6,
            Architecture = 7,
            GlobalPtr = 8,
            TLSTable = 9,
            LoadConfigTable = 10,
            BoundImport = 11,
            IAT = 12,
            DelayImportDescriptor = 13,
            CLRRuntimeHeader = 14,
            Reserved = 15
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct IMAGE_BASE_RELOCATION
        {
            public dword VirtualAdress;
            public dword SizeOfBlock;
        }

		        ////////////////////////////////////////////////////////////////////////////////
        // Tokens
        ////////////////////////////////////////////////////////////////////////////////
        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms686331(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        internal struct _STARTUPINFO
        {
            public UInt32 cb;
            public String lpReserved;
            public String lpDesktop;
            public String lpTitle;
            public UInt32 dwX;
            public UInt32 dwY;
            public UInt32 dwXSize;
            public UInt32 dwYSize;
            public UInt32 dwXCountChars;
            public UInt32 dwYCountChars;
            public UInt32 dwFillAttribute;
            public UInt32 dwFlags;
            public UInt16 wShowWindow;
            public UInt16 cbReserved2;
            public IntPtr lpReserved2;
            public IntPtr hStdInput;
            public IntPtr hStdOutput;
            public IntPtr hStdError;
        };

        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms686331(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        internal struct _STARTUPINFOEX
        {
            _STARTUPINFO StartupInfo;
            // PPROC_THREAD_ATTRIBUTE_LIST lpAttributeList;
        };

        //https://msdn.microsoft.com/en-us/library/windows/desktop/ms684873(v=vs.85).aspx
        [StructLayout(LayoutKind.Sequential)]
        internal struct _PROCESS_INFORMATION
        {
            public IntPtr hProcess;
            public IntPtr hThread;
            public UInt32 dwProcessId;
            public UInt32 dwThreadId;
        };

        //lpTokenAttributes
        [StructLayout(LayoutKind.Sequential)]
        internal struct _SECURITY_ATTRIBUTES
        {
            UInt32 nLength;
            IntPtr lpSecurityDescriptor;
            Boolean bInheritHandle;
        };

        [StructLayout(LayoutKind.Sequential)]
        internal struct _TOKEN_PRIVILEGES
        {
            public UInt32 PrivilegeCount;
            public _LUID_AND_ATTRIBUTES Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct _TOKEN_PRIVILEGES_ARRAY
        {
            public UInt32 PrivilegeCount;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 30)]
            public _LUID_AND_ATTRIBUTES[] Privileges;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct _LUID_AND_ATTRIBUTES
        {
            public _LUID Luid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct _LUID
        {
            public UInt32 LowPart;
            public UInt32 HighPart;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SidIdentifierAuthority
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.I1)]
            public byte[] Value;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct SID_AND_ATTRIBUTES
        {
            public IntPtr Sid;
            public UInt32 Attributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct TOKEN_MANDATORY_LABEL
        {
            public SID_AND_ATTRIBUTES Label;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct CacheData
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] userNameLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] domainNameLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] effectiveNameLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] fullNameLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] logonScriptLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] profilePathLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] homeDirectoryLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] homeDirectoryDriveLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] userId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] primaryGroupId;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] groupCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] logonDomainNameLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] logonDomainIdLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] lastAccess;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] lastAccessTime;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] revision;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] sidCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] valid;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] iterationCount;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] sifLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public Byte[] logonPackage;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] dnsDomainNameLength;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public Byte[] upnLength;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public Byte[] challenge;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct _CREDENTIAL
        {
            public Enums.CRED_FLAGS Flags;
            public Enums.CRED_TYPE Type;
            public IntPtr TargetName;
            public IntPtr Comment;
            public FILETIME LastWritten;
            public UInt32 CredentialBlobSize;
            public IntPtr CredentialBlob;
            public Enums.CRED_PERSIST Persist;
            public UInt32 AttributeCount;
            public IntPtr Attributes;
            public IntPtr TargetAlias;
            public IntPtr UserName;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _GUID {
            public Int32 Data1;
            public Int16 Data2;
            public Int16 Data3;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public Byte[] Data4;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct _VAULT_ITEM_7
        {
            public Guid SchemaId;
            public IntPtr FriendlyName;
            public IntPtr Resource;
            public IntPtr Identity;
            public IntPtr Authenticator;
            public Int64 LastWritten;
            public Int32 Flags;
            public Int32 PropertiesCount;
            public IntPtr Properties;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct _VAULT_ITEM_8
        {
            public Guid SchemaId;
            public IntPtr FriendlyName;
            public IntPtr Resource;
            public IntPtr Identity;
            public IntPtr Authenticator;
            public IntPtr PackageSid;
            public Int64 LastWritten;
            public Int32 Flags;
            public Int32 PropertiesCount;
            public IntPtr Properties;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct _VAULT_ITEM_DATA
        {
            public Int32 SchemaElementId;
            public Int32 unknown1;
            public Enums._VAULT_ELEMENT_TYPE Type;
            public Int32 unknown2;
            //public Object data;
        }

        internal struct _CREDENTIAL_ATTRIBUTE
        {
            String Keyword;
            Int32 Flags;
            Int32 ValueSize;
            IntPtr Value;
        }
    }
}