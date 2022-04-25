//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

// System
using System.IO;
using System.Text;
using System.Collections.Generic;

// NDI
using NDI.CapiSample.Data;
using NDI.CapiSample.Utility;

namespace NDI.CapiSample.Protocol.GBF
{
    /// <summary>
    /// System alert contains Events, Alerts, and Faults
    /// </summary>
    class GbfSystemAlert : GbfComponent
    {
        /// <summary>
        /// List of alerts in this packet
        /// </summary>
        public List<SystemAlert> alerts = new List<SystemAlert>();

        /// <summary>
        /// Parse all alerts from this packet.
        /// </summary>
        /// <param name="reader">Stream to read from</param>
        /// <param name="alertCount">Number of alerts in this packet.</param>
        public GbfSystemAlert(Stream reader, int alertCount)
        {
            for(int i = 0; i < alertCount; i++)
            {
                SystemAlert alert = new SystemAlert();

                alert.type = (SystemAlertCode)BytePacker.UnpackInt(reader, 1);
                BytePacker.UnpackInt(reader, 1); // Reserved
                alert.subType = BytePacker.UnpackInt(reader, 2);

                alerts.Add(alert);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            // Packet Info
            sb.AppendFormat("----GbfSystemAlert \n {0}", base.ToString());

            foreach(SystemAlert alert in alerts)
            {
                // Alert Info
                sb.AppendFormat("--Alert: conditionType={0:X2}, conditionCode={0:X4}\n", alert.type, alert.subType);
            }

            return base.ToString();
        }
    }
}
