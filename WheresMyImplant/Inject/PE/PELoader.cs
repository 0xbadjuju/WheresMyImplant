using System;
using System.IO;
using System.Runtime.InteropServices;

using Unmanaged.Headers;

namespace WheresMyImplant
{
    internal sealed class PELoader : Base, IDisposable
    {
        [Flags]
        private enum IMAGE_DATA_DIRECTORY_OPTIONS : int
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

        internal Boolean isDll = false;
        internal Boolean isExe = false;

        internal Boolean is64Bit;
        private Winnt._IMAGE_DOS_HEADER imageDosHeader;
        internal Winnt._IMAGE_FILE_HEADER imageFileHeader;
        internal Winnt._IMAGE_OPTIONAL_HEADER imageOptionalHeader32;
        internal Winnt._IMAGE_OPTIONAL_HEADER64 imageOptionalHeader64;
        internal Winnt._IMAGE_SECTION_HEADER[] imageSectionHeaders;
        internal Byte[] imageBytes;
        internal UInt32 sizeOfImage;
        internal UInt64 imageBase;
        internal UInt32 baseRelocationTableAddress;
        internal UInt32 importTableAddress;
        internal UInt32 addressOfEntryPoint;
        internal UInt16 dllCharacteristics = 0;

        private BinaryReader binaryReader;

        //https://github.com/mattifestation/PIC_Bindshell/blob/master/lib/PowerShell/Get-PEHeader.ps1
        //https://gist.github.com/subTee/2cb7973b677f37d32f04
        //https://www.microsoft.com/en-us/download/confirmation.aspx?id=19509
        //http://www.csn.ul.ie/~caolan/pub/winresdump/winresdump/doc/pefile.html

        internal PELoader()
        {
            
        }

        internal Boolean Execute(String library)
        {
            using (FileStream fileStream = new FileStream(library, FileMode.Open, FileAccess.Read))
            {
                binaryReader = new BinaryReader(fileStream);
                imageDosHeader = FromBinaryReader<Winnt._IMAGE_DOS_HEADER>(binaryReader);
                fileStream.Seek(imageDosHeader.e_lfanew, SeekOrigin.Begin);
                if (!ReadHeaders(ref binaryReader))
                {
                    return false;
                }
            }
            imageBytes = File.ReadAllBytes(library);
            return true;
        }

