using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System.Runtime.InteropServices;

namespace SauronSWPlugin
{
    class InstructionsGenerator
    {
        public static SldWorks swApp;
        public static ModelDoc2 swDoc = null;
        public static AssemblyDoc swAssembly = null;
        public static SelectionMgr swSelectionMgr = null;

        static int stepNumber = 0;

        public static void createInstructions(List<ComponentIdentifier> componentsToAssemble, List<IFeature> mirrorExtrusions, string STLLocation) {
            stepNumber = 0;

            string fileLocation = tempFile(".html");
            string webAddress = "file:///" + fileLocation.Replace("\\", "/");
            string html = HTMLHeader();

            html += tableHeader();

            html += step("print <a href='file:///" + STLLocation.Replace("\\", "/") + "'>" + STLLocation + "</a> on your favourite 3D printer", "C:\\Users\\Valkyrie\\projects\\sauron\\etc\\webImages\\uprint.jpg");

            foreach (ComponentIdentifier ci in componentsToAssemble)
            {
                html += componentAssemblyInstruction(ci);
            }

            foreach (IFeature mirror in mirrorExtrusions)
            {
                html += mirrorAssemblyInstruction(mirror);
            }

            html += step("screw the camera rig onto the body", "");

            html += tableFooter();

            html += HTMLFooter();

            System.IO.File.WriteAllText(@fileLocation, html);

            System.Diagnostics.Process.Start(webAddress);
        }

        private static string tempFile(string extension)
        {
            return System.IO.Path.GetTempFileName().Replace(".tmp", extension);
        }

        private static string HTMLHeader()
        {
            return "<html>\n<head><title>How to Build Your Computer Vision-ized Device</title></head>\n<body>";
        }

        private static string tableHeader()
        {
            return "<table width=100%>\n";
        }

        private static string step(string instruction, string imageLocation)
        {
            string html = "<tr bgcolor='#FFFFFF'><td width=50% style='vertical-align:top;padding:5px'>";

            if (stepNumber % 2 == 1)
            {
                // differentiate the alternating steps with pretty colors!
                html = html.Replace("#FFFFFF", "#E0E0E0");
            }

            html = html + "<h1>" + stepNumber + "</h1>";

            html = html + instruction;

            html = html + "</td>\n<td width=50% style='vertical-align:top;padding:5px'>";

            html = html + "<img src='file:///" + imageLocation.Replace("\\", "/") + "' width=100% >";

            html = html + "</td></tr>\n";

            stepNumber++;

            return html;
        }

        private static string componentAssemblyInstruction(ComponentIdentifier ci)
        {
            // first, we select the component and take a screenshot of the model
            string screenshotLocation = tempFile(".jpg");
            int errors = 0, warnings = 0;
            ci.component.Select(false);
            swDoc.ShowNamedView2("*Dimetric", -1);
            swDoc.ViewZoomtofit2();
            swDoc.Extension.SaveAs(screenshotLocation, 0, 0, null, ref errors, ref warnings);

            string allApplicable = "";
            // TODO : add in images for examples of all of these!
            if (ci.component.Name.StartsWith("button"))
            {
                allApplicable += step("stick a circle of reflective material on the bottom of the button as in the example below<br/>", screenshotLocation);
                allApplicable += step("use tweezers to put a small spring between the button and the body", "");
            }
            if (ci.component.Name.StartsWith("slider"))
            {
                allApplicable += step("stick a rectangle of reflective material on the bottom of the slider as in the example below<br/>", screenshotLocation);
            }
            if (ci.component.Name.StartsWith("dpad"))
            {
                allApplicable += step("stick a circle of reflective material on the bottom of each of the direction pad's protrusions as in the example below<br/>", screenshotLocation);
                allApplicable += step("use tweezers to put a small spring between the direction pad and the body", "");
            }
            if (ci.component.Name.StartsWith("scroll-wheel"))
            {
                allApplicable += step("stick horizontal stripes (about 10) of reflective material on the outside of the scroll wheel as in the example below<br/>", screenshotLocation);
            }
            if (ci.component.Name.StartsWith("joystick"))
            {
                allApplicable += step("stick a rectangle of reflective material on the bottom of each of the joystick's protrusions as in the example below<br/>", screenshotLocation);
            }
            if (ci.component.Name.StartsWith("dial"))
            {
                allApplicable += step("stick a circle of reflective material on the bottom of the dial's protrusion as in the example below<br/>", screenshotLocation);
            }

            return allApplicable;
        }

        private static string mirrorAssemblyInstruction(IFeature mirror)
        {
            // first, we select the component and take a screenshot of the model
            string screenshotLocation = tempFile(".jpeg");
            int errors = 0, warnings = 0;
            mirror.Select(false);
            swDoc.ShowNamedView2("*Isometric", -1);
            swDoc.ViewZoomtofit2();
            swDoc.Extension.SaveAs(screenshotLocation, 0, 0, null, ref errors, ref warnings);

            return step("glue a craft mirror into your assembly at the highlighted location", screenshotLocation);
        }

        private static string tableFooter()
        {
            return "</table>";
        }

        private static string HTMLFooter()
        {
            return "</body>\n</html>";
        }
    }
}
