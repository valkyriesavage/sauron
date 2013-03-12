using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System.Runtime.InteropServices;

namespace SauronSWPlugin
{
    class ReflectionPoint
    {
        public double[] xyz;
        public double[] nxnynz;

        public MathPoint location;
        public MathVector normal;

        public ReflectionPoint(MathUtility mathUtils, double[] xyz, double[] nxnynz)
        {
            this.xyz = xyz;
            this.nxnynz = nxnynz;

            location = mathUtils.CreatePoint(xyz);
            normal = mathUtils.CreateVector(nxnynz);
        }
    }
}
