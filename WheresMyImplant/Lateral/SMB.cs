using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace WheresMyImplant
{
    abstract class SMB : Base, IDisposable
    {
        protected TcpClient smbClient;
        protected NetworkStream streamSocket;
        protected String system;

        protected Byte[] recieve = new Byte[81920];

        private static readonly Byte[] status_okay = { 0x00, 0x00, 0x00, 0x00 };
        private static readonly Byte[] status_access_denied = new Byte[] { 0x22, 0x00, 0x00, 0xc0 };
        private static readonly Byte[] status_file_closed = new Byte[] { 0x28, 0x01, 0x00, 0xc0 };
        private static readonly Byte[] status_file_not_found = new Byte[] { 0x34, 0x01, 0x00, 0xc0 };
        private static readonly Byte[] status_object_name_not_found = new Byte[] { 0x34, 0x00, 0x00, 0xc0 };
        private static readonly Byte[] status_object_path_not_found = new Byte[] { 0x3a, 0x00, 0x00, 0xc0 };
        private static readonly Byte[] status_network_name_deleted = new Byte[] { 0xc9, 0x00, 0x00, 0xc0 };
        private static readonly Byte[] status_invalid_parameter = new Byte[] { 0x0d, 0x00, 0x00, 0xc0 };

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        protected Boolean GetStatus(Byte[] status)
        {
            if (status.SequenceEqual(status_okay))
            {
                return true;
            }
            else if (status.SequenceEqual(status_access_denied))
            {
                Console.WriteLine("[-] Access Denied");
                return false;
            }
            else if (status.SequenceEqual(status_file_closed))
            {
                Console.WriteLine("[-] File Closed");
                return false;
            }
            else if (status.SequenceEqual(status_file_not_found) || status.SequenceEqual(status_object_name_not_found))
            {
                Console.WriteLine("[-] File Not Found");
                return false;
            }
            else if (status.SequenceEqual(status_object_path_not_found))
            {
                Console.WriteLine("[-] Directory Not Found");
                return false;
            }
            else if (status.SequenceEqual(status_invalid_parameter))
            {
                Console.WriteLine("[-] Invalid Parameter");
                return false;
            }
            else if (status.SequenceEqual(status_network_name_deleted))
            {
                Console.WriteLine("[-] Network Name Deleted");
                return false;
            }
            else
            {
                Console.WriteLine("[-] " + BitConverter.ToString(status));
                return false;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public SMB()
        {
            smbClient = new TcpClient();
            smbClient.Client.ReceiveTimeout = 30000;
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create Network Layer Connection
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean Connect(String system)
        {
            this.system = system;

            try
            {
                smbClient.Connect(system, 445);
            }
            catch (Exception)
            {
                return false;
            }

            if (!smbClient.Connected)
            {
                return false;
            }
            streamSocket = smbClient.GetStream();
            return true;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public void Dispose()
        {
            

            if (null != smbClient && smbClient.Connected)
                smbClient.Close();

            if (null != streamSocket)
                streamSocket.Close(60);
        }
    }
}
