//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

using System;

namespace NDI.CapiSample.Data
{
    public class Transform
    {
        public const int STATUS_MASK_MISSING = 0x0100;
        public const int STATUS_MASK_FACE = 0xE000;
        public const int STATUS_MASK_CODE = 0x00FF;

        public Transform()
        {
            toolHandle = 0;
            rawStatus = STATUS_MASK_MISSING;
            position = new Vector3();
            orientation = new Quaternion();
        }

        /// <summary>
        /// Tool Handle identifier
        /// </summary>
        public int toolHandle;
        
        /// <summary>
        /// Tool Position
        /// </summary>
        public Vector3 position;

        /// <summary>
        /// Tool Orientation
        /// </summary>
        public Quaternion orientation;

        /// <summary>
        /// 3D Fit Error
        /// </summary>
        public double error;

        /// <summary>
        /// Raw Status value passed over the wire, this gets split into `isMissing`, `faceNumber`, and `status`.
        /// </summary>
        public int rawStatus;

        /// <summary>
        /// True if the tool is missing.
        /// </summary>
        public bool isMissing;

        /// <summary>
        /// Tool Face number being used to track.
        /// </summary>
        public int faceNumber;

        /// <summary>
        /// Tracking status for the tool.
        /// </summary>
        public TransformStatus status;

        public override string ToString()
        {
            return string.Format("{0:X2},{1},{2:X2},{3},{4},{5,8:F3}", toolHandle, Enum.GetName(typeof(TransformStatus), status), faceNumber, position.ToString(), orientation.ToString(), error);
        }
    }

    /// <summary>
    /// The least significant eight bits of the transform status is an error code in this list
    /// </summary>
    public enum TransformStatus
    {
        Enabled = 0x00,
        PartiallyOutOfVolume = 0x03,
        OutOfVolume = 0x09,
        TooFewMarkers = 0x0D,
        Inteference = 0x0E,
        BadTransformFit = 0x11,
        DataBufferLimit = 0x12,
        AlgorithmLimit = 0x13,
        FellBehind = 0x14,
        OutOfSynch = 0x15,
        ProcessingError = 0x16,
        ToolMissing = 0x1F,
        TrackingNotEnabled = 0x20,
        ToolUnplugged = 0x21
    }
}
