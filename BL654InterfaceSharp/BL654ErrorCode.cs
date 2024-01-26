using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BL654Interface
{
    public enum BL65ErrorCode
    {
        OK = 0,
        InvalidSRegNumber = 01,
        ValueOutOfRange = 02,
        SyntaxError = 05,
        InvalidAddress = 09,
        CommandCannotBeProcessedInCurrentState = 14,
        UnknownCommand = 15,
        ValueSuppliedNotValid = 33,
        GPIONotAvailable = 46,
        TooFewParameters = 47,
        TooManyParameters = 48,
        HexStringNotValid = 49,
        SaveFail = 50,
        RestoreFail = 51,
        VSPOpenFail = 52,
        InvalidAdvertType = 53,
        InvalidUUID = 54,
        ServiceNotEnded = 55,
        CharacteristicNotEnded = 56,
        ServiceNotStarted = 57,
        TooManyCharacteristics = 58,
        CharacteristicNotStarted = 59,
        NFCNotOpen = 60,
        NFCNDEFMessageEmpty = 61,
        DirectedAdvertButPeerAddressMissing = 62,
        InvalidChannelMask = 63,
        InvalidAdvertReports = 64,
        InvalidAdvertReportData = 65,
        InvalidAdvertReportDataSize = 66,
        InvalidOutOfBandData = 67,
        NewlineCharacterNotFound = 68,
        FunctionalityNotCoded = 99,

        //Specific to characteristic read/write
        InvalidErrorCode = 256,
        InvalidAttributeHandle = 257,
        ReadNotPermitted = 258,
        WriteNotPermitted = 259,
        UsedInATTasInvalidPDU = 260,
        AuthenticatedLinkRequired = 261,
        UsedInATTasRequestNotSupported = 262,
        InvalidOffset = 263,
        UsedInATTasInsufficientAuthorisation = 264,
        UsedInATTasPrepareQueueFull = 265,
        ATTAttributeNotFound = 266,
        CannotReadWriteWithBlobRequests = 267,
        EncryptionKeyTooSmall = 268,
        InvalidValueSize = 269,
        VeryUnlikelyError = 270,
        EncryptedLinkRequired1 = 271,
        AttributeTypeNotSupportedGroupingAttribute = 272,
        EncryptedLinkRequired2 = 273,

        NORDICERROR_INVALID_PARM                 = 0x6207,
        NORDICERROR_INVALID_DATA                 = 0x620B,
        NORDICERROR_INVALID_DATA_SIZE            = 0x620C,

        
        ERROR_BLE_GATTC_NO_MORE_DATA             = 0x6052,
        ERROR_BLE_DATA_SIZE                    =   0x620C,
        ERROR_BLE_RESOURCES                    =   0x6213,
        ERROR_BLE_NO_TX_BUFFERS                =   0x6804,


        /// <summary>
        /// Not returned by the device, this happens when the library doesn't know how to parse an error response.
        /// </summary>
        UNKNOWN_ERROR = -1,

        /// <summary>
        /// No known response returned from the AT interface
        /// </summary>
        NO_KNOWN_RESPONSE = -2,

        CONNECTION_FAILED = -3,
    }

}
