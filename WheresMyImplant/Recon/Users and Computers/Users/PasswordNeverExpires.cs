using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class PasswordNeverExpires : DomainUsers
    {
        private const String FILTER = "(userAccountControl:1.2.840.113556.1.4.803:=65536)";
        private const String PATH = "";

        public PasswordNeverExpires(String server)
            : base(server)
        {
        }

        public PasswordNeverExpires(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void QueryUsers()
        {
            Console.WriteLine("[*] Querying Domain DontExpirePassword Users");
            QueryUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }

        public void QueryProtectedUsers()
        {
            Console.WriteLine("[*] Querying Domain DontExpirePassword Users (AdminCount=1)");
            QueryProtectedUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }
    }
}