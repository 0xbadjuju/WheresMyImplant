namespace WheresMyImplant
{
    public sealed class Run
    {
        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void RunCMD(string command, string parameters)
        {
            RunCommandPrompt runCommandPrompt = new RunCommandPrompt(command, parameters);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void RunPowerShell(string command)
        {
            RunPowerShell runPowerShell = new RunPowerShell(command);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // 
        ////////////////////////////////////////////////////////////////////////////////
        public static void RunXpCmdShell(string server, string database, string username, string password, string command)
        {
            RunXPCmdShell runXPCmdShell = new RunXPCmdShell(server, database, username, password, command);
        }
    }
}
