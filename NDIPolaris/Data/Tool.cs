//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

using System.Collections.Generic;

namespace NDI.CapiSample.Data
{
    /// <summary>
    /// This class stores tracking information related to a single tool.
    ///
    /// Depending on how the data was gathered, some information may not be available.
    /// For example, BX returns no button information.
    /// </summary>
    public class Tool
    {
        #region Common

        /// <summary>
        /// The frame number that identifies when the data was collected
        /// </summary>
        public int frameNumber;

        /// <summary>
        /// The transform containing tracking information about the tool
        /// </summary>
        public Transform transform = new Transform();

        #endregion

        #region Used with TX and BX

        /// <summary>
        /// The status of the measurement device itself
        /// </summary>
        public int systemStatus;

        /// <summary>
        /// The status of the tool
        /// </summary>
        public int portStatus;

        /// <summary>
        /// The status of the port handle
        /// </summary>
        public HandleStatus handleStatus;

        #endregion

        #region Used with BX2

        /// <summary>
        /// Indicates what type of frame gathered the data. See the related enum to interpret this value
        /// </summary>
        public FrameType frameType;

        /// <summary>
        /// Each frame is given an sequence number, which clients can usually ignore
        /// </summary>
        public int frameSequenceIndex;

        /// <summary>
        /// Same as TransformStatus, but only codes that apply to the frame as a whole
        /// </summary>
        public int frameStatus;

        /// <summary>
        /// The "seconds" part of the timestamp
        /// </summary>
        public int timespec_s;

        /// <summary>
        /// The "nanoseconds" part of the timestamp
        /// </summary>
        public int timespec_ns;

        /// <summary>
        /// A list of marker 3Ds, if this was requested in the BX2 options
        /// </summary>
        public List<Marker> markers = new List<Marker>();

        /// <summary>
        /// Button data assoicated with the frame
        /// </summary>
        public List<int> buttons = new List<int>();

        /// <summary>
        /// System alerts that were active during the frame
        /// </summary>
        public List<SystemAlert> systemAlerts = new List<SystemAlert>();

        /// <summary>
        /// This flag indicates the data is new (useful for printing with BX2)
        /// </summary>
        public bool dataIsNew;

        /// <summary>
        /// This member is useful for holding static tool information during CSV output
        /// </summary>
        public string toolInfo;

        #endregion

        public override string ToString()
        {
            return string.Format("{0},{1:F3}", transform.ToString(), timespec_s + timespec_ns / 1E9);
        }
    }

    public enum HandleStatus
    {
        Valid = 0x01,
        Missing = 0x02,
        Disabled = 0x04
    }

    public enum FrameType
    {
        Dummy = 0x00,
        ActiveWireless = 0x01,
        Passive = 0x02,
        Active = 0x03,
        Laser = 0x04,
        Illuminated = 0x05,
        Background = 0x06,
        Magnetic = 0x07
    }
}
