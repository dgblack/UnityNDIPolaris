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
using System.Text;
using System.Collections.Generic;

// NDI
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol.GBF
{
    /// <summary>
    /// This is the root container of the General Binary Format packet payload.
    /// </summary>
    class GbfContainer
    {
        /// <summary>
        /// General Binary Format Version
        /// </summary>
        public int gbfVersion;

        /// <summary>
        /// Number of components specified in the response.
        /// </summary>
        public int componentCount;

        /// <summary>
        /// List of parsed components.
        /// </summary>
        public List<GbfComponent> components = new List<GbfComponent>();

        /// <summary>
        /// Parse a container and it's contents from the stream.
        /// </summary>
        /// <param name="reader">Stream to read the binary data from.</param>
        public GbfContainer(Stream reader)
        {
            // Read the GBF header info so we know how to parse the data
            gbfVersion = BytePacker.UnpackInt(reader, 2);
            componentCount = BytePacker.UnpackInt(reader, 2);

            // Read and build each component in the container.
            for(int i = 0; i < componentCount; i++)
            {
                components.Add(GbfComponent.buildComponent(reader));
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("----GbfContainer \n");
            sb.AppendFormat("gbfVersion={0:X4}\n", gbfVersion);
            sb.AppendFormat("componentCount={0:X4}\n", componentCount);

            foreach(GbfComponent component in components)
            {
                sb.Append(component.ToString());
            }

            return sb.ToString();
        }
    }
}
