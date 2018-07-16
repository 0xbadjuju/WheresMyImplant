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

        internal Boolean is64Bit;
        private Winnt._IMAGE_DOS_HEADER imageDosHeader;
        internal Winnt._IMAGE_FILE_HEADER imageFileHeader;
        internal Winnt._IMAGE_OPTIONAL_HEADER imageOptionalHeader32;
        internal Winnt._IMAGE_OPTIONAL_HEADER64 imageOptionalHeader64;
        internal Winnt._IMAGE_SECTION_HEADER[] imageSectionHeaders;
        internal Byte[] imageBytes;
        internal UInt32 sizeOfImage;
        private UInt32 imageBase;
        internal Int32 baseRelocationTableAddress;
        internal Int32 importTableAddress;
        internal Int32 addressOfEntryPoint;

        //https://github.com/mattifestation/PIC_Bindshell/blob/master/lib/PowerShell/Get-PEHeader.ps1
        //https://gist.github.com/subTee/2cb7973b677f37d32f04
        //https://www.microsoft.com/en-us/download/confirmation.aspx?id=19509
        //http://www.csn.ul.ie/~caolan/pub/winresdump/winresdump/doc/pefile.html

        internal PELoader()
        {
            
        }

        internal void Execute(String library)
        {
            using (FileStream fileStream = new FileStream(library, FileMode.Open, FileAccess.Read))
            {
                BinaryReader binaryReader = new BinaryReader(fileStream);
                imageDosHeader = FromBinaryReader<Winnt._IMAGE_DOS_HEADER>(binaryReader);
                fileStream.Seek(imageDosHeader.e_lfanew, SeekOrigin.Begin);
                ReadHeaders(ref binaryReader);
            }
            imageBytes = File.ReadAllBytes(library);
        }

        internal void Execute(Byte[] fileBytes)
        {
            using (MemoryStream memoryStream = new MemoryStream(fileBytes, 0, fileBytes.Length))
            {
                BinaryReader binaryReader = new BinaryReader(memoryStream);
                imageDosHeader = FromBinaryReader<Winnt._IMAGE_DOS_HEADER>(binaryReader);
                memoryStream.Seek(imageDosHeader.e_lfanew, SeekOrigin.Begin);
                ReadHeaders(ref binaryReader);
            }
            imageBytes = fileBytes;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private void ReadHeaders(ref BinaryReader binaryReader)
        {
            binaryReader.ReadUInt32();
            imageFileHeader = FromBinaryReader<Winnt._IMAGE_FILE_HEADER>(binaryReader);

            switch (imageFileHeader.Machine)
            {
                case Winnt.IMAGE_FILE_MACHINE.I386:
                    imageOptionalHeader32 = FromBinaryReader<Winnt._IMAGE_OPTIONAL_HEADER>(binaryReader);
                    sizeOfImage = imageOptionalHeader32.SizeOfImage;
                    baseRelocationTableAddress = (Int32)imageOptionalHeader32.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.BaseRelocationTable].VirtualAddress;
                    importTableAddress = (Int32)imageOptionalHeader32.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.ImportTable].VirtualAddress;
                    addressOfEntryPoint = (Int32)imageOptionalHeader32.AddressOfEntryPoint;
                    WriteOutputGood(String.Format("ImageBase: 0x{0}", imageOptionalHeader32.ImageBase.ToString("X4")));
                    WriteOutputGood(String.Format("EntryPoint: 0x{0}", imageOptionalHeader32.AddressOfEntryPoint.ToString("X4")));
                    is64Bit = false;
                    break;
                case Winnt.IMAGE_FILE_MACHINE.AMD64:
                    imageOptionalHeader64 = FromBinaryReader<Winnt._IMAGE_OPTIONAL_HEADER64>(binaryReader);
                    sizeOfImage = imageOptionalHeader64.SizeOfImage;
                    baseRelocationTableAddress = (Int32)imageOptionalHeader64.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.BaseRelocationTable].VirtualAddress;
                    importTableAddress = (Int32)imageOptionalHeader64.ImageDataDirectory[(Int32)IMAGE_DATA_DIRECTORY_OPTIONS.ImportTable].VirtualAddress;
                    addressOfEntryPoint = (Int32)imageOptionalHeader64.AddressOfEntryPoint;
                    WriteOutputGood(String.Format("ImageBase: 0x{0}", imageOptionalHeader64.ImageBase.ToString("X4")));
                    WriteOutputGood(String.Format("EntryPoint: 0x{0}", imageOptionalHeader64.AddressOfEntryPoint.ToString("X4")));
                    is64Bit = true;
                    break;
                default:
                    Console.WriteLine("default");
                    return;
            };
            imageSectionHeaders = new Winnt._IMAGE_SECTION_HEADER[(Int32)imageFileHeader.NumberOfSections];
            for (int i = 0; i < (Int32)imageFileHeader.NumberOfSections; ++i)
            {
                imageSectionHeaders[i] = FromBinaryReader<Winnt._IMAGE_SECTION_HEADER>(binaryReader);
            }
            binaryReader.Close();
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

        }
    }
}