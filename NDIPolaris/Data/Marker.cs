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
    public class Marker
    {
        /// <summary>
        /// Marker tracking status.
        /// </summary>
        public MarkerStatus status;

        /// <summary>
        /// Marker index in the tracked tool.
        /// </summary>
        public int index;

        /// <summary>
        /// 3D Position of the marker relative to the camera
        /// </summary>
        public Vector3 position = new Vector3();

        public override string ToString()
        {
            return string.Format("{0:X2},{1},{2}", index, Enum.GetName(typeof(MarkerStatus), status), position.ToString());
        }
    }

    /// <summary>
    /// Marker Status
    /// </summary>
    public enum MarkerStatus
    {
        Okay = 0x00,
        Missing = 0x01,
        OutOfVolume = 0x05,
        PossiblePhantom = 0x06,
        Saturated = 0x07,
        SaturatedOutOfVolume = 0x08
    }
}
