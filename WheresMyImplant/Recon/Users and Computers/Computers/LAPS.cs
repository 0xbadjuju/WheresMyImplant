using System;
using System.DirectoryServices;

namespace DomainInfo
{
    class LAPS : LDAP
    {
        private const String FILTER = "(&(objectCategory=computer)(ms-MCS-AdmPwd=*))";

        public LAPS(String server)
            : base(server)
        {
        }

        public LAPS(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Console.WriteLine("[*] Querying Domain Computers");
            Query(FILTER);
        }

        public void Print()
        {
            Console.WriteLine("{0,-20} {1,-30} {2}", "Name", "Operating System", "Administrator Password");
            Console.WriteLine("{0,-20} {1,-30} {2}", "----", "----------------", "----------------------");
            try
            {
                foreach (SearchResult result in ldapQueryResult)
                {
                    String name = "";
                    if (0 < result.Properties["name"].Count)
                        name = (String)result.Properties["name"][0];

                    String operatingsystem = "";
                    if (0 < result.Properties["operatingsystem"].Count)
                        operatingsystem = (String)result.Properties["operatingsystem"][0];

                    String admPwd = "";
                    if (0 < result.Properties["ms-MCS-AdmPwd"].Count)
                        admPwd = (String)result.Properties["ms-MCS-AdmPwd"][0];

                    Console.WriteLine("{0,-20} {1,-30} {2}", name, operatingsystem, admPwd);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
