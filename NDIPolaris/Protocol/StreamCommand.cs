//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

// System
using System.Collections.Generic;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// The StreamCommand tells the position sensor to repeatedly send new data without
    /// requiring a request command for each. This helps reduce the latency as a full 
    /// round trip is no longer required.
    /// </summary>
    public class StreamCommand : Command
    {
        /// <summary>
        /// Delegate callback when this command receives a new packet response.
        /// </summary>
        /// <param name="response">New response data</param>
        public delegate void OnNewPacket(Packet response);

        /// <summary>
        /// Unique stream ID used to identify the streamed command.
        /// Defaults to the command used.
        /// </summary>
        private string _streamId = "";

        /// <summary>
        /// Stream ID property, set the default if not specified.
        /// </summary>
        public string StreamId { 
            get => _streamId; 
            private set {
                if(value == "")
                {
                    _streamId = _command;
                }
                else
                {
                    _streamId = value;
                }
            }
        }

        /// <summary>
        /// Frame count divisor to reduce the amount of data received.
        /// </summary>
        public int FrameCountDivisor { get; private set; } = 1;

        /// <summary>
        /// List of listeners to call when new data arrives.
        /// </summary>
        public List<OnNewPacket> Listeners { get; } = new List<OnNewPacket>();

        /// <summary>
        /// Final Command String property.
        /// </summary>
        public override string CommandString {
            get
            {
                string cmd = "STREAM";
                
                if(FrameCountDivisor > 1)
                {
                    cmd += " --interval=" + FrameCountDivisor;
                }

                // TODO: Handle diffs

                cmd += " --id=" + StreamId;
                cmd += " " + base.CommandString;

                return cmd;
            }
        }

        /// <summary>
        /// Create a new command with the specified parameters.
        /// </summary>
        /// <param name="command">Base command to be streamed.</param>
        /// <param name="streamId">Unique Stream ID.</param>
        /// <param name="frameCountDivisor">Integer frame count devisor to reduce rate of data.</param>
        public StreamCommand(string command, string streamId = "", int frameCountDivisor = 1) : base(command)
        {
            StreamId = streamId;
            FrameCountDivisor = frameCountDivisor;
        }

        /// <summary>
        /// Add a response to the command and notify the task and listeners waiting on it. Called by CAPI class.
        /// </summary>
        /// <param name="response">Packet response returned by the position sensor.</param>
        public override void AddResponse(Packet response)
        {
            lock (this)
            {
                // Update the single value to point to the latest response
                Response = response;

                // The stream command first has an ASCII response stating that it
                // was accepted. Notify the task it was received.
                if (!CompletionSource.Task.IsCompleted)
                {
                    CompletionSource.SetResult(true);
                }

                foreach(var listener in Listeners)
                {
                    listener(response);
                }
            }
        }
    }
}
