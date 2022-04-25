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
using System.Collections.Generic;

// NDI
using NDI.CapiSample.Data;
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol.GBF
{
    /// <summary>
    /// Component containing 6D transform information for each tool.
    /// </summary>
    class GbfData6D : GbfComponent
    {
        /// <summary>
        /// List of tracked tools transforms.
        /// </summary>
        public List<Transform> tools = new List<Transform>();

        /// <summary>
        /// Parse all tools from the stream.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        /// <param name="numTools">Number of tools in this component.</param>
        public GbfData6D(Stream reader, int numTools)
        {
            for (int i = 0; i < numTools; i++)
            {
                Transform transform = new Transform();
                transform.toolHandle = BytePacker.UnpackInt(reader, 2);
                transform.rawStatus = BytePacker.UnpackInt(reader, 2);

                // Parse the raw status into its subcomponents.
                transform.isMissing = (transform.rawStatus & Transform.STATUS_MASK_MISSING) != 0;
                transform.status = (TransformStatus)(transform.rawStatus & Transform.STATUS_MASK_CODE);
                transform.faceNumber = (transform.rawStatus & Transform.STATUS_MASK_FACE) >> 12;

                // Transform P&O data will only be present if not missing.
                if (!transform.isMissing)
                {
                    transform.orientation.q0 = BytePacker.UnpackFloat(reader);
                    transform.orientation.qx = BytePacker.UnpackFloat(reader);
                    transform.orientation.qy = BytePacker.UnpackFloat(reader);
                    transform.orientation.qz = BytePacker.UnpackFloat(reader);

                    transform.position.x = BytePacker.UnpackFloat(reader);
                    transform.position.y = BytePacker.UnpackFloat(reader);
                    transform.position.z = BytePacker.UnpackFloat(reader);

                    transform.error = BytePacker.UnpackFloat(reader);
                }

                tools.Add(transform);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            // Component Info
            sb.AppendFormat("----GbfData6D \n {0}", base.ToString());

            foreach (var tool in tools)
            {
                // Tool Info
                sb.AppendFormat("toolHandle={0:X4}\n", tool.toolHandle);
                sb.AppendFormat("status={0:X4}", tool.status);
                if(tool.isMissing)
                {
                    sb.Append(" - MISSING");
                }
                sb.AppendFormat(", code={0:X2} ({1})\n", tool.status, Enum.GetName(typeof(TransformStatus), tool.status));
                sb.AppendFormat("[x,y,z] = [{0}]\n[q0,qx,qy,qz] = [{1}]\n", tool.position, tool.orientation);
                sb.AppendFormat("fit error={0,8:F3}\n", tool.error);
            }

            return sb.ToString();
        }
    }
}
