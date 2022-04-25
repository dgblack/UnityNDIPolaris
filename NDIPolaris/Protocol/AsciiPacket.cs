//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

// System
using System;
using System.IO;
using System.Text;

// NDI
using NDI.CapiSample.Data;
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// AsciiPacket handles ASCII responses from the position sensor. These can be success/error messages or user parameters.
    /// </summary>
    public class AsciiPacket : Packet
    {
        /// <summary>
        /// ASCII data without the CRC component.
        /// </summary>
        public string Data { get; private set; } = "";

        /// <summary>
        /// The received trailing CRC value.
        /// </summary>
        public uint ReceivedCRC { get; private set; } = 0;

        /// <summary>
        /// The calculated CRC value.
        /// </summary>
        public uint CalculatedCRC { get; private set; } = 0;

        /// <summary>
        /// The error code, if present.
        /// </summary>
        public uint ErrorCode { get; private set; } = 0;

        /// <summary>
        /// Reads ASCII characters from the stream until a carriage return is found.
        /// </summary>
        /// <param name="stream">IO Stream to read from, should support seeking.</param>
        /// <returns></returns>
        public AsciiPacket(Stream stream)
        {
            byte[] read = new byte[1];
            while (true)
            {
                for (int r = 0; r < read.Length && stream.CanRead; r += stream.Read(read, r, read.Length - r)) ;

                Bytes.AddRange(read);

                if(Bytes.Count < 5)
                {
                    // Minimum is CRC + \r
                    continue;
                }
                else if(Bytes[Bytes.Count - 1] == (byte)'\r')
                {
                    // carriage return so we're done
                    break;
                }
            }

            if(Bytes.Count < 5)
            {
                IsValid = false;
                return;
            }

            // Parse content
            byte[] array = Bytes.ToArray();
            byte[] content = new byte[Bytes.Count - 5];
            Array.Copy(array, content, content.Length);

            // Parse Hex CRC String
            byte[] crc = new byte[4];
            Array.Copy(array, content.Length, crc, 0, crc.Length);
            string crcString = Encoding.ASCII.GetString(crc);
            ReceivedCRC = uint.Parse(crcString, System.Globalization.NumberStyles.HexNumber);

            CalculatedCRC = CRC.CalculateCRC16(content);
            if (ReceivedCRC != CalculatedCRC)
            {
                IsValid = false;
                return;
            }

            Data = Encoding.ASCII.GetString(content);

            // Log any errors
            ErrorCode = GetErrorCode(Data);
            IsValid = ErrorCode == 0;
        }

        /// <summary>
        /// Get the error description string for this packet.
        /// </summary>
        /// <returns>Error string.</returns>
        public string GetErrorString()
        {
            return GetErrorString(ErrorCode);
        }

        /// <summary>
        /// Output the data string without the CRC.
        /// </summary>
        /// <returns>Data string</returns>
        public override string ToString()
        {
            return Encoding.ASCII.GetString(Bytes.ToArray());
        }

        /// <summary>
        /// Parse the Error or Warning response for the corresponding code.
        /// </summary>
        /// <param name="asciiResponse">ASCII response from a command.</param>
        /// <returns>
        /// Zero: No Error
        /// Less than 1000: Error Code
        /// 1000 or more: Warning Code
        /// </returns>
        private static uint GetErrorCode(string asciiResponse)
        {
            uint errorCode = 0;
            if (asciiResponse.StartsWith("ERROR"))
            {
                errorCode = uint.Parse(asciiResponse.Substring(5, 2), System.Globalization.NumberStyles.HexNumber);
            }
            else if (asciiResponse.StartsWith("WARNING"))
            {
                errorCode = uint.Parse(asciiResponse.Substring(7, 2), System.Globalization.NumberStyles.HexNumber) + Constants.WARNING_CODE_OFFSET;
            }

            return errorCode;
        }

        /// <summary>
        /// Looks up the human readable error or warning message from their corresponding tables.
        /// </summary>
        /// <param name="code">Error code from GetErrorCode()</param>
        /// <returns>String message</returns>
        public static string GetErrorString(uint code)
        {
            if (code > Constants.WARNING_CODE_OFFSET)
            {
                code -= Constants.WARNING_CODE_OFFSET;
                if (code >= 0 && code < Constants.WARNING_STRINGS.Length)
                {
                    return Constants.WARNING_STRINGS[code];
                }
            }
            else
            {
                if (code >= 0 && code < Constants.ERROR_STRINGS.Length)
                {
                    return Constants.ERROR_STRINGS[code];
                }
            }

            return "";
        }
    }
}
