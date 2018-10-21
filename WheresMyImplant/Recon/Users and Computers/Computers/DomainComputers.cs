using System;
using System.DirectoryServices;

namespace DomainInfo
{
    class DomainComputers : LDAP
    {
        private const String DOMAINCOMPUTERS = "(&(objectCategory=computer))";

        public DomainComputers(String server)
            : base(server)
        {
        }

        public DomainComputers(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Console.WriteLine("[*] Querying Domain Computers");
            Query(DOMAINCOMPUTERS);
        }

        public void Print()
        {
            Console.WriteLine("{0,-20} {1}", "Name", "Operating System");
            Console.WriteLine("{0,-20} {1}", "----", "----------------");
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

                    Console.WriteLine("{0,-20} {1}", name, operatingsystem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}
