//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

// System
using System.Threading.Tasks;

namespace NDI.CapiSample.Protocol
{
    /// <summary>
    /// Command class used to keep a record of the command sent to the 
    /// position sensor, and it's response.
    /// </summary>
    public class Command
    {
        /// <summary>
        /// Command string specified to send to the position sensor.
        /// </summary>
        protected string _command;

        /// <summary>
        /// Response packet returned from the position sensor.
        /// </summary>
        public Packet Response { get; protected set; }

        /// <summary>
        /// Completion source used to notify the CAPI class' send function that a response has been provided.
        /// </summary>
        public TaskCompletionSource<bool> CompletionSource { get; } = new TaskCompletionSource<bool>();

        /// <summary>
        /// Flag to keep track if a response has been set.
        /// </summary>
        public bool HasValidResponse { get { return Response != null && Response.IsValid; } }

        /// <summary>
        /// Final command string to be sent to the server with proper carriage return.
        /// </summary>
        public virtual string CommandString { 
            get {
                // If the command ends with a CR then we assume it is correctly formed.
                if (_command.Length > 0 && _command[_command.Length - 1] != '\r')
                {
                    // There must always be a space after the command
                    string suffix = " \r";
                    var parts = _command.Split(' ');

                    // If there are parameters after the space then we don't need to add a space after the parameters.
                    if (parts.Length > 1)
                    {
                        suffix = "\r";
                    }

                    return _command + suffix;
                }
                return _command;
            }
        }

        /// <summary>
        /// Create the command as specified.
        /// </summary>
        /// <param name="command">ASCII command to send to the position sensor.</param>
        public Command(string command)
        {
            _command = command;
        }

        /// <summary>
        /// Add a response to the command and notify the task waiting on it. Called by CAPI class.
        /// </summary>
        /// <param name="response">Packet response returned by the position sensor.</param>
        public virtual void AddResponse(Packet response)
        {
            lock (this)
            {
                Response = response;
                CompletionSource.SetResult(true);
            }
        }
    }
}
