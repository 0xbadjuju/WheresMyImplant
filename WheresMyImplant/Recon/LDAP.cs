using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.DirectoryServices;
using System.IO;

namespace DomainInfo
{
    class LDAP : IDisposable
    {
        private DirectoryEntry directoryEntry;
        private DirectorySearcher directorySearcher;

        private String domainName;
        private String netbiosName;
        private String folderPath = ".";

        protected SearchResultCollection ldapQueryResult;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public LDAP(String server)
        {
            directoryEntry = new DirectoryEntry("LDAP://" + server);
            InitialCollection();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public LDAP(String server, String username, String password)
        {
            directoryEntry = new DirectoryEntry("LDAP://" + server, username, password);
            InitialCollection();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private Boolean InitialCollection()
        {
            try
            {
                if (directoryEntry.Properties.Count > 0)
                {
                    Object kluge = directoryEntry.NativeObject;
                    directoryEntry.AuthenticationType = AuthenticationTypes.Secure;
                    directorySearcher = new DirectorySearcher(directoryEntry);
                    netbiosName = directoryEntry.Name.Remove(0, 3);
                    domainName = ((String)directoryEntry.Properties["distinguishedName"].Value).Remove(0, 3).Replace(",DC=", ".");
                    Console.WriteLine("Using {0} ({1})", domainName, netbiosName);
                    return true;
                }
                return false;
            }
            catch (System.Runtime.InteropServices.COMException ComException)
            {
                Console.WriteLine("Connection Failed: ");
                Console.WriteLine(ComException.ToString());
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public virtual Boolean SetFolderPath(String folderPath)
        {   
            if (!Directory.Exists(folderPath))
            {
                try
                {
                    Directory.CreateDirectory(folderPath);
                }
                catch (Exception ex)
                {
                    if (ex is NotSupportedException || ex is IOException)
                        Console.WriteLine(ex.Message);
                    else
                        Console.WriteLine(ex.ToString());
                    return false;
                }
            }
            this.folderPath = folderPath;
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Query(String ldapQuery)
        {
            try
            {
                directorySearcher.Filter = ldapQuery;
                ldapQueryResult = directorySearcher.FindAll();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void OutCsv(String fileName)
        {
            string path = folderPath + @"\" + fileName;
            
            if (!File.Exists(path))
            {
                StreamWriter streamWriter = File.CreateText(path);
                streamWriter.Close();
            }

            using (StreamWriter streamWriter = File.AppendText(path))
            {
                foreach (SearchResult result in ldapQueryResult)
                {
                    //Write out the headers, and get the number of items
                    foreach (String property in result.Properties.PropertyNames)
                    {
                        streamWriter.Write("\"" + property + "\",");
                    }
                }
                streamWriter.Write("\n");

                foreach (SearchResult result in ldapQueryResult)
                {
                    foreach (String property in result.Properties.PropertyNames)
                    {
                        try
                        {
                            streamWriter.Write("\"");
                            for (Int32 i = 0; i < result.Properties[property].Count; i++)
                            {
                                streamWriter.Write(result.Properties[property][i]);
                                if (result.Properties[property].Count > 1)
                                {
                                    Console.Write(".");
                                }
                            }
                            streamWriter.Write("\",");
                        }
                        catch (IndexOutOfRangeException exception)
                        {
                            Console.WriteLine(exception.ToString());
                        }
                    }
                    streamWriter.Write("\n");
                }
                Console.WriteLine();
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        ~LDAP()
        {
            Dispose();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            if (null != directoryEntry)
            {
                directoryEntry.Close();
                directoryEntry.Dispose();
                directoryEntry = null;
            }
        }
    }
}
