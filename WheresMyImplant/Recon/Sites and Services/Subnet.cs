using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class Subnet : LDAP
    {
        private const String FILTER = "(objectCategory=subnet)";
        private const String PATH = "CN=Subnets,CN=Sites,CN=Configuration";

        public Subnet(String server)
            : base(server)
        {
        }

        public Subnet(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Query(FILTER);
        }
    }
}