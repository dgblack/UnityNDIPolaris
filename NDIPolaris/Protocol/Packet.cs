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
using System.Collections.Generic;

// NDI
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// Generic response packet.
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Packet contained all expected data and passes CRC checks.
        /// </summary>
        public bool IsValid { get; protected set; } = false;

        /// <summary>
        /// Raw bytes contained in the packet.
        /// </summary>
        public List<byte> Bytes { get; protected set; } = new List<byte>();

        /// <summary>
        /// Build the correct type of packet.
        /// </summary>
        /// <param name="stream">IO Stream that can seek.</param>
        /// <returns>A packet</returns>
        public static Packet BuildPacket(Stream stream)
        {
            // Read enough for the binary start sequence, even if this is an ascii response. 
            // The minimum ascii response length is 5 bytes
            // The binary start sequence length is 2 bytes
            PacketStartSequence sequence = (PacketStartSequence)BytePacker.UnpackInt(stream, 2);
            
            // Go back so that the actual packet parsing can use the data
            stream.Seek(-2, SeekOrigin.Current);

            switch(sequence)
            {
                case PacketStartSequence.ShortReply:
                    return new BinaryPacket(stream);
                case PacketStartSequence.StreamReply:
                    return new StreamPacket(stream);
                case PacketStartSequence.LongReply:
                    throw new NotImplementedException();
                default: // ASCII Reply
                    return new AsciiPacket(stream);
            }
        }

        /// <summary>
        /// Output the raw bytes to a string.
        /// </summary>
        /// <returns>String representation of byte array</returns>
        public override string ToString()
        {
            return BitConverter.ToString(Bytes.ToArray());
        }
    }

    public enum PacketStartSequence
    {
        ShortReply = 0xA5C4,
        LongReply = 0xA5C8,
        StreamReply = 0xB5D4
    }
}
