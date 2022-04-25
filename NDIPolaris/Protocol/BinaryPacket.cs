//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

// System
using System.IO;

// NDI
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// BinaryPackets can wrap a number of responses including BX or BX2 responses.
    /// </summary>
    public class BinaryPacket : Packet
    {
        /// <summary>
        /// Magic number start sequence.
        /// </summary>
        public PacketStartSequence StartSequence { get; private set; } = 0;

        /// <summary>
        /// Payload size
        /// </summary>
        public int ReplyLength { get; private set; } = 0;

        /// <summary>
        /// Transmitted header CRC value
        /// </summary>
        public int HeaderCRC { get; private set; } = 0;

        /// <summary>
        /// True if the calculated CRC value matches the transmitted value for the header.
        /// </summary>
        public bool HeaderOkay { get; private set; } = false;

        /// <summary>
        /// Payload Data
        /// </summary>
        public byte[] Payload { get; private set; } = null;

        /// <summary>
        /// Transmitted data CRC value
        /// </summary>
        public int PayloadCRC { get; private set; } = 0;

        /// <summary>
        /// True if the calculated CRC value matches the transmitted value for the payload.
        /// </summary>
        public bool PayloadOkay { get; private set; } = false;

        /// <summary>
        /// Get a buffered stream wrapper for the payload.
        /// </summary>
        /// <returns>A buffered stream wrapper for the payload.</returns>
        public Stream GetPayloadStream()
        {
            return new MemoryStream(Payload);
        }

        /// <summary>
        /// Parse the binary header values and CRC checks.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        public BinaryPacket(Stream reader)
        {
            // Need to read the header into an array for use with CRC check before reading actual values.
            byte[] header = new byte[4];
            for (int r = 0; r < header.Length && reader.CanRead; r += reader.Read(header, r, header.Length - r)) ;
            Bytes.AddRange(header);
            Stream headerStream = new MemoryStream(header);
            StartSequence = (PacketStartSequence)BytePacker.UnpackInt(headerStream, 2);
            ReplyLength = BytePacker.UnpackInt(headerStream, 2);

            // Check header CRC
            byte[] crc = new byte[2];
            for (int r = 0; r < crc.Length && reader.CanRead; r += reader.Read(crc, r, crc.Length - r)) ;
            Bytes.AddRange(crc);
            reader.Seek(-2, SeekOrigin.Current);

            HeaderCRC = BytePacker.UnpackInt(reader, 2);
            uint calculatedCRC = CRC.CalculateCRC16(header);
            HeaderOkay = calculatedCRC == HeaderCRC;
            if(!HeaderOkay)
            {
                IsValid = false;
                return;
            }

            // Read payload
            Payload = new byte[ReplyLength];
            for (int r = 0; r < Payload.Length && reader.CanRead; r += reader.Read(Payload, r, Payload.Length - r)) ;
            Bytes.AddRange(Payload);

            // Only the short binary reply has a payload CRC
            if (StartSequence == PacketStartSequence.ShortReply)
            {
                // Check payload CRC
                crc = new byte[2];
                for (int r = 0; r < crc.Length && reader.CanRead; r += reader.Read(crc, r, crc.Length - r)) ;
                Bytes.AddRange(crc);
                reader.Seek(-2, SeekOrigin.Current);

                PayloadCRC = BytePacker.UnpackInt(reader, 2);
                calculatedCRC = CRC.CalculateCRC16(Payload);
                PayloadOkay = calculatedCRC == PayloadCRC;
            }
            else
            {
                // No payload CRC for long reply length
                PayloadOkay = true;
            }

            IsValid = HeaderOkay && PayloadOkay;
        }
    }
}
