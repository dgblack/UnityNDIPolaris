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
using System.Net.Sockets;

// NDI
using NDI.CapiSample.Utility;
using NDI.CapiSample.Protocol;

namespace NDI.CapiSample
{
    /// <summary>
    /// Connect to a device CAPI device over TCP.
    /// </summary>
    public class CapiTcp : Capi
    {
        private string _ip;
        private int _port;
        private TcpClient _tcp;

        /// <summary>
        /// Store connection info for the standardized connect() call.
        /// </summary>
        /// <param name="ip">IP Address of Tracking Device</param>
        /// <param name="port">Port of Tracking Device</param>
        public CapiTcp(string ip, int port = 8765)
        {
            _ip = ip;
            _port = port;
            _tcp = new TcpClient();
        }

        public override string GetConnectionInfo()
        {
            return _ip + ":" + _port;
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

                _tcp.Connect(_ip, _port);
                _tcp.ReceiveTimeout = 3000;
                if (!_tcp.Connected)
                {
                    return false;
                }
                _stream = new SeekableBufferedStream(_tcp.GetStream(), BUFFER_SIZE);
                IsConnected = true;
            }
            catch (Exception e)
            {
                _logger("Exception: " + e.Message);
                IsConnected = false;
                return false;
            }

            Listen();
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

            if (_tcp.Connected)
            {
                _tcp.Close();
            }

            if (_listenThread != null)
            {
                _listenThread.Join();
            }

            return true;
        }
    }
}
