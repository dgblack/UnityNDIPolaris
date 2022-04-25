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
    /// <summary>
    /// This class represents a system condition that may impact tracking performance.
    /// </summary>
    public class SystemAlert
    {
        public SystemAlertCode type;

        /// <summary>
        /// This subtype can be a member of the SystemFaultType, SystemAlertType, or SystemEventType enums.
        /// </summary>
        public int subType;
    }

    /// <summary>
    /// The type of the SystemAlert message.
    /// </summary>
    public enum SystemAlertCode
    {
        Fault = 0x00,
        Alert = 0x01,
        Event = 0x02
    }

    /// <summary>
    /// The subtype of the SystemAlert Fault message.
    /// </summary>
    public enum SystemFaultType
    {
        Ok = 0x0000,
        FatalParameter = 0x0001,
        SensorParameter = 0x0002,
        MainVoltage = 0x0003,
        SensorVoltage = 0x0004,
        IlluminatorVoltage = 0x0005,
        IlluminatorCurrent = 0x0006,
        Sensor0Temp = 0x0007, // left temperature sensor
        Sensor1Temp = 0x0008, // right temperature sensor
        MainTemp = 0x0009,
        SensorMalfunction = 0x000a
    }

    /// <summary>
    /// The subtype of the SystemAlert Alert message.
    /// </summary>
    public enum SystemAlertType
    {
        Ok = 0x0000,
        BatteryLow = 0x0001,
        BumpDetected = 0x0002,
        IncompatibleFirmware = 0x0003,
        NonFatalParameter = 0x0004,
        FlashMemoryFull = 0x0005,
        // Reserved = 0x0006,
        StorageTempExceeded = 0x0007,
        TempHigh = 0x0008,
        TempLow = 0x0009,
        ScuDisconnected = 0x000a,
        PtpClockSynch = 0x000e
    }

    /// <summary>
    /// The subtype of the SystemAlert Event message.
    /// </summary>
    public enum SystemEventType
    {
        Ok = 0x0000,
        ToolPluggedIn = 0x0001,
        ToolUnplugged = 0x0002,
        SiuPluggedIn = 0x0003,
        SiuUnplugged = 0x0004
    }
}
