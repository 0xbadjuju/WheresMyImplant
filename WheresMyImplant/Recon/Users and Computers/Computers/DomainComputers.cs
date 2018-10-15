using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
    }
}
