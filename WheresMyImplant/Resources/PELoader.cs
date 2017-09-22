using System;
using System.IO;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    public class PELoader
    {
        public Boolean is64Bit;
        public Structs._IMAGE_DOS_HEADER imageDosHeader;
        public Structs._IMAGE_FILE_HEADER imageFileHeader;
        public Structs._IMAGE_OPTIONAL_HEADER64 imageOptionalHeader64;
        public Structs._IMAGE_OPTIONAL_HEADER32 imageOptionalHeader32;
        public Structs._IMAGE_SECTION_HEADER[] imageSectionHeaders;
        public byte[] imageBytes;
        public UInt32 sizeOfImage;
        public UInt32 imageBase;
        public Int32 baseRelocationTableAddress;
        public Int32 importTableAddress;
        public Int32 addressOfEntryPoint;

        //https://github.com/mattifestation/PIC_Bindshell/blob/master/lib/PowerShell/Get-PEHeader.ps1
        //https://gist.github.com/subTee/2cb7973b677f37d32f04
        //https://www.microsoft.com/en-us/download/confirmation.aspx?id=19509
        //http://www.csn.ul.ie/~caolan/pub/winresdump/winresdump/doc/pefile.html

        public PELoader(string libary)
        {
            FileStream fileStream = new FileStream(libary, System.IO.FileMode.Open, System.IO.FileAccess.Read);
            BinaryReader binaryReader = new BinaryReader(fileStream);
            imageDosHeader = FromBinaryReader<Structs._IMAGE_DOS_HEADER>(binaryReader);
            fileStream.Seek(imageDosHeader.e_lfanew, SeekOrigin.Begin);
            ReadHeaders(ref binaryReader);
            fileStream.Close();
            imageBytes = System.IO.File.ReadAllBytes(libary);
        }

        public PELoader(byte[] fileBytes)
        {
            MemoryStream memoryStream = new MemoryStream(fileBytes, 0, fileBytes.Length);
            BinaryReader binaryReader = new BinaryReader(memoryStream);
            imageDosHeader = FromBinaryReader<Structs._IMAGE_DOS_HEADER>(binaryReader);
            memoryStream.Seek(imageDosHeader.e_lfanew, SeekOrigin.Begin);
            ReadHeaders(ref binaryReader);
            memoryStream.Close();
            imageBytes = fileBytes;
        }

        private void ReadHeaders(ref BinaryReader binaryReader)
        {
            binaryReader.ReadUInt32();
            imageFileHeader = FromBinaryReader<Structs._IMAGE_FILE_HEADER>(binaryReader);

            switch (imageFileHeader.Machine)
            {
                case 0x14c:
                    imageOptionalHeader32 = FromBinaryReader<Structs._IMAGE_OPTIONAL_HEADER32>(binaryReader);
                    sizeOfImage = imageOptionalHeader32.SizeOfImage;
                    baseRelocationTableAddress = (Int32)imageOptionalHeader32.ImageDataDirectory[(Int32)Structs.IMAGE_DATA_DIRECTORY_OPTIONS.BaseRelocationTable].VirtualAddress;
                    importTableAddress = (Int32)imageOptionalHeader32.ImageDataDirectory[(Int32)Structs.IMAGE_DATA_DIRECTORY_OPTIONS.ImportTable].VirtualAddress;
                    addressOfEntryPoint = (Int32)imageOptionalHeader32.AddressOfEntryPoint;
                    Console.WriteLine("ImageBase = {0}", imageOptionalHeader32.ImageBase.ToString("X4"));
                    Console.WriteLine("EntryPoint = {0}", imageOptionalHeader32.AddressOfEntryPoint.ToString("X4"));
                    is64Bit = false;
                    break;
                case 0x8664:
                    imageOptionalHeader64 = FromBinaryReader<Structs._IMAGE_OPTIONAL_HEADER64>(binaryReader);
                    sizeOfImage = imageOptionalHeader64.SizeOfImage;
                    baseRelocationTableAddress = (Int32)imageOptionalHeader64.ImageDataDirectory[(Int32)Structs.IMAGE_DATA_DIRECTORY_OPTIONS.BaseRelocationTable].VirtualAddress;
                    importTableAddress = (Int32)imageOptionalHeader64.ImageDataDirectory[(Int32)Structs.IMAGE_DATA_DIRECTORY_OPTIONS.ImportTable].VirtualAddress;
                    addressOfEntryPoint = (Int32)imageOptionalHeader64.AddressOfEntryPoint;
                    Console.WriteLine("ImageBase = {0}", imageOptionalHeader64.ImageBase.ToString("X4"));
                    Console.WriteLine("EntryPoint = {0}", imageOptionalHeader64.AddressOfEntryPoint.ToString("X4"));
                    is64Bit = true;
                    break;
                default:
                    return;
            };
            imageSectionHeaders = new Structs._IMAGE_SECTION_HEADER[imageFileHeader.NumberOfSections];
            for (int i = 0; i < imageFileHeader.NumberOfSections; ++i)
            {
                imageSectionHeaders[i] = FromBinaryReader<Structs._IMAGE_SECTION_HEADER>(binaryReader);
            }
            binaryReader.Close();
        }

        //https://social.msdn.microsoft.com/Forums/vstudio/en-US/9bdf0eb7-a003-4880-a441-4ce06cf80cbf/whats-the-easiest-way-to-parse-windows-pe-files?forum=csharpgeneral
        private static T FromBinaryReader<T>(BinaryReader binaryReader) where T : struct
        {
            byte[] bytes = binaryReader.ReadBytes(Marshal.SizeOf(typeof(T)));

            GCHandle handle = GCHandle.Alloc(bytes, GCHandleType.Pinned);
            var theStructure = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();

            return theStructure;
        }
    }
}