using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class DomainControllers : LDAP
    {
        private const String DOMAINCONTROLLER = "(&(objectCategory=computer)(userAccountControl:1.2.840.113556.1.4.803:=8192))";

        public DomainControllers(String server)
            : base(server)
        {
        }

        public DomainControllers(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Console.WriteLine("[*] Querying Domain Controllers");
            Query(DOMAINCONTROLLER);
        }
    }
}
