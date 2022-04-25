//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

namespace NDI.CapiSample.Utility
{
    class CRC
    {
        /// <summary>
        /// Lookup table for CRC polynomial.
        /// </summary>
        private static uint[] _crcTable = new uint[256];

        /// <summary>
        /// Keep track of an initialization state so that we don't need to manualy initialize before use.
        /// </summary>
        private static bool _initialized = false;

        /// <summary>
        /// Populate the CRC lookup table
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;

            for (int i = 0; i < 256; i++)
            {
                long lCrcTable = i;
                for (int j = 0; j < 8; j++)
                {
                    lCrcTable = (lCrcTable >> 1) ^ ((lCrcTable & 1) != 0 ? 0xA001L : 0);
                }
                _crcTable[i] = (uint) lCrcTable & 0xFFFF;
            }
        }

        /// <summary>
        /// Calculates the CRC16 of the data
        /// </summary>
        /// <param name="data">The reply without its trailing CRC16 + CR</param>
        /// <returns>The CRC16 of the reply</returns>
        public static uint CalculateCRC16(byte [] data)
        {
            Initialize();

            uint uCRC = 0;
            foreach(byte b in data)
            {
                uCRC = CalculateValue(uCRC, (char)b);
            }
            return uCRC;
        }

        /// <summary>
        /// Calculates a CRC value using a lookup table
        /// </summary>
        /// <param name="crc">The previous value of the running CRC</param>
        /// <param name="data">The data to add to the running CRC</param>
        /// <returns>The value to add to the running CRC</returns>
        private static uint CalculateValue(uint crc, int data)
        {
            crc = _crcTable[(crc ^ data) & 0xFF] ^ (crc >> 8);
            return (crc & 0xFFFF);
        }
    }
}
