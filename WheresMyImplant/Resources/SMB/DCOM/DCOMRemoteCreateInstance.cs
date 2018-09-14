using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WheresMyImplant
{
    class DCOMRemoteCreateInstance
    {
        private readonly Byte[] DCOMVersionMajor = { 0x05, 0x00 };
        private readonly Byte[] DCOMVersionMinor = { 0x07, 0x00 };
        private readonly Byte[] DCOMFlags = { 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] DCOMReserved = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] DCOMCausalityID;
        private readonly Byte[] Unknown = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Unknown2 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] Unknown3 = { 0x00, 0x00, 0x02, 0x00 };
        private Byte[] Unknown4;
        private Byte[] IActPropertiesCntData;
        private readonly Byte[] IActPropertiesOBJREFSignature = { 0x4d, 0x45, 0x4f, 0x57 };
        private readonly Byte[] IActPropertiesOBJREFFlags = { 0x04, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesOBJREFIID = { 0xa2, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFCLSID = { 0x38, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFCBExtension = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] IActPropertiesCUSTOMOBJREFSize;
        private Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesTotalSize;
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesReserved = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderCommonHeader = { 0x01, 0x10, 0x08, 0x00, 0xcc, 0xcc, 0xcc, 0xcc };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderPrivateHeader = { 0xb0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderTotalSize;
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderCustomHeaderSize = { 0xc0, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderReserved = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesDestinationContext = { 0x02, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesNumActivationPropertyStructs = { 0x06, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsInfoClsid = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrReferentID = { 0x00, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrReferentID = { 0x04, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesNULLPointer = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrMaxCount = { 0x06, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid = { 0xb9, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid2 = { 0xab, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid3 = { 0xa5, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid4 = { 0xa6, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid5 = { 0xa4, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid6 = { 0xaa, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrMaxCount = { 0x06, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize = { 0x68, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize2 = { 0x58, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize3 = { 0x90, 0x00, 0x00, 0x00 };
        private Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize4;
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize5 = { 0x20, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize6 = { 0x30, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesCommonHeader = { 0x01, 0x10, 0x08, 0x00, 0xcc, 0xcc, 0xcc, 0xcc };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesPrivateHeader = { 0x58, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesSessionID = { 0xff, 0xff, 0xff, 0xff };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesRemoteThisSessionID = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesClientImpersonating = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesPartitionIDPresent = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesDefaultAuthnLevel = { 0x02, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesPartitionGuid = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesProcessRequestFlags = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesOriginalClassContext = { 0x14, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesFlags = { 0x02, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesReserved = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesUnusedBuffer = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoCommonHeader = { 0x01, 0x10, 0x08, 0x00, 0xcc, 0xcc, 0xcc, 0xcc };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoPrivateHeader = { 0x48, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoInstantiatedObjectClsId = { 0x5e, 0xf0, 0xc3, 0x8b, 0x6b, 0xd8, 0xd0, 0x11, 0xa0, 0x75, 0x00, 0xc0, 0x4f, 0xb6, 0x88, 0x20 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoClassContext = { 0x14, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoActivationFlags = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoFlagsSurrogate = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoInterfaceIdCount = { 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoInstantiationFlag = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIdsPtr = { 0x00, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationEntirePropertySize = { 0x58, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationVersionMajor = { 0x05, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationVersionMinor = { 0x07, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIdsPtrMaxCount = { 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIds = { 0x18, 0xad, 0x09, 0xf3, 0x6a, 0xd8, 0xd0, 0x11, 0xa0, 0x75, 0x00, 0xc0, 0x4f, 0xb6, 0x88, 0x20 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIdsUnusedBuffer = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoCommonHeader = { 0x01, 0x10, 0x08, 0x00, 0xcc, 0xcc, 0xcc, 0xcc };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoPrivateHeader = { 0x80, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientOk = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoReserved = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoReserved2 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoReserved3 = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrReferentID = { 0x00, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoNULLPtr = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextUnknown = { 0x60, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextCntData = { 0x60, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFSignature = { 0x4d, 0x45, 0x4f, 0x57 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFFlags = { 0x04, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFIID = { 0xc0, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFCUSTOMOBJREFCLSID = { 0x3b, 0x03, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xc0, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x46 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFCUSTOMOBJREFCBExtension = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFCUSTOMOBJREFSize = { 0x30, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoUnusedBuffer = { 0x01, 0x00, 0x01, 0x00, 0x63, 0x2c, 0x80, 0x2a, 0xa5, 0xd2, 0xaf, 0xdd, 0x4d, 0xc4, 0xbb, 0x37, 0x4d, 0x37, 0x76, 0xd7, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoCommonHeader = { 0x01, 0x10, 0x08, 0x00, 0xcc, 0xcc, 0xcc, 0xcc };
        private Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoPrivateHeader;
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoAuthenticationFlags = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoPtrReferentID = { 0x00, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoNULLPtr = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoReserved = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameReferentID = { 0x04, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNULLPtr = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoReserved2 = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameMaxCount;
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameOffset = { 0x00, 0x00, 0x00, 0x00 };
        private Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameActualCount;
        private Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameString;
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoCommonHeader = { 0x01, 0x10, 0x08, 0x00, 0xcc, 0xcc, 0xcc, 0xcc };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoPrivateHeader = { 0x10, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoNULLPtr = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoProcessID = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoApartmentID = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoContextID = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoCommonHeader = { 0x01, 0x10, 0x08, 0x00, 0xcc, 0xcc, 0xcc, 0xcc };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoPrivateHeader = { 0x20, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoNULLPtr = { 0x00, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrReferentID = { 0x00, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestClientImpersonationLevel = { 0x02, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestNumProtocolSequences = { 0x01, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestUnknown = { 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestProtocolSeqsArrayPtrReferentID = { 0x04, 0x00, 0x02, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestProtocolSeqsArrayPtrMaxCount = { 0x01, 0x00, 0x00, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestProtocolSeqsArrayPtrProtocolSeq = { 0x07, 0x00 };
        private readonly Byte[] IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoUnusedBuffer = { 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };

        internal DCOMRemoteCreateInstance()
        {

        }

        internal void SetDCOMCausalityID(Byte[] DCOMCausalityID)
        {
            this.DCOMCausalityID = DCOMCausalityID;
        }

        internal void SetServerInfoName(String SetServerInfoName)
        {
            Byte[] targetUnicode = Encoding.Unicode.GetBytes(SetServerInfoName);
            IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameString = Misc.Combine(targetUnicode, new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameActualCount = BitConverter.GetBytes(SetServerInfoName.Length + 1);
            IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameMaxCount = BitConverter.GetBytes(SetServerInfoName.Length + 1);

            Combine combine = new Combine();
            for (Int32 i = 0; i < (Math.Truncate((Decimal)(targetUnicode.Length / 8 + 1) * 8) - targetUnicode.Length); i++)
            {
                combine.Extend(new Byte[] { 0x00 });
            }
            targetUnicode = Misc.Combine(targetUnicode, combine.Retrieve());

            Unknown4 = IActPropertiesCntData = BitConverter.GetBytes(targetUnicode.Length + 720);
            IActPropertiesCUSTOMOBJREFSize = BitConverter.GetBytes(targetUnicode.Length + 680);
            IActPropertiesCUSTOMOBJREFIActPropertiesTotalSize = IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderTotalSize = BitConverter.GetBytes(targetUnicode.Length + 664);
            IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoPrivateHeader = Misc.Combine(BitConverter.GetBytes(targetUnicode.Length + 40), new Byte[] { 0x00, 0x00, 0x00, 0x00 });
            IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize4 = BitConverter.GetBytes(targetUnicode.Length + 56);
        }

        internal Byte[] GetRequest()
        {
            Combine combine = new Combine();
            combine.Extend(DCOMVersionMajor);
            combine.Extend(DCOMVersionMinor);
            combine.Extend(DCOMFlags);
            combine.Extend(DCOMReserved);
            combine.Extend(DCOMCausalityID);
            combine.Extend(Unknown);
            combine.Extend(Unknown2);
            combine.Extend(Unknown3);
            combine.Extend(Unknown4);
            combine.Extend(IActPropertiesCntData);
            combine.Extend(IActPropertiesOBJREFSignature);
            combine.Extend(IActPropertiesOBJREFFlags);
            combine.Extend(IActPropertiesOBJREFIID);
            combine.Extend(IActPropertiesCUSTOMOBJREFCLSID);
            combine.Extend(IActPropertiesCUSTOMOBJREFCBExtension);
            combine.Extend(IActPropertiesCUSTOMOBJREFSize);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesTotalSize);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesReserved);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderCommonHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderPrivateHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderTotalSize);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderCustomHeaderSize);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesCustomHeaderReserved);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesDestinationContext);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesNumActivationPropertyStructs);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsInfoClsid);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrReferentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrReferentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesNULLPointer);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrMaxCount);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid2);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid3);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid4);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid5);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsIdPtrPropertyStructGuid6);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrMaxCount);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize2);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize3);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize4);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize5);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesClsSizesPtrPropertyDataSize6);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesCommonHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesPrivateHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesSessionID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesRemoteThisSessionID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesClientImpersonating);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesPartitionIDPresent);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesDefaultAuthnLevel);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesPartitionGuid);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesProcessRequestFlags);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesOriginalClassContext);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesFlags);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesReserved);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSpecialSystemPropertiesUnusedBuffer);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoCommonHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoPrivateHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoInstantiatedObjectClsId);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoClassContext);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoActivationFlags);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoFlagsSurrogate);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoInterfaceIdCount);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInfoInstantiationFlag);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIdsPtr);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationEntirePropertySize);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationVersionMajor);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationVersionMinor);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIdsPtrMaxCount);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIds);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesInstantiationInterfaceIdsUnusedBuffer);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoCommonHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoPrivateHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientOk);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoReserved);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoReserved2);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoReserved3);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrReferentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoNULLPtr);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextUnknown);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextCntData);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFSignature);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFFlags);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFIID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFCUSTOMOBJREFCLSID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFCUSTOMOBJREFCBExtension);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoClientPtrClientContextOBJREFCUSTOMOBJREFSize);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesActivationContextInfoUnusedBuffer);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoCommonHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoPrivateHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoAuthenticationFlags);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoPtrReferentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoNULLPtr);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoReserved);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameReferentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNULLPtr);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoReserved2);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameMaxCount);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameOffset);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameActualCount);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesSecurityInfoServerInfoServerInfoNameString);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoCommonHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoPrivateHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoNULLPtr);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoProcessID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoApartmentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesLocationInfoContextID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoCommonHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoPrivateHeader);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoNULLPtr);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrReferentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestClientImpersonationLevel);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestNumProtocolSequences);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestUnknown);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestProtocolSeqsArrayPtrReferentID);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestProtocolSeqsArrayPtrMaxCount);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoRemoteRequestPtrRemoteRequestProtocolSeqsArrayPtrProtocolSeq);
            combine.Extend(IActPropertiesCUSTOMOBJREFIActPropertiesPropertiesScmRequestInfoUnusedBuffer);
            return combine.Retrieve();
        }   
    }
}
