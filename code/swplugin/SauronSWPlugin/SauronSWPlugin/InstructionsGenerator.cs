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

        public static void createInstructions(List<ComponentIdentifier> componentsToAssemble, List<IFeature> mirrorExtrusions) {
            stepNumber = 0;

            string fileLocation = tempFile(".html");
            string webAddress = "file:///" + fileLocation.Replace("\\", "/");
            string html = HTMLHeader();

            html += tableHeader();

            foreach (ComponentIdentifier ci in componentsToAssemble)
            {
                html += componentAssemblyInstruction(ci);
            }

            foreach (IFeature mirror in mirrorExtrusions)
            {
                html += mirrorAssemblyInstruction(mirror);
            }

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
            return "<html>";
        }

        private static string tableHeader()
        {
            return "<table>";
        }

        private static string step(string instruction, string imageLocation)
        {
            string html = "<tr bgcolor='#000000'><td width=50% style='vertical-align:top;padding:5px'>";

            if (stepNumber % 2 == 1)
            {
                // differentiate the alternating steps with pretty colors!
                html = html.Replace("#000000", "#E0E0E0");
            }

            html = html + "stuff in column 1";

            html = html + "</td><td width=50% style='vertical-align:top;padding:5px'>";

            html = html + "stuff in column 2";

            html = html + "</td></tr>\n";

            stepNumber++;

            return html;
        }

        private static string componentAssemblyInstruction(ComponentIdentifier ci)
        {
            // first, we select the component and take a screenshot of the model
            string screenshotLocation = tempFile(".jpeg");
            int errors = 0, warnings = 0;
            ci.component.Select(false);
            swDoc.ShowNamedView2("*Isometric", -1);
            swDoc.ViewZoomtofit2();
            swDoc.Extension.SaveAs(screenshotLocation, 0, 0, null, ref errors, ref warnings);

            string instruction = "";
            if (ci.component.Name.StartsWith("button"))
            {
                instruction = "stick a circle of reflective material on the bottom of the button as in the example below<br/>";
            }
            if (ci.component.Name.StartsWith("slider"))
            {
                instruction = "stick a rectangle of reflective material on the bottom of the slider as in the example below<br/>";
            }
            if (ci.component.Name.StartsWith("dpad"))
            {
                instruction = "stick a circle of reflective material on the bottom of each of the direction pad's protrusions as in the example below<br/>";
            }
            if (ci.component.Name.StartsWith("scroll-wheel"))
            {
                instruction = "stick horizontal stripes (about 10) of reflective material on the outside of the scroll wheel as in the example below<br/>";
            }
            if (ci.component.Name.StartsWith("joystick"))
            {
                instruction = "stick a rectangle of reflective material on the bottom of each of the joystick's protrusions as in the example below<br/>";
            }
            if (ci.component.Name.StartsWith("dial"))
            {
                instruction = "stick a circle of reflective material on the bottom of the dial's protrusion as in the example below<br/>";
            }

            return step(instruction, screenshotLocation);
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
            return "</html>";
        }
    }
}
