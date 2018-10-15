using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class FSMORoles : LDAP
    {
        private const String FSMOROLES = "(&(objectClass=*)(fSMORoleOwner=*))";

        private const String PDCEMULATOR = "(&(objectClass=*)(fSMORoleOwner=*))";

        private const String RIDMASTER = "(&(objectClass=rIDManager)(fSMORoleOwner=*))";
        private const String SCHEMAMASTER = "(&(objectClass=schemaNamingContext)(fSMORoleOwner=*))";
        private const String DOMAINNAMINGMASTER = "(&(objectClass=crossRefContainer)(fSMORoleOwner=*))";
        private const String INFRASTRUCTUREMASTER = "(&(objectClass=infrastructureUpdate)(fSMORoleOwner=*))";
        private const String DOMAINDNSZONEMASTER = "(&(objectClass=domainDNS)(fSMORoleOwner=*))";
        private const String FORESTDNSZONEMASTER = "(&(objectClass=*)(fSMORoleOwner=*))";


        public FSMORoles(String server)
            : base(server)
        {
        }

        public FSMORoles(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void Query()
        {
            Console.WriteLine("[*] Querying Domain Controllers");
            //Query(DOMAINCONTROLLER);
        }
    }
}
