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
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// Reply contents from the BX command
    /// </summary>
    public class BxReply
    {
        /// <summary>
        /// Parse a BX reply from the BinaryPacket. 
        /// Unlike the BX2 command, we need to know the replyOptions used in the original command sent to the position sensor.
        /// </summary>
        /// <param name="packet">BinaryPacket response from the position sensor.</param>
        /// <param name="replyOption">Reply options sent to the position sensor.</param>
        /// <returns>A list of tools updated this frame.</returns>
        public static List<Tool> Parse(BinaryPacket packet, int replyOption)
        {
            List<Tool> components = new List<Tool>();

            // Make sure the packet was valid and the correct type
            if (!packet.IsValid || packet.StartSequence != PacketStartSequence.ShortReply )
            {
                return components;
            }

            // Get a memory stream to access the payload contents
            var reader = packet.GetPayloadStream();

            // Get the number of components present in this packet
            int componentCount = BytePacker.UnpackInt(reader, 1);

            // Read each component
            for (int i = 0; i < componentCount; i++)
            {
                Tool component = new Tool();

                // Read tool header info
                component.transform.toolHandle = BytePacker.UnpackInt(reader, 1);
                component.handleStatus = (HandleStatus)BytePacker.UnpackInt(reader, 1);

                // Pose data only exists if the status is valid
                if (component.handleStatus == HandleStatus.Valid)
                {
                    if ((replyOption & (int)BxReplyOptionFlags.TransformData) != 0)
                    {
                        component.transform.orientation.q0 = BytePacker.UnpackFloat(reader);
                        component.transform.orientation.qx = BytePacker.UnpackFloat(reader);
                        component.transform.orientation.qy = BytePacker.UnpackFloat(reader);
                        component.transform.orientation.qz = BytePacker.UnpackFloat(reader);
                        component.transform.position.x = BytePacker.UnpackFloat(reader);
                        component.transform.position.y = BytePacker.UnpackFloat(reader);
                        component.transform.position.z = BytePacker.UnpackFloat(reader);
                        component.transform.error = BytePacker.UnpackFloat(reader);
                        component.portStatus = BytePacker.UnpackInt(reader, 4) & 0x0000FFFF;
                        component.frameNumber = BytePacker.UnpackInt(reader, 4);
                    }

                    // TODO: Parse other data options
                }

                components.Add(component);
            }

            // Read system status footer
            int status = BytePacker.UnpackInt(reader, 2);
            foreach(var tool in components)
            {
                tool.systemStatus = status;
            }

            return components;
        }
    }

    public enum BxReplyOptionFlags
    {
        TransformData = 0x0001,
        ToolAndMarkerInfo = 0x0002,
        StrayActiveMarkerData = 0x0004,
        ToolMarkerData = 0x0008,
        AllTransforms = 0x0800,
        StrayPassiveMarkerData = 0x1000
    }
}
