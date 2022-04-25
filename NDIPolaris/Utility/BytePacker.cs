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
using System.Text;

namespace NDI.CapiSample.Utility
{
    /// <summary>
    /// Bytepacking static helper functions for reading binary data from the CAPI protocol.
    /// </summary>
    class BytePacker
    {
        /// <summary>
        /// Unpack up to four bytes of data into an integer value.
        /// </summary>
        /// <param name="buffer">Data buffer to read from.</param>
        /// <param name="size">Number of bytes to read.</param>
        /// <returns>integer value unpacked</returns>
        public static int UnpackInt(Stream buffer, int size = 4)
        {
            if (size < 1 || size > 4)
            {
                throw new Exception("Invalid int byte length.");
            }

            byte[] data = new byte[size];
            for (int r = 0; r < data.Length && buffer.CanRead; r += buffer.Read(data, r, size - r)) ;

            int sum = 0;
            for (int i = 0; i < data.Length; i++)
            {
                sum += data[i] << (i * 8);
            }

            return sum;
        }

        /// <summary>
        /// Unpack four bytes from the Stream into a float.
        /// </summary>
        /// <param name="reader">Binary data buffer.</param>
        /// <returns>float value unpacked.</returns>
        public static float UnpackFloat(Stream reader)
        {
            byte[] data = new byte[4];
            
            if (reader.Read(data, 0, data.Length) != data.Length)
            {
                throw new Exception("Not enough data for float size.");
            }

            return BitConverter.ToSingle(data, 0);
        }

        /// <summary>
        /// Unpack the specified length of characters from the stream.
        /// </summary>
        /// <param name="buffer">IO Stream that can seek.</param>
        /// <param name="length">Number of characters to read.</param>
        /// <returns></returns>
        public static string UnpackString(Stream buffer, int length)
        {
            byte[] data = new byte[length];
            for (int r = 0; r < data.Length && buffer.CanRead; r += buffer.Read(data, r, data.Length - r)) ;
            return Encoding.ASCII.GetString(data, 0, data.Length);
        }
    }
}
