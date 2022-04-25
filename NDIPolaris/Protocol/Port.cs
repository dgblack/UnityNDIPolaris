//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

using System;
using System.IO;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// Tracked Device Port information.
    /// </summary>
    public class Port
    {
        /// <summary>
        /// Internal reference to the CAPI connection so that this object can send its own commands.
        /// </summary>
        private Capi _connection;

        /// <summary>
        /// Port handle to identify a tracked tool
        /// </summary>
        public int PortHandle { get; private set; } = 0;

        /// <summary>
        /// Port Status, see PortStatusFlags enum.
        /// </summary>
        public int Status { get; private set; } = 0;

        /// <summary>
        /// See PortToolType enum.
        /// </summary>
        public PortToolType ToolType { get; private set; } = 0;

        /// <summary>
        /// Manufacturer ID.
        /// </summary>
        public string Manufacturer { get; private set; } = "";

        /// <summary>
        /// Tool Revision
        /// </summary>
        public string Revision { get; private set; } = "";

        /// <summary>
        /// Serial number, see API Guide for conventions.
        /// </summary>
        public int SerialNumber { get; private set; } = 0;

        /// <summary>
        /// Part number of tool, 20 characters.
        /// </summary>
        public string PartNumber { get; private set; } = "";

        /// <summary>
        /// Degrees of Freedom for the port (Aurora)
        /// </summary>
        public DegreesOfFreedom DegreesOfFreedom { get; private set; } = DegreesOfFreedom.Unknown;

        /// <summary>
        /// Number of sensors attached to the port (Aurora)
        /// </summary>
        public int NumberOfSensors { get; private set; } = 0;

        /// <summary>
        /// Initialize the port with its handle and a reference to the connection
        /// </summary>
        /// <param name="portHandle">Port handle identifier.</param>
        /// <param name="connection">Connection to the device.</param>
        /// <param name="status">Status of the port.</param>
        public Port(int portHandle, Capi connection, int status = 0)
        {
            this.PortHandle = portHandle;
            this._connection = connection;
            this.Status = status;
        }

        /// <summary>
        /// Initialize the Port on the device.
        /// </summary>
        /// <returns>True if no error occurred.</returns>
        public bool Initialize()
        {
            var command = _connection.Send($"PINIT {PortHandle:X2}");
            return command.HasValidResponse;
        }

        /// <summary>
        /// Enable the Port on the device.
        /// </summary>
        /// <returns>True if no error occurred.</returns>
        public bool Enable()
        {
            var command = _connection.Send($"PENA {PortHandle:X2}D");
            return command.HasValidResponse;
        }

        /// <summary>
        /// Free the Port on the device.
        /// </summary>
        /// <returns>True if no error occurred.</returns>
        public bool Free()
        {
            var command = _connection.Send($"PHF {PortHandle:X2}");
            return command.HasValidResponse;
        }

        /// <summary>
        /// Load the SROM file onto the device for this Port.
        /// </summary>
        /// <param name="filePath">Relative or absolute path to the device.</param>
        /// <returns>True if no error occurred.</returns>
        public bool LoadSROM(string filePath)
        {
            // Tool data is sent in chunks of 128 hex characters (64-bytes).
            // It must be an integer number of chunks, padded with zeroes at the end.
            const int MESSAGE_SIZE_CHARACTERS = 128;
            const int MESSAGE_SIZE_BYTES = MESSAGE_SIZE_CHARACTERS / 2;

            // Try to read the SROM file.
            byte[] data;
            try
            {
                data = File.ReadAllBytes(filePath);
            }
            catch (Exception e)
            {
                _connection.GetLogger().Invoke("Failed to load SROM file for port " + PortHandle + ": " + e.Message);
                return false;
            }

            // Pad the hex string to match the message size
            string hex = BitConverter.ToString(data).Replace("-", "");
            int numPackets = (int)Math.Ceiling(hex.Length / (float)MESSAGE_SIZE_CHARACTERS);
            hex = hex.PadRight(numPackets * MESSAGE_SIZE_CHARACTERS, '0');

            // Split the hex string into message sized commands
            for(int i = 0; i < numPackets; i++)
            {
                string cmd = String.Format("PVWR {0:X2}{1:X4}{2}", PortHandle, i * MESSAGE_SIZE_BYTES, hex.Substring(i * MESSAGE_SIZE_CHARACTERS, MESSAGE_SIZE_CHARACTERS));
                var command = _connection.Send(cmd);
                if (!command.HasValidResponse)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Update this object with information about the Port.
        /// </summary>
        /// <param name="options">Use the enum PortHandleInfoFlags to build options.</param>
        /// <returns>True if no error occurred.</returns>
        public bool GetInfo(int options = (int)PortHandleInfoFlags.ToolInfo | (int)PortHandleInfoFlags.PartNumber)
        {
            // Send the command and get the text response
            int offset = 0;
            var command = _connection.Send($"PHINF {PortHandle:X2}{options:X4}");
            if (!command.HasValidResponse)
            {
                return false;
            }
            var packet = (AsciiPacket)command.Response;
            string response = packet.Data;

            // Tool Info
            if((options & (int)PortHandleInfoFlags.ToolInfo) != 0)
            {
                int phinf_tool_info_length = 33;

                if(response.Length > offset + phinf_tool_info_length)
                {
                    if (!ParseToolInfo(response.Substring(offset, phinf_tool_info_length)))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                offset += phinf_tool_info_length;
            }

            // Tool Part Number
            if((options & (int)PortHandleInfoFlags.PartNumber) != 0)
            {
                int phinf_tool_part_number_length = 20;

                if(response.Length > offset + phinf_tool_part_number_length)
                {
                    PartNumber = response.Substring(offset, phinf_tool_part_number_length);
                }
                else
                {
                    return false;
                }

                offset += phinf_tool_part_number_length;
            }

            // LED Info
            if ((options & (int)PortHandleInfoFlags.LEDInfo) != 0)
            {
                int phinf_led_info_length = 2;
                // TODO: implement switch and led info
                _connection.GetLogger().Invoke("Device: Reply option 0x0008 not implemented.");
                offset += phinf_led_info_length;
            }

            // Physical Port Location
            if((options & (int)PortHandleInfoFlags.PhysicalPort) != 0)
            {
                int phinf_physical_length = 14;
                // TODO: Implement physical port details
                _connection.GetLogger().Invoke("Device: Reply option 0x0020 not implemented.");
                offset += phinf_physical_length;
            }

            // GPIO Line Definitions
            if((options & (int)PortHandleInfoFlags.GPIOLines) != 0)
            {
                int phinf_gpio_length = 4;
                // TODO: implmeent gpio line details
                _connection.GetLogger().Invoke("Device: Reply option 0x0040 not implemented.");
                offset += phinf_gpio_length;
            }

            // Sensor configuration and physical port location
            if((options & (int)PortHandleInfoFlags.SensorConfig) != 0)
            {
                int phinf_sensor_length = 12;

                if (response.Length > offset + phinf_sensor_length)
                {
                    if (!ParseSensorConfig(response.Substring(offset, phinf_sensor_length)))
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }

                offset += phinf_sensor_length;
            }

            return true;
        }

        /// <summary>
        /// Parse tool info from the response string
        /// </summary>
        /// <param name="response">Response string containing data</param>
        /// <returns>True if data was parsed.</returns>
        private bool ParseToolInfo(string response)
        {
            if (response.Length < 33) return false;

            ToolType = (PortToolType)int.Parse(response.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            // skip 6
            Manufacturer = response.Substring(8, 12);
            Revision = response.Substring(20, 3);
            SerialNumber = int.Parse(response.Substring(23, 8), System.Globalization.NumberStyles.HexNumber);
            Status = int.Parse(response.Substring(31, 2), System.Globalization.NumberStyles.HexNumber);

            return true;
        }

        /// <summary>
        /// Parse sensor info from the response string
        /// </summary>
        /// <param name="response">Response string containing data</param>
        /// <returns>True if data was parsed.</returns>
        private bool ParseSensorConfig(string response)
        {
            if (response.Length < 12) return false;

            DegreesOfFreedom = (DegreesOfFreedom)int.Parse(response.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            NumberOfSensors = int.Parse(response.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);

            // TODO: Physical Port Information
            // SCU Instance
            // Port at SCU
            // Tool Port at SIU
            // Channel

            return true;
        }

        public override string ToString()
        {
            return String.Format("{0:X2},{1},{2},{3},{4}", PortHandle, Manufacturer, PartNumber, SerialNumber, Status);
        }
    }

    public enum PortHandleSearchType
    {
        All = 0,
        PortsToFree = 1,
        NotInit = 2,
        NotEnabled = 3,
        Enabled = 4
    }

    public enum PortStatusFlags
    {
        ToolInPort = 0x01,
        Switch1Closed = 0x02,
        Switch2Closed = 0x04,
        Switch3Closed = 0x08,
        Initialized = 0x10,
        Enabled = 0x20,
        // Reserved
        CurrentlyToolInPort = 0x80
    }

    public enum PortToolType
    {
        Reference = 0x01,
        Probe = 0x02,
        ButtonBox = 0x03,
        SoftwareDefined = 0x04,
        MicroscopeTracker = 0x05,
        // Reserved
        CalibrationDevice = 0x07,
        ToolDockingStation = 0x08,
        IsolationBox = 0x09,
        CArmTracker = 0x0A,
        Catheter = 0x0B
        // Reserved
    }

    public enum PortHandleInfoFlags
    {
        ToolInfo = 0x0001,
        PartNumber = 0x004,
        LEDInfo = 0x0008,
        PhysicalPort = 0x0020,
        GPIOLines = 0x0040,
        SensorConfig = 0x0080 // Aurora
    }

    public enum DegreesOfFreedom
    {
        Unknown = 0,
        Three = 3,
        Five = 5,
        Six = 6
    }
}
