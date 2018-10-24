using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class OrganizationalUnit : LDAP
    {
        private const String FILTER = "(objectCategory=subnet)";
        private const String PATH = "CN=Subnets,CN=Sites,CN=Configuration";

        public OrganizationalUnit(String server)
            : base(server)
        {
        }

        public OrganizationalUnit(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Query(FILTER);
        }
    }
}