        internal Boolean Execute(Byte[] fileBytes)
        {
            using (MemoryStream memoryStream = new MemoryStream(fileBytes, 0, fileBytes.Length))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                imageDosHeader = FromBinaryReader<Winnt._IMAGE_DOS_HEADER>(binaryReader);
                memoryStream.Seek(imageDosHeader.e_lfanew, SeekOrigin.Begin);
                if (!ReadHeaders(ref binaryReader))
                {
                    return false;
                }
            }
            imageBytes = fileBytes;
            return true;
         }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean ReadHeaders(ref BinaryReader binaryReader)
        {
            binaryReader.ReadUInt32();
            imageFileHeader = FromBinaryReader<Winnt._IMAGE_FILE_HEADER>(binaryReader);

            if (Winnt.CHARACTERISTICS.IMAGE_FILE_DLL == (imageFileHeader.Characteristics & Winnt.CHARACTERISTICS.IMAGE_FILE_DLL))
            {
                WriteOutputNeutral("Injecting DLL");
                isDll = true;
            }
            else if (Winnt.CHARACTERISTICS.IMAGE_FILE_EXECUTABLE_IMAGE == (imageFileHeader.Characteristics & Winnt.CHARACTERISTICS.IMAGE_FILE_EXECUTABLE_IMAGE))
            {
                WriteOutputNeutral("Injecting EXE");
                isExe = true;
            }

            foreach (Winnt.CHARACTERISTICS c in (Winnt.CHARACTERISTICS[])Enum.GetValues(typeof(Winnt.CHARACTERISTICS)))
            {
                if ((UInt16)c == (UInt16)(c & imageFileHeader.Characteristics))
                {
                    WriteOutputNeutral(String.Format("PE Characteristic: {0}", (Winnt.CHARACTERISTICS)(c & imageFileHeader.Characteristics)));
                }
            }

            UInt16 subsystem = 0;
            
            switch (imageFileHeader.Machine)
            {
                case Winnt.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_I386:
                    imageOptionalHeader32 = FromBinaryReader<Winnt._IMAGE_OPTIONAL_HEADER>(binaryReader);
                    sizeOfImage = imageOptionalHeader32.SizeOfImage;
                    baseRelocationTableAddress = imageOptionalHeader32.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.BaseRelocationTable].VirtualAddress;
                    importTableAddress = imageOptionalHeader32.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.ImportTable].VirtualAddress;
                    addressOfEntryPoint = imageOptionalHeader32.AddressOfEntryPoint;
                    imageBase = imageOptionalHeader32.ImageBase;
                    subsystem = (UInt16)imageOptionalHeader32.Subsystem;
                    dllCharacteristics = (UInt16)imageOptionalHeader32.DllCharacteristics;
                    is64Bit = false;
                    break;
                case Winnt.IMAGE_FILE_MACHINE.IMAGE_FILE_MACHINE_AMD64:
                    imageOptionalHeader64 = FromBinaryReader<Winnt._IMAGE_OPTIONAL_HEADER64>(binaryReader);
                    sizeOfImage = imageOptionalHeader64.SizeOfImage;
                    baseRelocationTableAddress = imageOptionalHeader64.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.BaseRelocationTable].VirtualAddress;
                    importTableAddress = imageOptionalHeader64.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.ImportTable].VirtualAddress;
                    addressOfEntryPoint = imageOptionalHeader64.AddressOfEntryPoint;
                    imageBase = imageOptionalHeader64.ImageBase;
                    subsystem = (UInt16)imageOptionalHeader64.Subsystem;
                    dllCharacteristics = (UInt16)imageOptionalHeader64.DllCharacteristics;
                    is64Bit = true;
                    break;
                default:
                    return false;
            };

            WriteOutputGood(String.Format("ImageBase: 0x{0}", imageBase.ToString("X4")));
            WriteOutputGood(String.Format("EntryPoint: 0x{0}", addressOfEntryPoint.ToString("X4")));
            foreach (Winnt.SUBSYSTEM ss in (Winnt.SUBSYSTEM[]) Enum.GetValues(typeof(Winnt.SUBSYSTEM)))
            {
                if ((UInt16)ss == ((UInt16)ss & subsystem))
                {
                    WriteOutputNeutral(String.Format("PE SubSystem: {0}", (Winnt.SUBSYSTEM)((UInt16)ss & subsystem)));
                }
            }

            foreach (Winnt.DLL_CHARACTERISTICS dll in (Winnt.DLL_CHARACTERISTICS[])Enum.GetValues(typeof(Winnt.DLL_CHARACTERISTICS)))
            {
                if ((UInt16)dll == ((UInt16)dll & dllCharacteristics))
                {
                    WriteOutputNeutral(String.Format("DLL Characteristics: {0}", (Winnt.DLL_CHARACTERISTICS)((UInt16)dll & dllCharacteristics)));
                }
            }

            imageSectionHeaders = new Winnt._IMAGE_SECTION_HEADER[(Int32)imageFileHeader.NumberOfSections];
            for (int i = 0; i < (Int32)imageFileHeader.NumberOfSections; ++i)
            {
                imageSectionHeaders[i] = FromBinaryReader<Winnt._IMAGE_SECTION_HEADER>(binaryReader);
            }
            
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // https://social.msdn.microsoft.com/Forums/vstudio/en-US/9bdf0eb7-a003-4880-a441-4ce06cf80cbf/whats-the-easiest-way-to-parse-windows-pe-files?forum=csharpgeneral
        ////////////////////////////////////////////////////////////////////////////////
        private static T FromBinaryReader<T>(BinaryReader binaryReader) where T : struct
        {
            byte[] bytes = binaryReader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            catch (Exception)
            {
                return default(T);
            }
            finally
            {
                handle.Free();
            }
        }

        ~PELoader()
        {
        }

        public void Dispose()
        {
            binaryReader.Close();
        }
    }
}