using System;
using System.Runtime.InteropServices;

using word = System.UInt16;
using dword = System.UInt32;
using qword = System.UInt64;

namespace WheresMyImplant
{
    public class Structs
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _IMAGE_SECTION_HEADER
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public char[] Name;
            public dword VirtualSize;
            public dword VirtualAddress;
            public dword SizeOfRawData;
            public dword PointerToRawData;
            public dword PointerToRelocations;
            public dword PointerToLinenumbers;
            public word NumberOfRelocations;
            public word NumberOfLinenumbers;
            public dword Characteristics;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _IMAGE_FILE_HEADER
        {
            public word Machine;
            public word NumberOfSections;
            public dword TimeDateStamp;
            public dword PointerToSymbolTable;
            public dword NumberOfSymbols;
            public word SizeOfOptionalHeader;
            public word Characteristics;
        }

        //https://msdn.microsoft.com/en-us/library/ms809762.aspx
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _IMAGE_IMPORT_DIRECTORY
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
        public struct IMAGE_DATA_DIRECTORY
        {
            public dword VirtualAddress;
            public dword Size;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _IMAGE_OPTIONAL_HEADER32
        {
            public word Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public dword SizeOfCode;
            public dword SizeOfInitializedData;
            public dword SizeOfUninitializedData;
            public dword AddressOfEntryPoint;
            public dword BaseOfCode;
            public dword BaseOfData;
            public dword ImageBase;
            public dword SectionAlignment;
            public dword FileAlignment;
            public word MajorOperatingSystemVersion;
            public word MinorOperatingSystemVersion;
            public word MajorImageVersion;
            public word MinorImageVersion;
            public word MajorSubsystemVersion;
            public word MinorSubsystemVersion;
            public dword Win32VersionValue;
            public dword SizeOfImage;
            public dword SizeOfHeaders;
            public dword CheckSum;
            public word Subsystem;
            public word DllCharacteristics;
            public dword SizeOfStackReserve;
            public dword SizeOfStackCommit;
            public dword SizeOfHeapReserve;
            public dword SizeOfHeapCommit;
            public dword LoaderFlags;
            public dword NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] ImageDataDirectory;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _IMAGE_OPTIONAL_HEADER64
        {
            public word Magic;
            public byte MajorLinkerVersion;
            public byte MinorLinkerVersion;
            public dword SizeOfCode;
            public dword SizeOfInitializedData;
            public dword SizeOfUninitializedData;
            public dword AddressOfEntryPoint;
            public dword BaseOfCode;
            public qword ImageBase;
            public dword SectionAlignment;
            public dword FileAlignment;
            public word MajorOperatingSystemVersion;
            public word MinorOperatingSystemVersion;
            public word MajorImageVersion;
            public word MinorImageVersion;
            public word MajorSubsystemVersion;
            public word MinorSubsystemVersion;
            public dword Win32VersionValue;
            public dword SizeOfImage;
            public dword SizeOfHeaders;
            public dword CheckSum;
            public word Subsystem;
            public word DllCharacteristics;
            public qword SizeOfStackReserve;
            public qword SizeOfStackCommit;
            public qword SizeOfHeapReserve;
            public qword SizeOfHeapCommit;
            public dword LoaderFlags;
            public dword NumberOfRvaAndSizes;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public IMAGE_DATA_DIRECTORY[] ImageDataDirectory;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IMAGE_NT_HEADERS
        {
            public dword Signature;
            public _IMAGE_FILE_HEADER FileHeader;
            public _IMAGE_OPTIONAL_HEADER32 OptionalHeader;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IMAGE_NT_HEADERS64
        {
            public dword Signature;
            public _IMAGE_FILE_HEADER FileHeader;
            public _IMAGE_OPTIONAL_HEADER64 OptionalHeader;
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct _IMAGE_DOS_HEADER
        {
            public word e_magic;
            public word e_cblp;
            public word e_cp;
            public word e_crlc;
            public word e_cparhdr;
            public word e_minalloc;
            public word e_maxalloc;
            public word e_ss;
            public word e_sp;
            public word e_csum;
            public word e_ip;
            public word e_cs;
            public word e_lfarlc;
            public word e_ovno;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)] //Matt has this set to 8
            public word[] e_res;
            public word e_oemid;
            public word e_oeminfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            public word[] e_res2;
            public dword e_lfanew; //Maybe Int64?
        };

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IMAGE_BASE_RELOCATION
        {
            public dword VirtualAdress;
            public dword SizeOfBlock;
        }

        public static void PrintStruct<T>(T imageDosHeader)
        {
            System.Reflection.FieldInfo[] fields = imageDosHeader.GetType().GetFields();
            Console.WriteLine("==========");
            foreach (var xInfo in fields)
            {
                Console.WriteLine("Field    {0}", xInfo.GetValue(imageDosHeader).ToString());
            }
            Console.WriteLine("==========");
        }
    }
}