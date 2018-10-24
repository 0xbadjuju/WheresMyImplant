using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class Site : LDAP
    {
        private const String FILTER = "(objectCategory=site)";
        private const String PATH = "CN=Sites,CN=Configuration";

        public Site(String server)
            : base(server)
        {
        }

        public Site(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Query(FILTER);
        }
    }
}