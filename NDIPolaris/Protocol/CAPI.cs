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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

// NDI
using NDI.CapiSample.Data;
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol
{
    // Logging delegate
    public delegate void Logger(string message);

    /// <summary>
    /// Combined API Protocol implementation.
    ///
    /// This is an abstract class that handles the CAPI protocol without any communication implementations.
    /// Inherit from this class with your communication layer implementation. See CAPISerial for an example.
    /// </summary>
    public abstract class Capi
    {
        /// <summary>
        /// Stream buffer size; this is the maximum we can seek backwards when reading.
        /// </summary>
        protected const int BUFFER_SIZE = 1024;

        /// <summary>
        /// Logging delegate
        /// </summary>
        protected Logger _logger;

        /// <summary>
        /// Buffered data stream
        /// </summary>
        protected SeekableBufferedStream _stream;

        /// <summary>
        /// We have a connection to the hardware
        /// </summary>
        public bool IsConnected { protected set; get; }

        /// <summary>
        /// TSTART has been called without error
        /// </summary>
        public bool IsTracking { protected set; get; }

        /// <summary>
        /// Log values being sent and received.
        /// </summary>
        public bool LogTransit { set; get; }

        /// <summary>
        /// Non streaming commands receive responses in the order that they are sent.
        /// </summary>
        private Queue<Command> _nonStreamingCommands = new Queue<Command>();

        /// <summary>
        /// Streaming commands receive responses with their associated streamId. This maps the streamId to the command.
        /// </summary>
        private Dictionary<string, StreamCommand> _streamingCommands = new Dictionary<string, StreamCommand>();

        /// <summary>
        /// Listen thread to be joined when Disconnect() is called.
        /// </summary>
        protected Thread _listenThread = null;

        /// <summary>
        /// Initialize with sane defaults.
        /// </summary>
        public Capi()
        {
            LogTransit = false;
            IsConnected = false;
            IsTracking = false;
            SetLogger(null);
        }

        /// <summary>
        /// Get the sample code version.
        /// </summary>
        /// <returns>Version string</returns>
        public static string GetVersion()
        {
            return typeof(Capi).Assembly.GetName().Version.ToString();
        }

        /// <summary>
        /// Changes the logging function.
        /// </summary>
        /// <param name="logger"></param>
        public void SetLogger(Logger logger)
        {
            _logger = logger;

            // Default logger to print to console
            if (_logger == null)
            {
                _logger = new Logger((message) =>
                {
                    Console.WriteLine(message);
                });
            }
        }

        public Logger GetLogger()
        {
            return _logger;
        }

        #region ProtocolAbstraction

        /// <summary>
        /// Return the connection info required by the connection implementation.
        /// </summary>
        /// <returns>Human readable string of connection information.</returns>
        public abstract string GetConnectionInfo();

        /// <summary>
        /// Connect to the system hardware. Override this in a child class to implement.
        ///
        /// See CAPISerial for a serial example.
        /// </summary>
        /// <returns>True if connection succeeds.</returns>
        public abstract bool Connect();

        /// <summary>
        /// Disconnect from the system hardware. Override this in a child class implementation.
        ///
        /// See CAPISerial for a serial example.
        /// </summary>
        /// <returns>True if already disconnected or the disconnection succeeds.</returns>
        public abstract bool Disconnect();
        #endregion

        #region SendReceive

        /// <summary>
        /// Start a listen thread to read from the connection stream. This must be the
        /// only thread reading from the stream and should be called in the communication 
        /// implementation of this class, see CapiSerial or CapiTcp.
        /// 
        /// Each Command will be notified of it's response when it is read from the stream.
        /// </summary>
        public void Listen()
        {
            _listenThread = new Thread(() =>
            {
            // Only read while connected.
                while (IsConnected && _stream.CanRead)
                {
                    Packet packet = null;

                    try
                    {
                        // Blocking build a packet
                        packet = Packet.BuildPacket(_stream);
                    }
                    catch (Exception e)
                    {
                        // If we were trying to disconnect, don't show a read error message
                        if (IsConnected)
                        {
                            _logger(e.Message);
                        }
                        break;
                    }

                    // Log raw communication
                    if (LogTransit)
                    {
                        _logger("<< " + packet);
                    }

                    // Log error message if present
                    if (!packet.IsValid && packet is AsciiPacket)
                    {
                        _logger(((AsciiPacket)packet).GetErrorString());
                    }

                    // Streaming packets are responses to the STREAM command and contain AsciiPackets or BinaryPackets
                    if (packet is StreamPacket)
                    {
                        var streamPacket = (StreamPacket)packet;

                        // Protect the dictionary with a lock because new streaming commands may be added while we receive packets.
                        lock (_streamingCommands)
                        {
                            if (_streamingCommands.ContainsKey(streamPacket.StreamId))
                            {
                                _streamingCommands[streamPacket.StreamId].AddResponse(streamPacket.Contents);
                            }
                            else
                            {
                                _logger($"Packet received for StreamId '{streamPacket.StreamId}' but we did not start listening for it.");
                            }
                        }
                    }
                    else
                    {
                        // Non-streaming commands are request-response, all commands will receive a response in the order they were sent.
                        // Protect the queue with a lock, as new commands may be sent prior to receiving the response.
                        lock (_nonStreamingCommands)
                        {
                            if (_nonStreamingCommands.Count > 0)
                            {
                                var cmd = _nonStreamingCommands.Dequeue();
                                cmd.AddResponse(packet);
                            }
                            else
                            {
                                _logger("Packet not handled: " + packet);
                            }
                        }
                    }
                }

                _logger("Listen thread stopped.");
                IsConnected = false;
                IsTracking = false;
                _stream.Close();

                // If there are still pending commands, then we became disconnected unexpectedly,
                // notify any waiting tasks with cancelled exceptions.
                foreach(var cmd in _nonStreamingCommands)
                {
                    cmd.CompletionSource.SetException(new TaskCanceledException("Could not read data."));
                }

                // Clear any pending commands
                _nonStreamingCommands.Clear();
                _streamingCommands.Clear();
            });
            _listenThread.Start();
        }

        /// <summary>
        /// Send the STREAM command with the specified command to be streamed. This requires API version G.003 or newer.
        /// 
        /// You must implement the OnNewPacket delegate and add it to the StreamCommand's Listeners property to receive updates.
        /// </summary>
        /// <param name="command">Command to be streamed from the position sensor.</param>
        /// <param name="streamId">Unique string identifier for the stream.</param>
        /// <param name="frameCountDivisor">Frame count divisor to reduce the frequency of updates.</param>
        /// <returns></returns>
        public StreamCommand StartStreaming(string command, string streamId = "", int frameCountDivisor = 1)
        {
            StreamCommand cmd = new StreamCommand(command, streamId, frameCountDivisor);

            // Protect the record of stream commands as it is being checked in the listen thread.
            lock (_streamingCommands)
            {
                if (_streamingCommands.ContainsKey(cmd.StreamId))
                {
                    _logger($"The StreamID '{streamId}' is taken. Stop streaming it or use a different StreamID.");
                    return null;
                }

                _streamingCommands.Add(cmd.StreamId, cmd);
            }

            // We do not need to send the command in the lock because the order does not matter.
            if (cmd != null)
            {
                // The STREAM command itself is not a streaming command and will receive an ascii response through the non-streaming response queue.
                var task = Send(cmd);
                task.Wait(-1);
            }

            return cmd;
        }

        /// <summary>
        /// Stop streaming the specified command.
        /// </summary>
        /// <param name="command">StreamCommand to stop.</param>
        /// <returns>True if the position sensor returned okay.</returns>
        public bool StopStreaming(StreamCommand command)
        {
            return StopStreaming(command.StreamId);
        }

        /// <summary>
        /// Stop streaming the specified stream id.
        /// </summary>
        /// <param name="streamId">Stream id to stop.</param>
        /// <returns>True if the position sensor returned okay.</returns>
        public bool StopStreaming(string streamId)
        {
            var cmd = Send($"USTREAM --id={streamId}");

            if (cmd.HasValidResponse)
            {
                lock (_streamingCommands)
                {
                    if (_streamingCommands.ContainsKey(streamId))
                    {
                        _streamingCommands.Remove(streamId);
                    }
                }
                return true;
            }
            else
            {
                _logger($"Failed to stop the StreamID '{streamId}'.");
            }

            return cmd.HasValidResponse;
        }

        /// <summary>
        /// Send a string command to the position sensor and wait for it's response.
        /// This function is synchronous.
        /// </summary>
        /// <param name="command">Command string to be sent to the position sensor.</param>
        /// <param name="expectResponse">If set to false the position sensor's response will be discarded.</param>
        /// <returns>Command that was sent to the server and contains the response packet.</returns>
        public Command Send(string command, bool expectResponse = true)
        {
            var cmd = new Command(command);
            var task = Send(cmd, expectResponse);
            try
            {
                // Aurora may take up to 12 seconds to reply. (Section 2.4 of DDUG-010-466-08-Aurora Application Program Interface Guide Rev 8)
                task.Wait(1000);
            }
            catch(Exception e)
            {
                _logger("Failed to receive response for command: " + command);
            }
            return cmd;
        }

        /// <summary>
        /// Send a Command to the position sensor and wait for it's response.
        /// This function is asynchronous.
        /// </summary>
        /// <param name="command">Command to be sent to the position sensor.</param>
        /// <param name="expectResponse">If set to false the position sensor's response will be discarded.</param>
        /// <returns>A task that will be completed with a boolean response indicating the response is valid or that the command could be sent.</returns>
        public async Task<bool> Send(Command command, bool expectResponse = true)
        {
            bool couldSend = false;

            // We need to add the command to our queue and send it within the lock
            // so that the order of commands remains consistent.
            lock (_nonStreamingCommands)
            {
                // If we expect a response, add this command to our queue.
                if (expectResponse)
                {
                    _nonStreamingCommands.Enqueue(command);
                }

                couldSend = Write(command.CommandString);
            }

            // If we expect a response, then we must wait for the command's 
            // completion source task to complete.
            if (expectResponse)
            {
                await command.CompletionSource.Task;
                return command.HasValidResponse;
            }
            else
            {
                // If we are not expecting a response, just return if we were able to send the command.
                return couldSend;
            }
        }

        /// <summary>
        /// Write an ASCII message to the connection stream.
        /// </summary>
        /// <param name="command">Command with options to be sent. Preferrably terminated with `\r`</param>
        /// <returns>ASCII data response. Empty if not waiting.</returns>
        private bool Write(string command)
        {
            if (command.Length == 0) return false;

            if (LogTransit)
            {
                _logger(">> " + command);
            }

            byte[] bytes = Encoding.ASCII.GetBytes(command);

            // Append the terminator suffix.
            if (bytes[bytes.Length - 1] != (byte)'\r')
            {
                byte[] fixedCommand = new byte[bytes.Length + 1];
                Array.Copy(bytes, fixedCommand, bytes.Length);
                fixedCommand[fixedCommand.Length - 1] = (byte)'\r';
                bytes = fixedCommand;
            }

            try
            {
                if (_stream.CanWrite)
                {
                    _stream.Write(bytes, 0, bytes.Length);
                    return true;
                }
            }
            catch (Exception e)
            {
                _logger(e.Message);
            }

            IsConnected = false;
            IsTracking = false;
            _stream.Close();
            return false;
        }
        #endregion

        #region Commands

        /// <summary>
        /// Send the RESET command.
        ///
        /// Different connection methods may need to override this. (Ex. Serial).
        /// </summary>
        /// <returns>True if sent.</returns>
        public virtual bool Reset()
        {
            var cmd = Send("RESET", false);
            IsTracking = false;
            return true;
        }

        /// <summary>
        /// Initialize the system.
        /// </summary>
        /// <returns>True if successful.</returns>
        public bool Initialize()
        {
            var cmd = Send("INIT");
            return cmd.HasValidResponse;
        }

        /// <summary>
        /// Get the API Revision number.
        /// </summary>
        /// <returns>String revision number.</returns>
        public string GetAPIRevision()
        {
            var cmd = Send("APIREV");
            if (cmd.HasValidResponse)
            {
                return ((AsciiPacket)cmd.Response).Data;
            }
            return null;

        }

        /// <summary>
        /// Get the string response of the GET user parameter command.
        /// </summary>
        /// <param name="parameter">Parameter you want to retrieve.</param>
        /// <returns>String of key value pair(s).</returns>
        public string GetUserParameter(string parameter)
        {
            // TODO: Parse all user parameter values in the response
            var cmd = Send($"GET {parameter}");
            if (cmd.HasValidResponse)
            {
                return ((AsciiPacket)cmd.Response).Data;
            }
            return null;
        }

        /// <summary>
        /// Set the user parameter with the value specified
        /// </summary>
        /// <param name="key">User parameter name</param>
        /// <param name="value">String value to set</param>
        /// <returns></returns>
        public bool SetUserParameter(string key, string value)
        {
            var command = Send($"SET {key}={value}");
            return command.HasValidResponse;
        }

        /// <summary>
        /// Send the TSTART command.
        /// </summary>
        /// <returns>True if accepted.</returns>
        public bool TrackingStart()
        {
            if (IsTracking)
            {
                return true;
            }

            var command = Send("TSTART");
            IsTracking = command.HasValidResponse;
            return IsTracking;
        }

        /// <summary>
        /// Send the TSTOP command.
        /// </summary>
        /// <returns>True if accepted.</returns>
        public bool TrackingStop()
        {
            if (!IsTracking)
            {
                return true;
            }

            var command = Send("TSTOP");
            if (!command.HasValidResponse)
            {
                return false;
            }

            IsTracking = false;
            return true;
        }

        /// <summary>
        /// Send the BX command to retrieve pose.
        /// </summary>
        /// <param name="options">Options should bitwise combine options like Transform Data(0001) or Report All Transforms(0800)</param>
        /// <returns>List of tool pose data.</returns>
        public List<Tool> SendBX(int options = (int)BxReplyOptionFlags.TransformData | (int)BxReplyOptionFlags.AllTransforms)
        {
            // TODO: Parse other data options
            if ((options & ~((int)BxReplyOptionFlags.TransformData | (int)BxReplyOptionFlags.AllTransforms)) != 0)
            {
                _logger(String.Format("Reply parsing has not been implemented for BX options other than 0x{0:X4}.", (int)BxReplyOptionFlags.TransformData | (int)BxReplyOptionFlags.AllTransforms));
                return new List<Tool>();
            }

            var command = Send($"BX {options:X4}");
            if (command.HasValidResponse)
            {
                return BxReply.Parse((BinaryPacket)command.Response, options);
            }

            return new List<Tool>();
        }

        /// <summary>
        /// Send the BX2 command to retrieve pose.
        /// </summary>
        /// <param name="options">A string containing the BX2 options described in the Vega API guide</param>
        /// <returns>Tool data for all enabled tools that have new data since the last BX2 command, or an empty list.</returns>
        public List<Tool> SendBX2(string options = "")
        {
            var command = Send($"BX2 {options}");
            if (command.HasValidResponse)
            {
                return Bx2Reply.Parse((BinaryPacket)command.Response);
            }
            return new List<Tool>();
        }

        /// <summary>
        /// Request the next available port handle.
        /// </summary>
        /// <param name="hardwareDevice">Eight character name. Use wildcards "********" or a device name returned by PHINF</param>
        /// <param name="systemType">One character defining the system type. Use a wildcard "*"</param>
        /// <param name="toolType">One character defining the tool type. Wired="0" or Wireless="1" (passive or active-wireless)</param>
        /// <param name="portNumber">Two characters defining the port number: [01-03] for wired tools, 00 or ** for wireless tools.</param>
        /// <param name="dummyTool">
        ///     If you will use PVWR to load a tool definition file, use wildcards: "**"
        ///     If ToolType = Wired, either "01" or "02" adds the active wired dummy tool.
        ///     If ToolType = Wireless, "01" adds the passive dummy tool, "02" adds the active wireless dummy tool.
        /// </param>
        /// <returns>The requested port as a new object, or null on error.</returns>
        public Port PortHandleRequest(string hardwareDevice = "********", string systemType = "*", string toolType = "1", string portNumber = "00", string dummyTool = "**")
        {
            var command = Send(String.Format("PHRQ {0}{1}{2}{3}{4}", hardwareDevice, systemType, toolType, portNumber, dummyTool));

            if (!command.HasValidResponse)
            {
                return null;
            }

            var response = (AsciiPacket)command.Response;
            return new Port(int.Parse(response.Data.Substring(0, 2), System.Globalization.NumberStyles.HexNumber), this);
        }

        /// <summary>
        /// Search for port handles using PHSR.
        /// </summary>
        /// <param name="type">The PortHandleSearchType that defines what data is returned.</param>
        /// <returns> A list of Port objects, or an empty list if an error occurred.</returns>
        public List<Port> PortHandleSearchRequest(PortHandleSearchType type)
        {
            List<Port> list = new List<Port>();
            var command = Send($"PHSR {(int)type:X2}");

            // Check for errors
            if (!command.HasValidResponse)
            {
                return list;
            }

            var packet = (AsciiPacket)command.Response;
            string response = packet.Data;

            // Get the number of ports found
            int numPorts = int.Parse(response.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
            if (numPorts <= 0)
            {
                return list;
            }

            // Read the port details
            for (int i = 0; i < numPorts; i++)
            {
                int portHandle = int.Parse(response.Substring(i * 5 + 2, 2), System.Globalization.NumberStyles.HexNumber);
                int status = int.Parse(response.Substring(i * 5 + 4, 3), System.Globalization.NumberStyles.HexNumber);

                Port p = new Port(portHandle, this, status);

                list.Add(p);
            }

            return list;
        }
        #endregion
    }
}
