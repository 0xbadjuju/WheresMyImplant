using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal abstract class BaseRemote : Base
    {
        private IntPtr hProcess;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected BaseRemote(UInt32 processId)
        {
            WriteOutputNeutral(String.Format("Attempting to get handle on PID: {0}", processId));
            hProcess = kernel32.OpenProcess(kernel32.PROCESS_ALL_ACCESS, false, processId);
            if (IntPtr.Zero == hProcess)
            {
                WriteOutputBad("Unable to get process handle");
                return;
            }
            WriteOutputGood(String.Format("Received Handle: 0x{0}", hProcess.ToString("X4")));
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean Is32BitProcess()
        {
            Winbase._SYSTEM_INFO systemInfo;
            kernel32.GetNativeSystemInfo(out systemInfo);
            if (Winbase.INFO_PROCESSOR_ARCHITECTURE.PROCESSOR_ARCHITECTURE_INTEL == systemInfo.wProcessorArchitecture)
            {
                WriteOutputBad("System is 32Bit");
                return true;
            }

            Boolean is32Bit;
            if (!kernel32.IsWow64Process(hProcess, out is32Bit))
            {
                WriteOutputBad("IsWow64Process Failed");
                WriteOutputBad("Assuming 32Bit System");
                return true;
            }
            
            return is32Bit;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // No idea why this exists
        ////////////////////////////////////////////////////////////////////////////////
        protected IntPtr VirtualAllocExChecked(IntPtr lpAddress, UInt32 dwSize, Winnt.MEMORY_PROTECTION_CONSTANTS protection)
        {
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(
                hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, protection);
            if (IntPtr.Zero == lpBaseAddress)
            {
                WriteOutputBad("Unable to allocate memory");
                return IntPtr.Zero;
            }
            WriteOutputGood(String.Format("Allocated {0} bytes at {1}", dwSize, lpBaseAddress.ToString("X4")));
            WriteOutputGood(String.Format("\tMemory Protection Set to {0}", protection));
            return lpBaseAddress;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // No idea why this exists
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean WriteProcessMemoryChecked(IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 dwSize, String sectionName)
        {
            UInt32 dwNumberOfBytesWritten = 0;
            if (!kernel32.WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, ref dwNumberOfBytesWritten))
            {
                WriteOutputBad(
                    "Unable to write process memory" +
                    "\n\tResult:                  " + Marshal.GetLastWin32Error() +
                    "\n\tdwSize                   " + dwSize +
                    "\n\tlpBaseAddress            " + lpBaseAddress.ToString("X4") +
                    "\n\tlpBuffer                 " + lpBuffer.ToString("X4") +
                    "\n\tdwNumberOfBytesWritten   " + dwNumberOfBytesWritten
                );
                return false;
            }
            WriteOutputGood("Section " + sectionName.Replace("\0", "") + " (" + dwNumberOfBytesWritten + " bytes), Copied To " + lpBaseAddress.ToString("X4"));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // No idea why this exists
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean WriteProcessMemoryUnChecked(IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 dwSize, String sectionName)
        {
            UInt32 dwNumberOfBytesWritten = 0;
            if (IntPtr.Zero != lpBuffer)
            {
                return false;
            }

            if (!kernel32.WriteProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, ref dwNumberOfBytesWritten))
            {
                WriteOutputBad("Unable to write process memory");
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean ReadProcessMemoryChecked(IntPtr lpBaseAddress, IntPtr lpBuffer, UInt32 dwSize, String sectionName)
        {
            UInt32 dwNumberOfBytesRead = 0;
            if (!kernel32.ReadProcessMemory(hProcess, lpBaseAddress, lpBuffer, dwSize, ref dwNumberOfBytesRead))
            {
                WriteOutputBad(
                    "Unable to read process memory" +
                    "\n\tResult:                  " + Marshal.GetLastWin32Error() +
                    "\n\tdwSize                   " + dwSize +
                    "\n\tlpBaseAddress            " + lpBaseAddress.ToString("X4") +
                    "\n\tlpBuffer                 " + lpBuffer.ToString("X4") +
                    "\n\tdwNumberOfBytesRead      " + dwNumberOfBytesRead
                );
                return false;
            }
            //WriteOutputGood(String.Format("Section {0} ({1} bytes), Read From {2}", sectionName.Replace("\0", ""), dwNumberOfBytesRead, lpBaseAddress.ToString("X4")));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // No idea why this exists
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean CreateRemoteThreadChecked(IntPtr lpStartAddress, IntPtr lpParameter, IntPtr hThread)
        {
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            UInt32 dwCreationFlags = 0;
            UInt32 lpThreadId = 0;
            hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, lpStartAddress, lpParameter, dwCreationFlags, ref lpThreadId);
            if (IntPtr.Zero == hThread)
            {
                WriteOutputBad("CreateRemoteThread Failed");
                return false;
            }
            WriteOutputGood(String.Format("Thread Created {0}", lpThreadId));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected T PtrToStructureRemote<T>(IntPtr pointer)
        {
            UInt32 imageSizeOfStruct = (UInt32)Marshal.SizeOf(typeof(T));
            byte[] structBuffer = new byte[imageSizeOfStruct];
            GCHandle pinnedStructBuffer = GCHandle.Alloc(structBuffer, GCHandleType.Pinned);
            T marshaledStruct = default(T);
            try
            {
                IntPtr lpStructBytes = new IntPtr((Int64)pinnedStructBuffer.AddrOfPinnedObject());
                if (!ReadProcessMemoryChecked(pointer, lpStructBytes, imageSizeOfStruct, typeof(T).ToString()))
                {
                    return marshaledStruct;
                }
                marshaledStruct = (T)Marshal.PtrToStructure(lpStructBytes, typeof(T));
            }
            catch
            {
                return marshaledStruct;
            }
            finally
            {
                pinnedStructBuffer.Free();
            }
            return marshaledStruct;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected UInt16 ReadInt16Remote(IntPtr pointer, Int64 offset)
        {
            UInt16 integer = 0;
            UInt16 marshaledInt16 = 0;
            GCHandle pinnedInteger = GCHandle.Alloc(integer, GCHandleType.Pinned);
            //WriteOutputNeutral("pinnedInteger: " + pinnedInteger.AddrOfPinnedObject());
            try
            {
                IntPtr lpInteger = pinnedInteger.AddrOfPinnedObject();
                IntPtr adjustedPointer = new IntPtr(pointer.ToInt64() + offset);
                if (ReadProcessMemoryChecked(adjustedPointer, lpInteger, sizeof(UInt16), typeof(UInt16).ToString()))
                {
                    return marshaledInt16;
                }
                marshaledInt16 = (UInt16)Marshal.ReadInt16(lpInteger);
            }
            catch
            {
                WriteOutputBad("ReadInt16Remote Failed");
                return marshaledInt16;
            }
            finally
            {
                pinnedInteger.Free();
            }
            return marshaledInt16;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected Int32 PtrToInt32Remote(IntPtr pointer)
        {
            Int32 integer = 0;
            Int32 remoteInt32 = 0;
            GCHandle pinnedInteger = GCHandle.Alloc(integer, GCHandleType.Pinned);
            try
            {
                IntPtr lpInteger = new IntPtr((Int64)pinnedInteger.AddrOfPinnedObject());
                if (!ReadProcessMemoryChecked(pointer, lpInteger, sizeof(Int32), typeof(Int32).ToString()))
                {
                    return 0;
                }
                remoteInt32 = Marshal.ReadInt32(lpInteger);
            }
            catch (Exception ex)
            {
                WriteOutputBad("PtrToInt32Remote Failed");
                WriteOutput(ex.Message);
                return 0;
            }
            finally
            {
                pinnedInteger.Free();
            }
            return remoteInt32;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected Int64 ReadInt64Remote(IntPtr pointer)
        {
            Int64 integer = 0;
            Int64 marshaledInt64 = 0;
            GCHandle pinnedInteger = GCHandle.Alloc(integer, GCHandleType.Pinned);
            try
            {
                IntPtr lpInteger = pinnedInteger.AddrOfPinnedObject();
                if (!ReadProcessMemoryChecked(pointer, lpInteger, sizeof(Int64), typeof(Int64).ToString()))
                {
                    WriteOutputBad("ReadInt64Remote Failed");
                    return marshaledInt64;
                }
                marshaledInt64 = Marshal.ReadInt64(lpInteger);
            }
            catch
            {
                WriteOutputBad("ReadInt64Remote Failed");
                return marshaledInt64;
            }
            finally
            {
                pinnedInteger.Free();
            }
            return marshaledInt64;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean WriteInt64Remote(IntPtr lpBaseAddress, Int64 value)
        {
            GCHandle pinnedInteger = GCHandle.Alloc(value, GCHandleType.Pinned);
            try
            {
                IntPtr lpInteger = pinnedInteger.AddrOfPinnedObject();
                UInt32 dwNumberOfBytesWritten = 0;
                if (!kernel32.WriteProcessMemory(hProcess, lpBaseAddress, lpInteger, (UInt32)sizeof(Int64), ref dwNumberOfBytesWritten))
                {
                    WriteOutputBad("WriteInt64Remote Failed");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                WriteOutputBad("WriteInt64Remote Failed");
                WriteOutput(ex.Message);
                return false;
            }
            finally
            {
                pinnedInteger.Free();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected String PtrToStringAnsiRemote(IntPtr pointer)
        {
            Int32 offset = 0;
            String stringResult = "";
            Byte character = new Byte();
            GCHandle pinnedCharacter = GCHandle.Alloc(character, GCHandleType.Pinned);
            try
            {
                IntPtr lpCharacter = pinnedCharacter.AddrOfPinnedObject();
                Char marshaledChar;
                do
                {
                    IntPtr adjustedPointer = new IntPtr(pointer.ToInt64() + offset++);
                    ReadProcessMemoryChecked(adjustedPointer, lpCharacter, sizeof(Char), typeof(Char).ToString());
                    marshaledChar = (char)Marshal.ReadByte(lpCharacter);
                    stringResult += marshaledChar;
                } while (marshaledChar != '\0');
            }
            catch (Exception ex)
            {
                WriteOutputBad("WriteInt64Remote Failed");
                WriteOutput(ex.Message);
            }
            finally
            {
                pinnedCharacter.Free();
            }
            return stringResult;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected IntPtr LoadLibraryRemote(String library)
        {
            ////////////////////////////////////////////////////////////////////////////////
            IntPtr hmodule = kernel32.GetModuleHandle("kernel32.dll");
            IntPtr loadLibraryAddr = kernel32.GetProcAddress(hmodule, "LoadLibraryA");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_READWRITE);

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            IntPtr libraryPtr = Marshal.StringToHGlobalAnsi(library);
            Boolean writeProcessMemoryResult = kernel32.WriteProcessMemory(hProcess, lpBaseAddress, libraryPtr, dwSize, ref lpNumberOfBytesWritten);

            ////////////////////////////////////////////////////////////////////////////////
            Winnt.MEMORY_PROTECTION_CONSTANTS lpflOldProtect = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_NOACCESS;
            Boolean virtualProtectExResult = kernel32.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref lpflOldProtect);

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            IntPtr hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            return hThread;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean WaitForSingleObjectExRemote(IntPtr hThread)
        {
            if (0 != kernel32.WaitForSingleObjectEx(hProcess, hThread, 0xFFFFFFFF))
            {
                return false;
            }
            return true;
        }
    }
}
