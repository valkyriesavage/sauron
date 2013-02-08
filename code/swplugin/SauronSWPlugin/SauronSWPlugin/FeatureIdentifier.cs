using System;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System.Runtime.InteropServices;

namespace SauronSWPlugin
{
    public class FeatureIdentifier
    {
        public Feature feature;
        public Component2 component;

        public FeatureIdentifier()
        {
        }
    }
}
