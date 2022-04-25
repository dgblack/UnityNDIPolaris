//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

// System
using System.Collections.Generic;

// NDI
using NDI.CapiSample.Data;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// This class houses BX2 response parsing methods.
    /// </summary>
    public class Bx2Reply
    {
        /// <summary>
        /// Parse a BX2 reply from the BinaryPacket.
        /// </summary>
        /// <param name="packet">BinaryPacket response from the position sensor.</param>
        /// <returns>A list of tools that were updated.</returns>
        public static List<Tool> Parse(BinaryPacket packet)
        {
            // Make sure the packet was valid and the correct type
            if (!packet.IsValid || packet.StartSequence != PacketStartSequence.ShortReply)
            {
                return new List<Tool>();
            }

            // The payload should contain a GBF Container with a single Frame component
            GBF.GbfContainer container = new GBF.GbfContainer(packet.GetPayloadStream());
            foreach (var component in container.components)
            {
                if (component.type == GBF.ComponentType.Frame)
                {
                    GBF.GbfFrame frame = (GBF.GbfFrame)component;
                    return frame.GetToolList();
                }
            }

            // Return an empty list if we were unable to find the correct components
            return new List<Tool>();
        }
    }
}
