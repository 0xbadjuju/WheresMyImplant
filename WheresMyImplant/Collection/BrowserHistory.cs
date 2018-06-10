using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

using Unmanaged.Libraries;

namespace WheresMyImplant
{
    class BrowserHistory : Base
    {
        Regex url = new Regex(@"https?:\/\/([\w-]+\.)+[\w-]+\/[\w- ./?%&=]*");

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal BrowserHistory()
        {
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Chrome()
        {
            Regex bookmarkUrl = new Regex(@""".*url"": ""(.+)""");
            WriteOutput("");
            WriteOutputNeutral("Chrome");
            String file = Environment.GetEnvironmentVariable("LOCALAPPDATA") + @"\Google\Chrome\User Data\Default\Bookmarks";
            if (!File.Exists(file))
            {
                WriteOutputBad("Chrome not installed");
                return;
            }

            WriteOutput("");
            WriteOutputGood("Bookmarks");
            using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader streamReader = new StreamReader(fileStream, System.Text.Encoding.ASCII))
                {
                    do
                    {
                        String line = streamReader.ReadLine();
                        Match match = bookmarkUrl.Match(line);
                        if (!match.Success)
                        {
                            continue;
                        }

                        GroupCollection groups = match.Groups;
                        System.Collections.IEnumerator enumerator = groups.GetEnumerator();
                        enumerator.MoveNext(); enumerator.MoveNext();
                        WriteOutput(enumerator.Current.ToString());
                    }
                    while(!streamReader.EndOfStream);
                }
            }
            WriteOutput("");
            WriteOutputGood("History");
            String historyFile = Environment.GetEnvironmentVariable("LOCALAPPDATA") + @"\Google\Chrome\User Data\Default\History";
            if (!File.Exists(file))
            {
                WriteOutputBad("Chrome not installed");
                return;
            }

            using (FileStream fileStream = new FileStream(historyFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (StreamReader streamReader = new StreamReader(fileStream, System.Text.Encoding.ASCII))
                {
                    String content = streamReader.ReadToEnd();
                    String[] lines = content.Split(new String[] { "http" }, StringSplitOptions.RemoveEmptyEntries);
                    WriteOutput(lines.Length.ToString());
                    foreach (String line in lines)
                    {
                        Match match = url.Match("http"+line);
                        if (!match.Success)
                        {
                            continue;
                        }

                        GroupCollection groups = match.Groups;
                        System.Collections.IEnumerator enumerator = groups.GetEnumerator();
                        enumerator.MoveNext();
                        WriteOutput(enumerator.Current.ToString());
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void Firefox()
        {
            WriteOutput("");
            WriteOutputNeutral("Firefox");
            String[] historyFiles;
            try
            {
                String path = Environment.GetEnvironmentVariable("APPDATA") + @"\Mozilla\Firefox\Profiles\";
                historyFiles = Directory.GetFiles(path, "places.sqlite", SearchOption.AllDirectories);
            }
            catch (DirectoryNotFoundException)
            {
                WriteOutputBad("Firefox not installed");
                return;
            }

            WriteOutput("");
            WriteOutputGood("History");
            foreach (String file in historyFiles)
            {
                Console.WriteLine(file);
                String content;
                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(fileStream, System.Text.Encoding.ASCII))
                    {
                        content = streamReader.ReadToEnd();
                    }
                }
                Byte[] delimiter = { 0x25 };
                String strDelimiter = System.Text.Encoding.ASCII.GetString(delimiter);
                String[] lines = content.Split(new String[] {strDelimiter}, StringSplitOptions.RemoveEmptyEntries);
                foreach (String line in lines)
                {
                    Match match = url.Match(line);
                    if (!match.Success)
                    {
                        continue;
                    }

                    GroupCollection groups = match.Groups;
                    System.Collections.IEnumerator enumerator = groups.GetEnumerator();
                    enumerator.MoveNext();
                    WriteOutput(enumerator.Current.ToString());
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void InternetExplorer()
        {
            WriteOutput("");
            WriteOutputNeutral("Internet Explorer");
            WriteOutput("");
            WriteOutputGood("Bookmarks");
            String[] bookmarks = Directory.GetFiles(@"C:\" + Environment.GetEnvironmentVariable("HOMEPATH") + @"\Favorites", "*.url", SearchOption.AllDirectories);
            foreach (String bookmark in bookmarks)
            {
                System.Text.StringBuilder lpReturnedString = new System.Text.StringBuilder(260);
                kernel32.GetPrivateProfileString("InternetShortcut", "URL", "ERROR", lpReturnedString, 260, bookmark);
                WriteOutput(String.Format("{0,-25}{1, -35}", bookmark.Replace(".url", "").Split('\\').LastOrDefault(), lpReturnedString.ToString()));
            }
            WriteOutput("");
            WriteOutputGood("History");
            RegistryKey urls = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Internet Explorer\TypedURLs");
            foreach(String value in urls.GetValueNames())
            {
                WriteOutput((String)urls.GetValue(value));
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        ~BrowserHistory()
        {
        }
    }
}