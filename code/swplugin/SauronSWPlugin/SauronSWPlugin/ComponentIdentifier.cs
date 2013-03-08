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
        static int dpadCount = 0;

        public Component2 component;
        public string OSC_string;

        public ComponentIdentifier(Component2 component)
        {
            this.component = component;

            OSC_string = "";
            if (component.Name2.Contains("button"))
            {
                this.OSC_string = "/button/" + buttonCount;
                buttonCount++;
            }
            else if (component.Name2.Contains("slider"))
            {
                this.OSC_string = "/slider/" + sliderCount;
                sliderCount++;
            }
            else if (component.Name2.Contains("dial"))
            {
                this.OSC_string = "/dial/" + dialCount;
                dialCount++;
            }
            else if (component.Name2.Contains("joystick"))
            {
                this.OSC_string = "/joystick/" + joystickCount;
                joystickCount++;
            }
            else if (component.Name2.Contains("scroll-wheel"))
            {
                this.OSC_string = "/scrollwheel/" + scrollwheelCount;
                scrollwheelCount++;
            }
            else if (component.Name2.Contains("dpad"))
            {
                this.OSC_string = "/dpad/" + dpadCount;
                dpadCount++;
            }
        }

        override
        public string ToString()
        {
            return this.OSC_string;
        }

        public bool equals(Object o)
        {
            if (o.GetType() == this.GetType())
            {
                ComponentIdentifier ci = (ComponentIdentifier)o;
                if (ci.ToString().Equals(this.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        public bool isAddressedAs(String address)
        {
            return this.ToString().CompareTo(address) == 0;
        }
    }
}
