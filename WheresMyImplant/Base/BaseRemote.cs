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
        internal BaseRemote(UInt32 processId)
        {
            WriteOutputNeutral("Attempting to get handle on PID: " + processId);
            hProcess = kernel32.OpenProcess(kernel32.PROCESS_ALL_ACCESS, false, processId);
            if (IntPtr.Zero == hProcess)
            {
                WriteOutputBad("Unable to get process handle");
                return;
            }
            else
            {
                WriteOutputGood("Recieved Handle: 0x" + hProcess.ToString("X4"));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal IntPtr VirtualAllocExChecked(IntPtr lpAddress, UInt32 dwSize)
        {
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(
                hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT, Winnt.PAGE_EXECUTE_READWRITE
            );

            if (IntPtr.Zero == lpBaseAddress)
            {
                WriteOutputBad("Unable to allocate memory");
                Environment.Exit(1);
                //This is dumb
                return IntPtr.Zero;
            }
            else
            {
                WriteOutputGood("Allocated " + dwSize + " bytes at " + lpBaseAddress.ToString("X4"));
                WriteOutputGood("\tMemory Protection Set to PAGE_READWRITE");
                return lpBaseAddress;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean WriteProcessMemoryChecked(
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            UInt32 dwSize,
            string sectionName
        )
        {
            UInt32 dwNumberOfBytesWritten = 0;
            Boolean writeProcessMemoryResult = kernel32.WriteProcessMemory(
                hProcess, lpBaseAddress, lpBuffer, dwSize, ref dwNumberOfBytesWritten
            );

            if (writeProcessMemoryResult)
            {
                WriteOutputGood("Section " + sectionName.Replace("\0","") + " (" + dwNumberOfBytesWritten + " bytes), Copied To " + lpBaseAddress.ToString("X4"));
                return true;
            }
            else
            {
                WriteOutputBad(
                    "Unable to write process memory" +
                    "\n\tResult:                  " + writeProcessMemoryResult +
                    "\n\tdwSize                   " + dwSize +
                    "\n\tlpBaseAddress            " + lpBaseAddress.ToString("X4") +
                    "\n\tlpBuffer                 " + lpBuffer.ToString("X4") +
                    "\n\tdwNumberOfBytesWritten   " + dwNumberOfBytesWritten
                );
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean WriteProcessMemoryUnChecked(
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            UInt32 dwSize,
            string sectionName
        )
        {
            UInt32 dwNumberOfBytesWritten = 0;
            if (IntPtr.Zero != lpBuffer)
            {
                Boolean writeProcessMemoryResult = kernel32.WriteProcessMemory(
                    hProcess, lpBaseAddress, lpBuffer, dwSize, ref dwNumberOfBytesWritten
                );

                if (writeProcessMemoryResult)
                {
                    return true;
                }
                else
                {
                    WriteOutputBad("Unable to write process memory");
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadProcessMemoryChecked(
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            UInt32 dwSize,
            string sectionName
        )
        {
            UInt32 dwNumberOfBytesRead = 0;
            Boolean readProcessMemoryResult = kernel32.ReadProcessMemory(
                hProcess, lpBaseAddress, lpBuffer, dwSize, ref dwNumberOfBytesRead
            );


            if (readProcessMemoryResult)
            {
                WriteOutputGood("Section " + sectionName.Trim() + " (" + dwNumberOfBytesRead + " bytes), Read From To " + lpBaseAddress.ToString("X4"));
                return true;
            }
            else
            {
                WriteOutputBad(
                    "Unable to read process memory" +
                    "\n\tResult:                  " + readProcessMemoryResult +
                    "\n\tdwSize                   " + dwSize +
                    "\n\tlpBaseAddress            " + lpBaseAddress.ToString("X4") +
                    "\n\tlpBuffer                 " + lpBuffer.ToString("X4") +
                    "\n\tdwNumberOfBytesRead      " + dwNumberOfBytesRead
                );
                //This is dumb
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ReadProcessMemoryUnChecked(
            IntPtr lpBaseAddress,
            IntPtr lpBuffer,
            UInt32 dwSize,
            string sectionName
        )
        {
            UInt32 dwNumberOfBytesRead = 0;
            Boolean readProcessMemoryResult = kernel32.ReadProcessMemory(
                hProcess, lpBaseAddress, lpBuffer, dwSize, ref dwNumberOfBytesRead
            );

            if (readProcessMemoryResult)
            {
                return true;
            }
            else
            {
                WriteOutputNeutral("Unable to read process memory");
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal IntPtr CreateRemoteThreadChecked(IntPtr lpStartAddress, IntPtr lpParameter)
        {
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            UInt32 dwCreationFlags = 0;
            UInt32 lpThreadId = 0;
            IntPtr hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, lpStartAddress, lpParameter, dwCreationFlags, ref lpThreadId);
            WriteOutputGood("Thread Created " + lpThreadId);
            return hThread;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal T PtrToStructureRemote<T>(IntPtr pointer)
        {
            UInt32 imageSizeOfStruct = (UInt32)Marshal.SizeOf(typeof(T));
            byte[] structBuffer = new byte[imageSizeOfStruct];
            GCHandle pinnedStructBuffer = GCHandle.Alloc(structBuffer, GCHandleType.Pinned);
            IntPtr lpStructBytes = new IntPtr((Int64)pinnedStructBuffer.AddrOfPinnedObject());
            ReadProcessMemoryUnChecked(pointer, lpStructBytes, imageSizeOfStruct, typeof(T).ToString());
            T marshaledStruct = (T)Marshal.PtrToStructure(lpStructBytes, typeof(T));
            pinnedStructBuffer.Free();
            return marshaledStruct;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Int16 ReadInt16Remote(IntPtr pointer, Int32 offset)
        {
            Int16 integer = 0;
            GCHandle pinnedInteger = GCHandle.Alloc(integer, GCHandleType.Pinned);
            WriteOutputNeutral("pinnedInteger " + pinnedInteger.ToString());
            IntPtr lpInteger = new IntPtr((Int64)pinnedInteger.AddrOfPinnedObject());
            WriteOutputNeutral("lpInteger " + lpInteger.ToString());
            IntPtr adjustedPointer = new IntPtr(pointer.ToInt64() + offset);
            WriteOutputNeutral("adjustedPointer " + adjustedPointer.ToString());
            ReadProcessMemoryUnChecked(adjustedPointer, lpInteger, sizeof(Int16), typeof(Int16).ToString());
            Int16 marshaledInt16 = Marshal.ReadInt16(lpInteger);
            pinnedInteger.Free();
            return marshaledInt16;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Int32 PtrToInt32Remote(IntPtr pointer)
        {
            Int32 integer = 0;
            GCHandle pinnedInteger = GCHandle.Alloc(integer, GCHandleType.Pinned);
            IntPtr lpInteger = new IntPtr((Int64)pinnedInteger.AddrOfPinnedObject());
            ReadProcessMemoryUnChecked(pointer, lpInteger, sizeof(Int32), typeof(Int32).ToString());
            Int32 marshaledInt16 = Marshal.ReadInt32(lpInteger);
            return marshaledInt16;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Int64 ReadInt64Remote(IntPtr pointer)
        {
            Int64 integer = 0;
            GCHandle pinnedInteger = GCHandle.Alloc(integer, GCHandleType.Pinned);
            IntPtr lpInteger = new IntPtr((Int64)pinnedInteger.AddrOfPinnedObject());
            ReadProcessMemoryUnChecked(pointer, lpInteger, sizeof(Int64), typeof(Int64).ToString());
            Int64 marshaledInt16 = Marshal.ReadInt64(lpInteger);
            pinnedInteger.Free();
            return marshaledInt16;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean WriteInt64Remote(IntPtr pointer, Int64 value)
        {
            GCHandle pinnedInteger = GCHandle.Alloc(value, GCHandleType.Pinned);
            IntPtr lpInteger = new IntPtr((Int64)pinnedInteger.AddrOfPinnedObject());
            Boolean result = WriteProcessMemoryUnChecked(
                pointer,
                lpInteger,
                (UInt32)sizeof(Int64),
                ""
            );
            pinnedInteger.Free();
            return result;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal string PtrToStringAnsiRemote(IntPtr pointer)
        {
            int offset = 0;
            string stringResult = "";
            byte character = new byte();
            GCHandle pinnedCharacter = GCHandle.Alloc(character, GCHandleType.Pinned);
            IntPtr lpCharacter = new IntPtr((Int64)pinnedCharacter.AddrOfPinnedObject());
            char marshaledChar;
            do {
                IntPtr adjustedPointer = new IntPtr(pointer.ToInt64() + offset++);
                ReadProcessMemoryUnChecked(adjustedPointer, lpCharacter, sizeof(char), typeof(char).ToString());
                marshaledChar = (char)Marshal.ReadByte(lpCharacter);
                stringResult += marshaledChar;
            } while (marshaledChar != '\0');
            pinnedCharacter.Free();
            return stringResult;
        }

        internal IntPtr LoadLibraryRemote(string library)
        {
            ////////////////////////////////////////////////////////////////////////////////
            IntPtr hmodule = kernel32.GetModuleHandle("kernel32.dll");
            IntPtr loadLibraryAddr = kernel32.GetProcAddress(hmodule, "LoadLibraryA");

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpAddress = IntPtr.Zero;
            UInt32 dwSize = (UInt32)((library.Length + 1) * Marshal.SizeOf(typeof(char)));
            IntPtr lpBaseAddress = kernel32.VirtualAllocEx(hProcess, lpAddress, dwSize, kernel32.MEM_COMMIT | kernel32.MEM_RESERVE, Winnt.PAGE_READWRITE);

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpNumberOfBytesWritten = 0;
            IntPtr libraryPtr = Marshal.StringToHGlobalAnsi(library);
            Boolean writeProcessMemoryResult = kernel32.WriteProcessMemory(hProcess, lpBaseAddress, libraryPtr, dwSize, ref lpNumberOfBytesWritten);

            ////////////////////////////////////////////////////////////////////////////////
            UInt32 lpflOldProtect = 0;
            Boolean virtualProtectExResult = kernel32.VirtualProtectEx(hProcess, lpBaseAddress, dwSize, Winnt.PAGE_EXECUTE_READ, ref lpflOldProtect);

            ////////////////////////////////////////////////////////////////////////////////
            IntPtr lpThreadAttributes = IntPtr.Zero;
            UInt32 dwStackSize = 0;
            IntPtr lpParameter = IntPtr.Zero;
            UInt32 dwCreationFlags = 0;
            UInt32 threadId = 0;
            IntPtr hThread = kernel32.CreateRemoteThread(hProcess, lpThreadAttributes, dwStackSize, loadLibraryAddr, lpBaseAddress, dwCreationFlags, ref threadId);
            return hThread;
        }

        internal void WaitForSingleObjectExRemote(IntPtr hThread)
        {
            kernel32.WaitForSingleObjectEx(hProcess, hThread, 0xFFFFFFFF);
        }
    }
}
