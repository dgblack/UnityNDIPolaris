//----------------------------------------------------------------------------
//
// Copyright 2020 by Northern Digital Inc.
// All Rights Reserved
//
//----------------------------------------------------------------------------
// By using this Sample Code the licensee agrees to be bound by the
// provisions of the agreement found in the LICENSE.txt file.

using System;

namespace NDI.CapiSample.Data
{
    public class Quaternion
    {
        public double q0, qx, qy, qz;

        public Quaternion()
        {
            q0 = qx = qy = qz = Constants.BAD_FLOAT;
        }

        public Quaternion(double q0, double qx, double qy, double qz)
        {
            this.q0 = q0;
            this.qx = qx;
            this.qy = qy;
            this.qz = qz;
        }

        public override string ToString()
        {
            if (this.q0 == Constants.BAD_FLOAT)
            {
                return "MISSING,MISSING,MISSING,MISSING";
            }
            else
            {
                return String.Format("{0,10:F7},{1,10:F7},{2,10:F7},{3,10:F7}", q0, qx, qy, qz);
            }
        }
    }
}
