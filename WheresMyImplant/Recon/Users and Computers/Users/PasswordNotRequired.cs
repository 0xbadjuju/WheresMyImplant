using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class PasswordNotRequired : DomainUsers
    {
        private const String FILTER = "(userAccountControl:1.2.840.113556.1.4.803:=32)";
        private const String PATH = "";

        public PasswordNotRequired(String server)
            : base(server)
        {
        }

        public PasswordNotRequired(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void QueryUsers()
        {
            Console.WriteLine("[*] Querying Domain PasswordNotRequired Users");
            QueryUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }

        public void QueryProtectedUsers()
        {
            Console.WriteLine("[*] Querying Domain PasswordNotRequired Users");
            QueryProtectedUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }
    }
}