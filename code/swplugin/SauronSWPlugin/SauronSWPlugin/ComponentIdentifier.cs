using System;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System.Runtime.InteropServices;

namespace SauronSWPlugin
{
    public class ComponentIdentifier
    {
        static int buttonCount = 0;
        static int sliderCount = 0;
        static int dialCount = 0;
        static int joystickCount = 0;
        static int scrollwheelCount = 0;

        public Component2 component;
        public string OSC_string;

        public ComponentIdentifier(Component2 component)
        {
            OSC_string = "";
            if (component.Name2.Contains("button"))
            {
                OSC_string = "/button/" + buttonCount;
                buttonCount++;
            }
            else if (component.Name2.Contains("slider"))
            {
                OSC_string = "/slider/" + sliderCount;
                sliderCount++;
            }
            else if (component.Name2.Contains("dial"))
            {
                OSC_string = "/slider/" + dialCount;
                dialCount++;
            }
            else if (component.Name2.Contains("joystick"))
            {
                OSC_string = "/slider/" + joystickCount;
                joystickCount++;
            }
            else if (component.Name2.Contains("scrollwheel"))
            {
                OSC_string = "/slider/" + scrollwheelCount;
                scrollwheelCount++;
            }
        }
    }
}
