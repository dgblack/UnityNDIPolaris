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
using System.IO;
using System.Collections.Generic;

namespace NDI.CapiSample.Utility
{
    /// <summary>
    /// Implement the IO Stream class wrapping another stream with a buffer that can see.
    /// The C# BufferedStream does not support seeking if the base stream does not.
    /// </summary>
    public class SeekableBufferedStream : Stream
    {
        /// <summary>
        /// Base stream to read/write with.
        /// </summary>
        private Stream _sourceStream;

        /// <summary>
        /// Buffer storage
        /// </summary>
        private List<byte> _streamBuffer = new List<byte>();

        /// <summary>
        /// Cursor position in the buffer.
        /// </summary>
        private int _cursor = 0;

        /// <summary>
        /// Maximum buffer size, if the buffer exceeds this value the data will be discarded.
        /// </summary>
        private int _maxBufferSize = 1024;

        /// <summary>
        /// Wrap the specified stream with a buffer.
        /// </summary>
        /// <param name="stream">Base stream to read/write with.</param>
        /// <param name="maxSize">Maximum buffer size, if the buffer exceeds this value the data will be discarded.</param>
        public SeekableBufferedStream(Stream stream, int maxSize = 1024)
        {
            _cursor = 0;
            _sourceStream = stream;
            _maxBufferSize = maxSize;
        }

        /// <summary>
        /// Change the base stream without flushing the received bytes buffer for seeking.
        /// </summary>
        /// <param name="stream">The new base stream to read/write with.</param>
        public void SetBaseStream(Stream stream)
        {
            _sourceStream = stream;
        }

        /// <summary>
        /// Indicate whether the stream can be read from.
        /// </summary>
        public override bool CanRead => _sourceStream.CanRead;

        /// <summary>
        /// Indicate whether the stream can seek.
        /// </summary>
        public override bool CanSeek => true;

        /// <summary>
        /// Indicate whether the stream can be written to.
        /// </summary>
        public override bool CanWrite => _sourceStream.CanWrite;

        /// <summary>
        /// The current buffer length.
        /// </summary>
        public override long Length => _streamBuffer.Count;

        /// <summary>
        /// Get or Set the current cursor position in the buffer.
        /// </summary>
        public override long Position { 
            get => _cursor;
            set { 
                if(value >= 0 && value < _streamBuffer.Count)
                {
                    _cursor = (int)value;
                }
                else
                {
                    throw new ArgumentException("Invalid position value.");
                }
            } 
        }

        /// <summary>
        /// Clear the buffer and flush the base stream.
        /// </summary>
        public override void Flush()
        {
            _cursor = 0;
            _streamBuffer.Clear();
            _sourceStream.Flush();
        }

        /// <summary>
        /// Read the specified number of bytes into the buffer at the offset position.
        /// </summary>
        /// <param name="buffer">Buffer to read bytes into.</param>
        /// <param name="offset">Offset index in 'buffer' to place the bytes read.</param>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            if(buffer.Length < offset + count)
            {
                throw new ArgumentException("Buffer too small.");
            }

            // Read any bytes that exist in the buffer at the cursor location first
            int read = 0;
            while(_cursor < _streamBuffer.Count && read < count)
            {
                buffer[offset + read] = _streamBuffer[_cursor];
                _cursor++;
                read++;
            }

            // If we still need to read more, then start reading from the base stream.
            if(read < count && _sourceStream.CanRead)
            {
                // Try to read the remaining number of bytes.
                byte[] tmp = new byte[count - read];
                int r = _sourceStream.Read(tmp, 0, tmp.Length);

                // But we may not have read the full amount, so create a new array for easy adding to our buffer.
                byte[] tmp2 = new byte[r];
                Array.Copy(tmp, tmp2, r);

                // Add to our buffer and move the cursor to the new endpoint
                _streamBuffer.AddRange(tmp2);
                _cursor += r;

                // Copy the data out to the new offset
                Array.Copy(tmp2, 0, buffer, offset + read, r);
                read += r;
            }

            // Trim the buffer so that we don't consume all the memory.
            while (_streamBuffer.Count > _maxBufferSize)
            {
                _streamBuffer.RemoveAt(0);
                _cursor--;
            }

            return read;
        }

        /// <summary>
        /// Seek to a new position in the buffer.
        /// </summary>
        /// <param name="offset">Amount to offset by from the SeekOrigin.</param>
        /// <param name="origin">SeekOrigin to start seeking from.</param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            switch(origin)
            {
                case SeekOrigin.Begin:
                    return Position = offset;
                case SeekOrigin.Current:
                    return Position = (Position + offset);
                case SeekOrigin.End:
                    return Position = (_streamBuffer.Count + offset);
            }

            // If we received an unknown SeekOrigin, Seek to the end.
            return Position = _streamBuffer.Count;
        }

        /// <summary>
        /// Set the maximum length of the buffer.
        /// </summary>
        /// <param name="value">New maximum length</param>
        public override void SetLength(long value)
        {
            _maxBufferSize = (int)value;

            // Trim the buffer so that we don't consume all the memory.
            while (_streamBuffer.Count > _maxBufferSize)
            {
                _streamBuffer.RemoveAt(0);
                _cursor--;
            }
        }

        /// <summary>
        /// Write any data directly to the base stream.
        /// </summary>
        /// <param name="buffer">Buffer of data to write from.</param>
        /// <param name="offset">Offset in the buffer to start writing.</param>
        /// <param name="count">Number of bytes to write.</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            _sourceStream.Write(buffer, offset, count);
        }
    }
}
