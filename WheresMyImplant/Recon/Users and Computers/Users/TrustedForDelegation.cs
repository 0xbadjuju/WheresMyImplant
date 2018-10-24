using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class TrustedForDelegation : DomainUsers
    {
        private const String FILTER = "(|(UserAccountControl:1.2.840.113556.1.4.803:=524288)(UserAccountControl:1.2.840.113556.1.4.803:=16777216)))";
        private const String PATH = "";

        public TrustedForDelegation(String server)
            : base(server)
        {
        }

        public TrustedForDelegation(String server, String username, String password)
            : base(server, username, password)
        {
        }

        public void QueryUsers()
        {
            QueryUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }

        public void QueryProtectedUsers()
        {
            QueryProtectedUsers("(&" + DomainUsers.FILTER + FILTER + ")");
        }
    }
}