using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class ServicePrincipalName : DomainUsers
    {
        private const String USERFILTER = "(servicePrincipalName=*)";
        private const String COMPUTERFILTER = "(&(objectCategory=computer)(servicePrincipalName=*))";
        private const String PATH = "";

        public ServicePrincipalName(String server)
            : base(server)
        {
        }

        public ServicePrincipalName(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void QueryUsers()
        {
            Console.WriteLine("[*] Querying Domain Service Principal Names (Users)");
            QueryUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }

        public void QueryComputers()
        {
            Console.WriteLine("[*] Querying Domain Service Principal Names (Computers)");
            QueryUsers(COMPUTERFILTER);
        }
    }
}