using System;
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
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Print()
        {
            Console.WriteLine("{0,-50} {1}", "Name", "AdminCount");
            Console.WriteLine("{0,-50} {1}", "----", "----------");
            try
            {
                foreach (SearchResult result in ldapQueryResult)
                {
                    String name = "";
                    if (0 < result.Properties["name"].Count)
                        name = (String)result.Properties["name"][0];

                    String admincount = "";
                    if (0 < result.Properties["admincount"].Count)
                        admincount = 1 == (Int32)result.Properties["admincount"][0] ? "True" : "";

                    String description = "";
                    if (0 < result.Properties["description"].Count)
                        description = (String)result.Properties["description"][0];

                    Console.WriteLine("{0,-50} {1,-10} {2}", name, admincount, description);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //(&(objectCategory=user)(memberOf={group distinguished name}))
        ////////////////////////////////////////////////////////////////////////////////
        public void QueryUserGroups(String name)
        {
            Console.WriteLine("[*] Querying User Groups");
            base.Query(String.Format("(&(objectClass=user)(samAccountName={0}))", name));

            try
            {
                foreach (SearchResult result in ldapQueryResult)
                {
                    if (0 < result.Properties["distinguishedName"].Count)
                    {
                        name = (String)result.Properties["distinguishedName"][0];
                    }
                    else
                    {
                        Console.WriteLine("[-] Group Not Found");
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.WriteLine("[+] Using Distinguished Name: {0}", name);
            base.Query(String.Format("(&(objectCategory=group)(member={0}))", name));
            try
            {
                Console.WriteLine("[+] {0} Groups Found\n", ldapQueryResult.Count);
                Console.WriteLine("{0,-50} {1}", "Group Name", "AdminCount");
                Console.WriteLine("{0,-50} {1}", "----------", "----------");
                foreach (SearchResult result in ldapQueryResult)
                {
                    if (0 < result.Properties["name"].Count)
                        name = (String)result.Properties["name"][0];

                    String admincount = "";
                    if (0 < result.Properties["admincount"].Count)
                        admincount = 1 == (Int32)result.Properties["admincount"][0] ? "True" : "";

                    Console.WriteLine("{0,-50} {1}", name, admincount);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
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