### WheresMyImplant: A C# WMI Provider for long term persistance

This WMI provider includes functions to execute commands, payloads, and Empire Agent to maintain a low profile on the host.

### Methods

* RunCMD
  * Parameter: Command, Parameters
  
* RunPowerShell
  * Parameter: Command
  
* RunXpCmdShell
  * Parameter: Server, Database, UserName, Password, Command
  
* InjectShellCode
  * Parameter: ShellCodeString, ProcessId
  
* InjectShellCodeWMFIFSB4
  * Parameter: WmiClass, FileName, ProcessId
  
* InjectDll
  * Parameter: Library, ProcessId
  
* InjectDllWMIFS
  * WmiClass, FileName, ProcessId
  
* InjectPeFile
  * Parameter: FileName, Parameters, ProcessId
  
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
