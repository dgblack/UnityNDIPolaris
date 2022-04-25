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

namespace NDI.CapiSample.Protocol.GBF
{
    /// <summary>
    /// A frame item contains timing and type information wrapping subcomponents with tracking data.
    /// </summary>
    class GbfFrameItem
    {
        /// <summary>
        /// Indicates what type of frame gathered the data. See the related enum to interpret this value
        /// </summary>
        public FrameType type;

        /// <summary>
        /// Each frame is given an sequence number, which clients can usually ignore
        /// </summary>
        public int sequenceIndex;

        /// <summary>
        /// Same as Transform status, but only codes that apply to the frame as a whole
        /// </summary>
        public int status;

        /// <summary>
        /// The frame number that identifies when the data was collected
        /// </summary>
        public int number;

        /// <summary>
        /// The "seconds" part of the timestamp
        /// </summary>
        public int timespec_s;

        /// <summary>
        /// The "nanoseconds" part of the timestamp
        /// </summary>
        public int timespec_ns;

        /// <summary>
        /// This container holds other GbfComponents containing the data
        /// </summary>
        public GbfContainer data;

        /// <summary>
        /// Read the frame item headers and begin parsing the sub-container.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        public GbfFrameItem(Stream reader)
        {
            type = (FrameType)BytePacker.UnpackInt(reader, 1);
            sequenceIndex = BytePacker.UnpackInt(reader, 1);
            status = BytePacker.UnpackInt(reader, 2);
            number = BytePacker.UnpackInt(reader, 4);
            timespec_s = BytePacker.UnpackInt(reader, 4);
            timespec_ns = BytePacker.UnpackInt(reader, 4);
            data = new GbfContainer(reader);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("----GbfFrameDataItem \n");
            sb.AppendFormat("type={0:X2} ({1})\n", type, Enum.GetName(typeof(FrameType), type));
            sb.AppendFormat("sequenceIndex={0:X2}\n", sequenceIndex);
            sb.AppendFormat("status={0:X4}\n", status);
            sb.AppendFormat("number={0:X8}\n", number);
            sb.AppendFormat("timestamp={0:X8},{1:X8}\n", timespec_s, timespec_ns);
            sb.Append(data.ToString());

            return sb.ToString();
        }
    }
}
