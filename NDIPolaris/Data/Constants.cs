//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

namespace NDI.CapiSample.Data
{
    public static class Constants
    {
        /// <summary>
        /// Default float value for uninitialized objects
        /// </summary>
        public const double BAD_FLOAT = -3.697314E28;

        /// <summary>
        /// Warning codes start at 1000.
        /// </summary>
        public const int WARNING_CODE_OFFSET = 1000;

        /// <summary>
        /// These strings translate the warning code (the array index) to a descriptive message
        /// </summary>
        public static readonly string [] WARNING_STRINGS =
        {
            "OKAY", // 0x0 not a warning
	        "Possible hardware fault",
            "The tool violates unique geometry constraints",
            "The tool is incompatible with other loaded tools",
            "The tool is incompatible with other loaded tools and violate design contraints",
            "The tool does not specify a marker wavelength. The system will use the default wavelength."
        };

        /// <summary>
        /// These strings translate the error code (the array index) to a descriptive message
        /// </summary>
        public static readonly string[] ERROR_STRINGS =
        {
            "OKAY", // 0x00 not an error
	        "Invalid command.",
            "Command too long.",
            "Command too short.",
            "Invalid CRC calculated for command.",
            "Command timed out.",
            "Bad COMM settings.",
            "Incorrect number of parameters.",
            "Invalid port handle selected.",
            "Invalid priority.",
            "Invalid LED.",
            "Invalid LED state.",
            "Command is invalid while in the current mode.",
            "No tool is assigned to the selected port handle.",
            "Selected port handle not initialized.",
            "Selected port handle not enabled.",
            "System not initialized.", // 0x10
	        "Unable to stop tracking.",
            "Unable to start tracking.",
            "Tool or SROM fault. Unable to initialize.",
            "Invalid Position Sensor characterization parameters.",
            "Unable to initialize the system.",
            "Unable to start Diagnostic mode.",
            "Unable to stop Diagnostic mode.",
            "Reserved",
            "Unable to read device's firmware version information.",
            "Internal system error.",
            "Reserved",
            "Invalid marker activation signature.",
            "Reserved",
            "Unable to read SROM device.",
            "Unable to write to SROM device.",
            "Reserved", // 0x20
	        "Error performing current test on specified tool.",
            "Marker wavelength not supported.",
            "Command parameter is out of range.",
            "Unable to select volume.",
            "Unable to determine the system's supported features list.",
            "Reserved",
            "Reserved",
            "Too many tools are enabled.",
            "Reserved",
            "No memory is available for dynamic allocation.",
            "The requested port handle has not been allocated.",
            "The requested port handle is unoccupied.",
            "No more port handles available.",
            "Incompatible firmware versions.",
            "Invalid port description.",
            "Requested port is already assigned a port handle.", // 0x30
	        "Reserved",
            "Invalid operation on the requested port handle.",
            "Feature unavailable.",
            "Parameter does not exist.",
            "Invalid value type.",
            "Parameter value is out of range.",
            "Parameter index out of range.",
            "Invalid parameter size.",
            "Permission denied.",
            "Reserved",
            "File not found.",
            "Error writing to file.",
            "Error removing file.",
            "Reserved",
            "Reserved",
            "Invalid or corrupted tool definition", // 0x40
	        "Tool exceeds maximum markers, faces, or groups",
            "Required device not connected",
            "Reserved"
        };
    }
}
