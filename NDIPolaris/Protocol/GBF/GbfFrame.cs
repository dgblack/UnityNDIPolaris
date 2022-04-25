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
using System.Linq;
using System.Text;
using System.Collections.Generic;

// NDI
using NDI.CapiSample.Data;

namespace NDI.CapiSample.Protocol.GBF
{
    /// <summary>
    /// Frame contains all tracked tool data.
    /// </summary>
    public class GbfFrame : GbfComponent
    {
        /// <summary>
        /// List of frame subitems.
        /// </summary>
        List<GbfFrameItem> data = new List<GbfFrameItem>();

        /// <summary>
        /// Parse the frame from the stream. The Frame does not contain any header, only FrameItem subcomponents.
        /// </summary>
        /// <param name="reader">BufferedReader to read from.</param>
        /// <param name="itemCount">Number of FrameItems contained in the component.</param>
        public GbfFrame(Stream reader, int itemCount)
        {
            for(int i = 0; i < itemCount; i++)
            {
                data.Add(new GbfFrameItem(reader));
            }
        }

        /// <summary>
        /// Build a list of tools that were tracked in this frame.
        /// </summary>
        /// <returns>List of tools.</returns>
        public List<Tool> GetToolList()
        {
            // The end goal is to flatten the data into the Tool structures for client-side manipulation.
            Dictionary<int, Tool> tools = new Dictionary<int, Tool>();

            // System alerts are transmitted with each GbfFrameDataItem
            List<SystemAlert> alerts = new List<SystemAlert>();

            // Active, Active-Wireless, and Passive tools collect data in different ways.
            // Thus, each frame of data must be divided into separate frames for each tool type.
            // BX2 transmits the data as one or more GbfFrameDataItems on each GbfFrame.
            // Stray markers will be returned as 3D markers of a Tool with the handle 0xFFFF.
            foreach (var frame in data)
            {
                foreach(var component in frame.data.components)
                {
                    if(component.type == ComponentType.Data6D)
                    {
                        GbfData6D data = (GbfData6D)component;
                        foreach(var transform in data.tools)
                        {
                            // Ensure generic tool frame data exists
                            if (!tools.ContainsKey(transform.toolHandle))
                            {
                                tools.Add(transform.toolHandle, BuildTool(frame, transform.toolHandle));
                            }

                            // Update 6D Transform
                            tools[transform.toolHandle].transform = transform;
                        }
                    }
                    else if(component.type == ComponentType.Data3D)
                    {
                        GbfData3D data = (GbfData3D)component;
                        foreach(var tool in data.markers)
                        {
                            // Ensure generic tool frame data exists
                            if (!tools.ContainsKey(tool.Key))
                            {
                                tools.Add(tool.Key, BuildTool(frame, tool.Key));
                            }

                            // Update 3D Marker positions for the tool
                            tools[tool.Key].markers = tool.Value;
                        }
                    }
                    else if(component.type == ComponentType.SystemAlert)
                    {
                        GbfSystemAlert data = (GbfSystemAlert)component;
                        alerts = data.alerts;
                    }
                    else
                    {
                        // TODO: Handle other component types
                    }
                } // foreach component
            } // foreach frameitem

            // Copy alerts to each tool when we're done processing
            foreach(var tool in tools.Values)
            {
                tool.systemAlerts = alerts;
            }

            // The tool handle is stored in the tool itself, so just return the list of tools.
            return tools.Values.ToList();
        }

        /// <summary>
        /// Build a tool using the generic frame information before adding data from each component type.
        /// </summary>
        /// <param name="data">Frame data</param>
        /// <param name="handle">Tool Handle</param>
        /// <returns>A generic tool with no tracking data</returns>
        private Tool BuildTool(GbfFrameItem data, int handle)
        {
            Tool tool = new Tool();
            tool.dataIsNew = true;
            tool.transform.toolHandle = handle;
            tool.frameType = data.type;
            tool.frameSequenceIndex = data.sequenceIndex;
            tool.frameStatus = data.status;
            tool.frameNumber = data.number;
            tool.timespec_s = data.timespec_s;
            tool.timespec_ns = data.timespec_ns;
            return tool;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("----GbfFrame\n {0}", base.ToString());

            foreach(var item in data)
            {
                sb.Append(item.ToString());
            }

            return sb.ToString();
        }
    }
}
