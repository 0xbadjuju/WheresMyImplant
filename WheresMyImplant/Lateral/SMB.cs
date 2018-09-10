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
        private static readonly Byte[] status_network_name_deleted = new Byte[] { 0xc9, 0x00, 0x00, 0xc0 };

        protected Boolean GetStatus(Byte[] status)
        {
            if (status.SequenceEqual(status_okay))
            {
                return true;
            }
            else if (status.SequenceEqual(status_access_denied))
            {
                WriteOutput("[-] Access Denied");
                return false;
            }
            else if (status.SequenceEqual(status_file_closed))
            {
                WriteOutput("[-] File Closed");
                return false;
            }
            else if (status.SequenceEqual(status_file_not_found))
            {
                WriteOutput("[-] File Not Found");
                return false;
            }
            else if (status.SequenceEqual(status_network_name_deleted))
            {
                WriteOutput("[-] Network Name Deleted");
                return false;
            }
            else
            {
                WriteOutput("[-] " + BitConverter.ToString(status));
                return false;
            }
        }

        public SMB()
        {
            smbClient = new TcpClient();
        }

        ////////////////////////////////////////////////////////////////////////////////
        // Create Network Layer Connection
        ////////////////////////////////////////////////////////////////////////////////
        public Boolean Connect(String system)
        {
            this.system = system;
            smbClient.Client.ReceiveTimeout = 30000;

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
            if (null != streamSocket)
            {
                streamSocket.Dispose();
            }

            if (null != smbClient && smbClient.Connected)
            {
                smbClient.Close();
            }
        }
    }
}
