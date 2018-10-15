using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class DomainGroups: LDAP
    {
        //Active Domain Users
        protected const String FILTER = "(&(objectClass=group))";
        private const String PATH = "";


        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public DomainGroups(String server)
            : base(server)
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public DomainGroups(String server, String username, String password)
            : base(server, username, password)
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Query()
        {
            Console.WriteLine("[*] Querying Domain Groups");
            base.Query(FILTER);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void QueryGroupMembers()
        {
            String folderPath = ".";
            String fileName = "GroupMembers.csv";

            string path = folderPath + @"\" + fileName;

            if (!File.Exists(path))
            {
                StreamWriter streamWriter = File.CreateText(path);
                streamWriter.Close();
            }

            using (StreamWriter streamWriter = File.AppendText(path))
            {
                streamWriter.Write("\"Group\",\"User\"\n");
                //This may not be needed
                SearchResultCollection src = ldapQueryResult;
                foreach (SearchResult member in src)
                {
                    if (0 == member.Properties["samaccountname"].Count)
                    {
                        continue;
                    }
                    String group = (String)member.Properties["samaccountname"][0];
                    //Console.Write("[*] Querying Domain Group Members {0}", group);

                    String additionalFilter = String.Format("(samaccountname={0})", member.Properties["samaccountname"][0]);
                    base.Query("(&" + FILTER + additionalFilter + ")");
                    foreach (SearchResult user in ldapQueryResult)
                    {
                        foreach(String cn in user.Properties["member"])
                        {
                            streamWriter.Write("\"{0}\",\"{1}\"\n", group, cn);
                        }
                    }
                }
            }
        }
    }
}