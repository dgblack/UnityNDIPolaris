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
    /// Component containing tool 3D marker data.
    /// </summary>
    class GbfData3D : GbfComponent
    {
        /// <summary>
        /// List of markers for each tool.
        /// 
        /// Dictionary Key: Tool Handle
        /// </summary>
        public Dictionary<int, List<Marker>> markers = new Dictionary<int, List<Marker>>();

        /// <summary>
        /// Parse the Marker component from the stream.
        /// </summary>
        /// <param name="reader">Stream to read from.</param>
        /// <param name="numTools">Number of tools containing markers in this component.</param>
        public GbfData3D(Stream reader, int numTools)
        {
            // Markers are grouped by tools
            for(int i = 0; i < numTools; i++)
            {
                // Parse the tool information
                int toolHandle = BytePacker.UnpackInt(reader, 2);
                int toolMarkerCount = BytePacker.UnpackInt(reader, 2);

                // Tools contain markers
                markers[toolHandle] = new List<Marker>();
                for(int j = 0; j < toolMarkerCount; j++)
                {
                    Marker marker = new Marker();

                    marker.status = (MarkerStatus)BytePacker.UnpackInt(reader, 1);
                    BytePacker.UnpackInt(reader, 1); // reserved
                    marker.index = BytePacker.UnpackInt(reader, 2);

                    // Position data will only be present if the marker is not missing.
                    if(marker.status != MarkerStatus.Missing)
                    {
                        marker.position.x = BytePacker.UnpackFloat(reader);
                        marker.position.y = BytePacker.UnpackFloat(reader);
                        marker.position.z = BytePacker.UnpackFloat(reader);
                    }

                    markers[toolHandle].Add(marker);
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            // Component Info
            sb.AppendFormat("----GbfData3D \n {0}", base.ToString());

            foreach(var tool in markers)
            {
                // Tool Info
                sb.AppendFormat("toolHandleReference={0:X4}\n", tool.Key);
                sb.AppendFormat("numberOf3Ds={0:X4}\n", tool.Value.Count);

                for(int i = 0; i < tool.Value.Count; i++)
                {
                    // Marker Info
                    sb.AppendFormat("--Data3D: status={0:X2} ({1}), ", tool.Value[i].status, Enum.GetName(typeof(MarkerStatus), tool.Value[i].status));
                    sb.AppendFormat("markerIndex={0:X4}, ", tool.Value[i].index);
                    sb.AppendFormat("[x,y,z] = [{0}]\n ", tool.Value[i].position.ToString());
                }
            }

            return sb.ToString();
        }
    }
}
