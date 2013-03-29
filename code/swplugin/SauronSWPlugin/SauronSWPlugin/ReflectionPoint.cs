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

        public Component2 component;

        public ReflectionPoint(MathUtility mathUtils, double[] xyz, double[] nxnynz, Component2 component)
        {
            this.xyz = xyz;
            this.nxnynz = nxnynz;
            this.component = component;

            location = mathUtils.CreatePoint(xyz);
            normal = mathUtils.CreateVector(nxnynz);
        }
    }
}
