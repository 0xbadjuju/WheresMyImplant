using System;
using System.DirectoryServices;

namespace DomainInfo
{
    class DomainGroups: LDAP
    {
        //Active Domain Users
        protected const String FILTER = "(&(objectClass=group))";
        private const String PATH = "";


        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public DomainGroups(String server)
            : base(server)
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public DomainGroups(String server, String username, String password)
            : base(server, username, password)
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Query()
        {
            Console.WriteLine("[*] Querying Domain Groups");
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
        public void QueryGroupMembers(String name)
        {
            Console.WriteLine("[*] Querying Group Membership");
            base.Query(String.Format("(&(objectClass=group)(name={0}))", name));

            try
            {
                foreach (SearchResult result in ldapQueryResult)
                {
                    if (0 < result.Properties["distinguishedname"].Count)
                    {
                        name = (String)result.Properties["distinguishedname"][0];
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

            base.Query(String.Format("(&(objectCategory=user)(memberOf={0}))", name));
            Console.WriteLine("[+] Using Distinguished Name: {0}", name);
            try
            {
                Console.WriteLine("[+] {0} Users Found\n", ldapQueryResult.Count);
                Console.WriteLine("{0,-50} {1}", "Name", "AdminCount");
                Console.WriteLine("{0,-50} {1}", "----", "----------");
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
    }
}