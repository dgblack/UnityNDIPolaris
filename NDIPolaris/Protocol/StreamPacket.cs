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

// NDI
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// The StreamPacket is a response to the StreamCommand and wraps another Packet containing the actual response data.
    /// </summary>
    class StreamPacket : Packet
    {
        /// <summary>
        /// Magic number start sequence.
        /// </summary>
        public PacketStartSequence StartSequence { get; private set; } = 0;

        /// <summary>
        /// Unique Stream ID for the stream command this is responding to.
        /// </summary>
        public string StreamId { get; private set; } = "";

        /// <summary>
        /// Received CRC for the packet header.
        /// </summary>
        public int HeaderCrc { get; private set; } = 0;

        /// <summary>
        /// Header CRC check state.
        /// </summary>
        public bool HeaderOkay { get; private set; } = false;

        /// <summary>
        /// The Stream packet contains another packet as-is after the header, the same as if it were a command response.
        /// </summary>
        public Packet Contents { get; private set; } = null;

        /// <summary>
        /// Parse a new stream packet from the IO Stream.
        /// </summary>
        /// <param name="stream">IO Stream that can seek.</param>
        public StreamPacket(Stream stream)
        {
            // Read start sequence
            StartSequence = (PacketStartSequence)BytePacker.UnpackInt(stream, 2);

            // Read id length
            int streamIdLength = BytePacker.UnpackInt(stream, 2);

            // Go back and now read the full header for CRC calc
            stream.Seek(-4, SeekOrigin.Current);
            byte[] header = new byte[4 + streamIdLength];
            for (int r = 0; r < header.Length && stream.CanRead; r += stream.Read(header, r, header.Length - r)) ;
            Bytes.AddRange(header);

            // Go back and read just the string ID
            stream.Seek(-streamIdLength, SeekOrigin.Current);
            StreamId = BytePacker.UnpackString(stream, streamIdLength);

            // Read the CRC
            byte[] crc = new byte[2];
            for (int r = 0; r < crc.Length && stream.CanRead; r += stream.Read(crc, r, crc.Length - r)) ;
            Bytes.AddRange(crc);
            stream.Seek(-2, SeekOrigin.Current);
            HeaderCrc = BytePacker.UnpackInt(stream, 2);

            // Calculate the CRC
            uint calculatedCRC = CRC.CalculateCRC16(header);
            HeaderOkay = calculatedCRC == HeaderCrc;
            if (!HeaderOkay)
            {
                IsValid = false;
                return;
            }

            // Parse the streamed content.
            Contents = BuildPacket(stream);
            IsValid = Contents.IsValid;
        }

        /// <summary>
        /// Output the raw bytes to a string for stream header and contents.
        /// </summary>
        /// <returns>String representation of byte array</returns>
        public override string ToString()
        {
            return BitConverter.ToString(Bytes.ToArray()) + Contents.ToString();
        }
    }
}
