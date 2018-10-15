using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class KerberosPreauthentication : DomainUsers
    {
        private const String FILTER = "(userAccountControl:1.2.840.113556.1.4.803:=4194304)";
        private const String PATH = "";

        public KerberosPreauthentication(String server)
            : base(server)
        {
        }

        public KerberosPreauthentication(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void QueryUsers()
        {
            Console.WriteLine("[*] Querying Domain KRB PreAuth Users");
            QueryUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }

        public void QueryProtectedUsers()
        {
            Console.WriteLine("[*] Querying Domain KRB PreAuth Users (AdminCount=1)");
            QueryUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }
    }
}