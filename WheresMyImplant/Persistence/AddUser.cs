using System;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;

namespace WheresMyImplant
{
    sealed class AddUser
    {
        internal AddUser()
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void AddLocalUser(String username, String password)
        {
            try
            {
                DirectoryEntry computer = new DirectoryEntry(String.Format("WinNT://{0},computer", Environment.MachineName));
                DirectoryEntry user = computer.Children.Add(username, "user");
                user.Invoke("SetPassword", password);
                user.CommitChanges();
            }
            catch (Exception ex)
            {
                if (ex is DirectoryServicesCOMException)
                {
                    Console.WriteLine("User Likely Exists");
                }
                Console.WriteLine(ex.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void AddLocalAdmin(String username, String password)
        {
            using (var computer = new DirectoryEntry(String.Format("WinNT://{0},computer", Environment.MachineName)))
            {
                DirectoryEntry user = null;
                try
                {
                    user = computer.Children.Add(username, "user");
                    user.Invoke("SetPassword", password);
                    user.CommitChanges();
                }
                catch (Exception ex)
                {
                    if (ex is DirectoryServicesCOMException)
                    {
                        Console.WriteLine("User Likely Exists");
                    }
                    Console.WriteLine(ex.Message);
                }

                try
                {
                    DirectoryEntry group = computer.Children.Find("Administrators");
                    group.Invoke("Add", user.Path);
                    group.CommitChanges();
                }
                catch (Exception ex)
                {
                    if (ex is DirectoryServicesCOMException)
                    {
                        Console.WriteLine("User Likely Already Member of Group");
                    }
                    Console.WriteLine(ex.Message);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void AddDomainUser(String username, String email, String password)
        {
            try
            {
                using (var principalContext = new PrincipalContext(ContextType.Domain))
                {
                    using (var userPrincipal = new UserPrincipal(principalContext))
                    {
                        userPrincipal.SamAccountName = username;
                        userPrincipal.EmailAddress = email;
                        userPrincipal.SetPassword(password);
                        userPrincipal.Enabled = true;
                        userPrincipal.Save();
                    }
                }
            }
            catch (Exception ex)
            {
                if (ex is DirectoryServicesCOMException)
                {
                    Console.WriteLine("User Likely Exists");
                }
                Console.WriteLine(ex.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void AddDomainUserToDomainGroup(String domain, String userId, String groupName)
        {
            try
            {
                using (var principalContext = new PrincipalContext(ContextType.Domain, domain))
                {
                    var group = GroupPrincipal.FindByIdentity(principalContext, groupName);
                    group.Members.Add(principalContext, IdentityType.UserPrincipalName, userId);
                    group.Save();
                }
            }
            catch (Exception ex)
            {
                if (ex is DirectoryServicesCOMException)
                {
                    Console.WriteLine("User Likely Already Member of Group");
                }
                Console.WriteLine(ex.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void RemoveDomainUserToDomainGroup(String userId, String groupName, String domain)
        {
            try
            {
                using (var principalContext = new PrincipalContext(ContextType.Domain, domain))
                {
                    var group = GroupPrincipal.FindByIdentity(principalContext, groupName);
                    group.Members.Remove(principalContext, IdentityType.UserPrincipalName, userId);
                    group.Save();
                }
            }
            catch (Exception ex)
            {
                if(ex is DirectoryServicesCOMException)
                {
                    Console.WriteLine("User Likely not a Member of the Group");
                }
                Console.WriteLine(ex.Message);
            }
        }
    }
}