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
using System.IO.Ports;
using System.Threading;

// NDI
using NDI.CapiSample.Utility;
using NDI.CapiSample.Protocol;

namespace NDI.CapiSample
{
    /// <summary>
    /// Connect to a device CAPI device over Serial.
    /// </summary>
    public class CapiSerial : Capi
    {
        private string _comPort;
        private SerialPort _serial = new SerialPort();

        /// <summary>
        /// Store connection info for the standardized connect() call.
        /// </summary>
        /// <param name="comPort">COM port name.</param>
        /// <param name="maximumBaudrate">Use the maximum supported baudrate for the device.</param>
        public CapiSerial(string comPort)
        {
            _comPort = comPort;
        }

        /// <summary>
        /// List all available COM ports by name (Ex. COM3)
        /// </summary>
        /// <returns>Array of COM port names.</returns>
        public static string[] GetAvailableComPorts()
        {
            return SerialPort.GetPortNames();
        }

        /// <summary>
        /// Return the connection info.
        /// </summary>
        /// <returns></returns>
        public override string GetConnectionInfo()
        {
            return _comPort;
        }

        /// <summary>
        /// Connect to the serial device.
        /// </summary>
        /// <returns>True if connection succeeds.</returns>
        public override bool Connect()
        {
            try
            {
                IsConnected = false;
                IsTracking = false;

                // Default Connection Settings
                _serial = new SerialPort(_comPort, 9600, Parity.None, 8, StopBits.One);
                // Aurora may take up to 12 seconds to reply. (Section 2.4 of DDUG-010-466-08-Aurora Application Program Interface Guide Rev 8)
                _serial.ReadTimeout = _serial.WriteTimeout = 15000;
                _serial.Handshake = Handshake.None;

                if (!_serial.IsOpen)
                {
                    _serial.Open();
                }

                if (!_serial.IsOpen)
                {
                    return false;
                }

                Thread.Sleep(1000);

                _stream = new SeekableBufferedStream(_serial.BaseStream, BUFFER_SIZE);

                if (!SetCommunicationParams())
                {
                    return false;
                }
                
                IsConnected = true;
                Listen();
            }
            catch (Exception e)
            {
                _logger("Exception: " + e.Message);
                IsConnected = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Disconnect from the serial device.
        /// </summary>
        /// <returns>True if already disconnected or the disconnection succeeds.</returns>
        public override bool Disconnect()
        {
            if (IsTracking)
            {
                TrackingStop();
            }

            IsConnected = false;
            IsTracking = false;
            _serial.Close();

            if (_listenThread != null)
            {
                _listenThread.Join();
            }

            return true;
        }

        /// <summary>
        /// Set the serial parameters on both ends.
        /// Check the maximum baud rate supported as specified in your API Guide for your device.
        /// </summary>
        /// <returns>True of successfully set.</returns>
        public bool SetCommunicationParams(SerialBaudRates baudRate = SerialBaudRates.Baud921600, int dataBits = 8, Parity parity = Parity.None, StopBits stopBits = StopBits.One, bool enableHandshake = true)
        {
            // Serial break causes reset
            _serial.BreakState = true;
            Thread.Sleep(500);
            _serial.BreakState = false;
            Thread.Sleep(250);

            // Default Parameters
            _serial.BaudRate = GetRealBaudRateValue(SerialBaudRates.Baud9600);
            _serial.DataBits = 8;
            _serial.Parity = Parity.None;
            _serial.StopBits = StopBits.One;
            _serial.Handshake = Handshake.None;

            // RESET
            // We're not listening yet at this point, but the serial break sends an okay message back that needs to be read before we send the comm params.
            AsciiPacket response = (AsciiPacket)Packet.BuildPacket(_stream);
            if (!response.IsValid)
            {
                _logger("Failed to send break.");
            }

            // Send new parameters
            Send(String.Format("COMM {0:D}{1:D}{2:D}{3:D}{4:D}\r", (int)baudRate, dataBits == 8 ? 0 : 1, parity, stopBits == StopBits.One ? 0 : 1, enableHandshake ? 1 : 0), false);
            Thread.Sleep(100);

            // Update our connection settings
            _serial.Close();
            _serial.BaudRate = GetRealBaudRateValue(baudRate);
            _serial.DataBits = dataBits;
            _serial.Parity = parity;
            _serial.StopBits = stopBits;
            // Aurora may take up to 12 seconds to reply. (Section 2.4 of DDUG-010-466-08-Aurora Application Program Interface Guide Rev 8)
            _serial.ReadTimeout = _serial.WriteTimeout = 15000;
            if (enableHandshake)
            {
               _serial.Handshake = Handshake.RequestToSend;
            }
            else
            {
                _serial.Handshake = Handshake.None;
            }
            _serial.Open();
            _serial.DiscardInBuffer();
            _serial.DiscardOutBuffer();

            // Need to get the stream again after closing and opening.
            _stream.SetBaseStream(_serial.BaseStream);

            return true;
        }

        /// <summary>
        /// Reset the device and baudrate.
        /// </summary>
        /// <returns>True if successful.</returns>
        public override bool Reset()
        {
            base.Reset();

            IsConnected = false;

            if(_listenThread != null)
            {
                _listenThread.Join();
            }

            if (!SetCommunicationParams())
            {
                return false;
            }

            IsConnected = true;
            Listen();

            return true;
        }

        /// <summary>
        /// Convert the enum value into the actual Baud Rate value.
        /// </summary>
        /// <param name="baudRate">Enum value</param>
        /// <returns>Baudrate integer value</returns>
        private int GetRealBaudRateValue(SerialBaudRates baudRate)
        {
            switch(baudRate)
            {
                case SerialBaudRates.Baud9600: return 9600;
                case SerialBaudRates.Baud14400: return 14400;
                case SerialBaudRates.Baud19200: return 19200; // Note: Previously aliased to 1.2MBaud
                case SerialBaudRates.Baud38400: return 38400;
                case SerialBaudRates.Baud57600: return 57600;
                case SerialBaudRates.Baud115200: return 115200;
                case SerialBaudRates.Baud921600: return 921600;
                case SerialBaudRates.Baud1228739: return 1228739;
            }
            return 0;
        }
    }

    public enum SerialBaudRates {
        Baud9600 = 0,
        Baud14400 = 1,
        Baud19200 = 2,
        Baud38400 = 3,
        Baud57600 = 4,
        Baud115200 = 5,
        Baud921600 = 6,
        Baud1228739 = 7
    };
}
