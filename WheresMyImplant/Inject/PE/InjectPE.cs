using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    class InjectPE : Base
    {
        //https://msdn.microsoft.com/en-us/library/ms809762.aspx
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _IMAGE_IMPORT_DIRECTORY
        {
            public UInt32 RvaImportLookupTable;
            public UInt32 TimeDateStamp;
            public UInt32 ForwarderChain;
            public UInt32 RvaModuleName;
            public UInt32 RvaImportAddressTable;
        }

        internal InjectPE(PELoader peLoader, String parameters)
        {
            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = peLoader.sizeOfImage;
            IntPtr lpBaseAddress = kernel32.VirtualAlloc(lpAddress, dwSize, kernel32.MEM_COMMIT, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE);
            WriteOutputGood(String.Format("Allocated {0} bytes at 0x{1}", peLoader.sizeOfImage.ToString("X4"), lpBaseAddress.ToString("X4")));

            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < peLoader.imageFileHeader.NumberOfSections; i++)
            {
                IntPtr lpBaseAddressSection = new IntPtr(lpBaseAddress.ToInt32() + peLoader.imageSectionHeaders[i].VirtualAddress);
                UInt32 dwSizeSection = peLoader.imageSectionHeaders[i].SizeOfRawData;
                IntPtr lpAllocatedAddress = kernel32.VirtualAlloc(lpBaseAddressSection, dwSizeSection, kernel32.MEM_COMMIT, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE);
                Marshal.Copy(peLoader.imageBytes, (Int32)peLoader.imageSectionHeaders[i].PointerToRawData, lpAllocatedAddress, (Int32)peLoader.imageSectionHeaders[i].SizeOfRawData);
                WriteOutputGood(String.Format("Copied {0} to 0x{1}",  peLoader.imageSectionHeaders[i].Name, lpAllocatedAddress.ToString("X4")));
            }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr relocationTable = new IntPtr(lpBaseAddress.ToInt32() + peLoader.baseRelocationTableAddress);
            Winnt._IMAGE_BASE_RELOCATION relocationEntry = (Winnt._IMAGE_BASE_RELOCATION)Marshal.PtrToStructure(relocationTable, typeof(Winnt._IMAGE_BASE_RELOCATION));

            Int32 sizeOfRelocationStruct = Marshal.SizeOf(typeof(Winnt._IMAGE_BASE_RELOCATION));
            Int32 sizeofNextBlock = (Int32)relocationEntry.SizeOfBlock;
            IntPtr offset = relocationTable;

            ////////////////////////////////////////////////////////////////////////////////
            while (true)
            {
                Winnt._IMAGE_BASE_RELOCATION relocationNextEntry = new Winnt._IMAGE_BASE_RELOCATION();
                IntPtr lpNextRelocationEntry = new IntPtr(relocationTable.ToInt32() + (Int32)sizeofNextBlock);
                relocationNextEntry = (Winnt._IMAGE_BASE_RELOCATION)Marshal.PtrToStructure(lpNextRelocationEntry, typeof(Winnt._IMAGE_BASE_RELOCATION));
                IntPtr destinationAddress = new IntPtr(lpBaseAddress.ToInt32() + (Int32)relocationEntry.VirtualAdress);

                ////////////////////////////////////////////////////////////////////////////////
                for (Int32 i = 0; i < (Int32)((relocationEntry.SizeOfBlock - sizeOfRelocationStruct) / 2); i++)
                {
                    UInt16 value = (UInt16)Marshal.ReadInt16(offset, 8 + (2 * i));

                    UInt16 type = (UInt16)(value >> 12);
                    UInt16 fixup = (UInt16)(value & 0xfff);

                    switch (type)
                    {
                        case 0x0:
                            break;
                        case 0xA:
                            if (peLoader.is64Bit)
                            {
                                IntPtr patchAddress = new IntPtr(destinationAddress.ToInt64() + (Int32)fixup);
                                Int64 originalAddress = Marshal.ReadInt64(patchAddress);
                                Int64 delta64 = (Int64)(lpBaseAddress.ToInt64() - (Int64)peLoader.imageOptionalHeader64.ImageBase);
                                Marshal.WriteInt64(patchAddress, originalAddress + delta64);
                            }
                            else
                            {
                                IntPtr patchAddress = new IntPtr(destinationAddress.ToInt32() + (Int32)fixup);
                                Int32 originalAddress = Marshal.ReadInt32(patchAddress);
                                Int32 delta32 = (Int32)(lpBaseAddress.ToInt32() - (Int32)peLoader.imageOptionalHeader32.ImageBase);
                                Marshal.WriteInt32(patchAddress, originalAddress + delta32);
                            }
                            break;
                    }
                }
                offset = new IntPtr(relocationTable.ToInt32() + (int)sizeofNextBlock);
                sizeofNextBlock += (int)relocationNextEntry.SizeOfBlock;
                relocationEntry = relocationNextEntry;
                //"The last entry is set to zero (NULL) to indicate the end of the table." - cool
                if (0 == relocationNextEntry.SizeOfBlock)
                {
                    break;
                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            //http://sandsprite.com/CodeStuff/Understanding_imports.html
            ////////////////////////////////////////////////////////////////////////////////
            Int32 sizeOfStruct = Marshal.SizeOf(typeof(_IMAGE_IMPORT_DIRECTORY));
            Int32 multiplier = 0;
            while (true)
            {
                Int32 dwImportTableAddressOffset = ((sizeOfStruct * multiplier++) + peLoader.importTableAddress);
                IntPtr lpImportAddressTable = new IntPtr(lpBaseAddress.ToInt32() + dwImportTableAddressOffset);
                _IMAGE_IMPORT_DIRECTORY imageImportDirectory = (_IMAGE_IMPORT_DIRECTORY)Marshal.PtrToStructure(lpImportAddressTable, typeof(_IMAGE_IMPORT_DIRECTORY));
                if (0 == imageImportDirectory.RvaImportAddressTable)
                {
                    break;
                }

                ////////////////////////////////////////////////////////////////////////////////
                IntPtr dllNamePTR = new IntPtr(lpBaseAddress.ToInt32() + imageImportDirectory.RvaModuleName);
                string dllName = Marshal.PtrToStringAnsi(dllNamePTR);
                IntPtr hModule = kernel32.LoadLibrary(dllName);
                WriteOutputGood(String.Format("Loaded {0} at {1}", dllName, hModule.ToString("X4")));
                ////////////////////////////////////////////////////////////////////////////////
                IntPtr lpRvaImportAddressTable = new IntPtr(lpBaseAddress.ToInt32() + imageImportDirectory.RvaImportAddressTable);
                while (true)
                {
                    Int32 dwRvaImportAddressTable = Marshal.ReadInt32(lpRvaImportAddressTable);
                    if (0 == dwRvaImportAddressTable)
                    {
                        break;
                    }
                    IntPtr lpDllFunctionName = (new IntPtr(lpBaseAddress.ToInt32() + dwRvaImportAddressTable + 2));
                    string dllFunctionName = Marshal.PtrToStringAnsi(lpDllFunctionName);
                    IntPtr functionAddress = kernel32.GetProcAddress(hModule, dllFunctionName);
                    Marshal.WriteInt64(lpRvaImportAddressTable, (Int64)functionAddress);
                    lpRvaImportAddressTable = new IntPtr(lpRvaImportAddressTable.ToInt32() + 8);
                }
            }
            ////////////////////////////////////////////////////////////////////////////////
            String parameter = "";
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            Int32 dwStartAddress = lpBaseAddress.ToInt32() + peLoader.addressOfEntryPoint;
            IntPtr lpStartAddress = new IntPtr(dwStartAddress);
            IntPtr lpParameter = new IntPtr();//Convert.ToInt32(parameter));
            UInt32 dwCreationFlags = 0;
            UInt32 lpThreadId = 0;
            IntPtr hThread = kernel32.CreateThread(lpThreadAttributes, dwStackSize, lpStartAddress, lpParameter, dwCreationFlags, ref lpThreadId);
            WriteOutputGood(String.Format("Created thread 0x{0}", hThread.ToString("X4")));
            ////////////////////////////////////////////////////////////////////////////////
            kernel32.WaitForSingleObject(hThread, 0xFFFFFFFF);
        }
    }
}
