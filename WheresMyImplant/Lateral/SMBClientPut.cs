using System;
using System.IO;
using System.Linq;

namespace WheresMyImplant
{
    sealed class SMBClientPut : SMBClient
    {
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public SMBClientPut() : base()
        {
            
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean CreateRequest(String folder, Int32 step)
        {
            treeId = recieve.Skip(40).Take(4).ToArray();

            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x05, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2CreateRequest createRequest = new SMB2CreateRequest();
            if (!String.IsNullOrEmpty(folder))
                createRequest.SetFileName(folder);
            createRequest.SetExtraInfo(2, 0);
            if (1 == step)
            {
                createRequest.SetCreateOptions(new Byte[] { 0x60, 0x00, 0x20, 0x00 });
                createRequest.SetAccessMask(new Byte[] { 0x89, 0x00, 0x12, 0x00 });
                createRequest.SetShareAccess(new Byte[] { 0x01, 0x00, 0x00, 0x00 });
            }
            else if (2 == step)
            {
                createRequest.SetCreateOptions(new Byte[] { 0x64, 0x00, 0x20, 0x00 });
                createRequest.SetAccessMask(new Byte[] { 0x97, 0x01, 0x13, 0x00 });
                createRequest.SetShareAccess(new Byte[] { 0x00, 0x00, 0x00, 0x00 });
                createRequest.SetFileAttributes(new Byte[] { 0x20, 0x00, 0x00, 0x00 });
                createRequest.SetDisposition(new Byte[] { 0x05, 0x00, 0x00, 0x00 });
            }
            createRequest.SetRequestedOplockLevel(new Byte[] { 0xff });
            Byte[] bData = createRequest.GetRequest();

            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(Misc.Combine(bSessionService, bHeader), bData);
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
            {
                guidFileHandle = recieve.Skip(0x0084).Take(16).ToArray();
                return true;
            }
            return false;
        }
    
        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean GetInfoRequest()
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x10, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2GetInfo getInfo = new SMB2GetInfo();
            getInfo.SetClass(new Byte[] { 0x02 });
            getInfo.SetInfoLevel(new Byte[] { 0x01 });
            getInfo.SetMaxResponseSize(new Byte[] { 0x58, 0x00, 0x00, 0x00 });
            getInfo.SetGetInfoInputOffset(new Byte[] { 0x00, 0x00 });
            getInfo.SetGUIDHandleFile(guidFileHandle);
            Byte[] bData = getInfo.GetRequest();

            header.SetChainOffset(bData.Length);
            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            
            Byte[] bHeader = header.GetHeader();

            SMB2Header header2 = new SMB2Header();
            header2.SetCommand(new Byte[] { 0x10, 0x00 });
            header2.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header2.SetMessageID(++messageId);
            header2.SetProcessID(processId);
            header2.SetTreeId(treeId);
            header2.SetSessionID(sessionId);
            header2.SetFlags(new Byte[] { 0x00, 0x00, 0x00, 0x04 });

            SMB2GetInfo getInfo2 = new SMB2GetInfo();
            getInfo2.SetClass(new Byte[] { 0x02 });
            getInfo2.SetInfoLevel(new Byte[] { 0x05 });
            getInfo2.SetMaxResponseSize(new Byte[] { 0x50, 0x00, 0x00, 0x00 });
            getInfo2.SetGetInfoInputOffset(new Byte[] { 0x00, 0x00 });
            getInfo2.SetGUIDHandleFile(guidFileHandle);
            Byte[] bData2 = getInfo2.GetRequest();

            if (signing)
            {
                header2.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header2.SetSignature(sessionKey, ref bData2);
            }
            Byte[] bHeader2 = header2.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length + bHeader2.Length);
            sessionService.SetDataLength(bData.Length + bData2.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Combine combine = new Combine();
            combine.Extend(bHeader);
            combine.Extend(bData);
            combine.Extend(bHeader2);
            combine.Extend(bData2);
            Byte[] bSend = Misc.Combine(bSessionService, combine.Retrieve());
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
            {
                return true;
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal Boolean SetInfoRequest(String sourceFilePath, String destination)
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x11, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2SetInfo setInfo = new SMB2SetInfo();
            setInfo.SetClass(new Byte[] { 0x01 });
            setInfo.SetInfoLevel(new Byte[] { 0x14 });
            setInfo.SetGUIDHandleFile(guidFileHandle);

            //This may need to be coverted to int32
            using (FileStream fileStream = new FileStream(Path.GetFullPath(sourceFilePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (BinaryReader binaryReader = new BinaryReader(fileStream))
                {
                    setInfo.SetBuffer(BitConverter.GetBytes(binaryReader.BaseStream.Length));
                }
            }

            setInfo.SetGUIDHandleFile(guidFileHandle);
            Byte[] bData = setInfo.GetRequest();

            header.SetChainOffset(bData.Length);
            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(bSessionService, Misc.Combine(bHeader, bData));
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);

            if (GetStatus(recieve.Skip(12).Take(4).ToArray()))
            {
                treeId = recieve.Skip(40).Take(4).ToArray();
                return true;
            }
            return false;
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        internal void WriteRequest(String sourceFilePath)
        {
            Int64 bytesTransmitted = 0;
            using (var fileStream = new FileStream(Path.GetFullPath(sourceFilePath), FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                using (var binaryReader = new BinaryReader(fileStream))
                {
                    binaryReader.BaseStream.Seek(0, SeekOrigin.Begin);

                    while (bytesTransmitted < binaryReader.BaseStream.Length)
                    {
                        Int32 length = (Int32)(binaryReader.BaseStream.Length >= bytesTransmitted + 65535 ? 65535 : binaryReader.BaseStream.Length - bytesTransmitted);
                        Byte[] fileBytes = new Byte[length];

                        binaryReader.BaseStream.Seek(bytesTransmitted, SeekOrigin.Begin);
                        binaryReader.Read(fileBytes, 0, length);
                        _WriteRequest(fileBytes, bytesTransmitted);
                        bytesTransmitted += length;

                        Console.WriteLine(bytesTransmitted + "/" + binaryReader.BaseStream.Length);
                    }
                    
                }
            }

            
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        private void _WriteRequest(Byte[] buffer, Int64 offset)
        {
            SMB2Header header = new SMB2Header();
            header.SetCommand(new Byte[] { 0x09, 0x00 });
            header.SetCreditsRequested(new Byte[] { 0x01, 0x00 });
            header.SetMessageID(++messageId);
            header.SetProcessID(processId);
            header.SetTreeId(treeId);
            header.SetSessionID(sessionId);

            SMB2WriteRequest writeRequest = new SMB2WriteRequest();
            writeRequest.SetGuidHandleFile(guidFileHandle);
            writeRequest.SetBuffer(buffer);
            writeRequest.SetOffset(offset);

            Byte[] bData = writeRequest.GetRequest();

            header.SetChainOffset(bData.Length);
            if (signing)
            {
                header.SetFlags(new Byte[] { 0x08, 0x00, 0x00, 0x00 });
                header.SetSignature(sessionKey, ref bData);
            }
            Byte[] bHeader = header.GetHeader();

            NetBIOSSessionService sessionService = new NetBIOSSessionService();
            sessionService.SetHeaderLength(bHeader.Length);
            sessionService.SetDataLength(bData.Length);
            Byte[] bSessionService = sessionService.GetNetBIOSSessionService();

            Byte[] bSend = Misc.Combine(bSessionService, Misc.Combine(bHeader, bData));
            streamSocket.Write(bSend, 0, bSend.Length);
            streamSocket.Flush();
            streamSocket.Read(recieve, 0, recieve.Length);
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        public new void Dispose()
        {
            base.Dispose();
        }

        ////////////////////////////////////////////////////////////////////////////////
        //
        ////////////////////////////////////////////////////////////////////////////////
        ~SMBClientPut()
        {
            Dispose();
        }
    }
}
