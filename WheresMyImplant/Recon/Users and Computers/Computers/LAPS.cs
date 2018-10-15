using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
            Console.WriteLine("[*] Querying Domain Controllers");
            Query(FILTER);
        }
    }
}
