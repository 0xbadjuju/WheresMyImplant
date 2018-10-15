using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class TrustedDomain : LDAP
    {
        private const String TRUSTEDDOMAIN = "(objectClass=trustedDomain)";

        public TrustedDomain(String server)
            : base(server)
        {
        }

        public TrustedDomain(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Query(TRUSTEDDOMAIN);
        }
    }
}