using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;

namespace DomainInfo
{
    class DomainUsers : LDAP
    {
        //Active Domain Users
        protected const String FILTER = "(&(samAccountType=805306368)(!(userAccountControl:1.2.840.113556.1.4.803:=2)))";
        protected const String ADMINCOUNT = "(adminCount=1)";
        private const String PATH = "";


        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public DomainUsers(String server)
            : base(server)
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public DomainUsers(String server, String username, String password)
            : base(server, username, password)
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Query()
        {
            Console.WriteLine("[*] Querying Domain Users");
            base.Query(FILTER);
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Get Protected Users
        ////////////////////////////////////////////////////////////////////////////////
        public void QueryAdminCount()
        {
            Console.WriteLine("[*] Querying Domain Users (AdminCount=1)");
            base.Query("(&" + FILTER + ADMINCOUNT + ")");
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Used to query for specific properties - eg kerberos preauth
        ////////////////////////////////////////////////////////////////////////////////
        public virtual void QueryUsers(String AdditionalFilter)
        {
            base.Query("(&" + FILTER + AdditionalFilter + ")");
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Used to query for specific properties - eg kerberos preauth
        ////////////////////////////////////////////////////////////////////////////////
        public virtual void QueryProtectedUsers(String AdditionalFilter)
        {
            base.Query("(&" + FILTER + ADMINCOUNT + AdditionalFilter + ")");
        }
    }
}