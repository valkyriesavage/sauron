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
    class Camera
    {
        public Component2 fieldOfView;
        public static String FOV = "fov";
        public MathPoint centreOfVision;
        public MathVector cameraDirection;

        public Camera(Component2 fieldOfView, MathPoint centreOfVision, MathVector cameraDirection)
        {
            this.fieldOfView = fieldOfView;
            this.centreOfVision = centreOfVision;
            this.cameraDirection = cameraDirection;
        }

        public double[] calculateReflectionDir(double[] xyz)
        {
            double[] reflectedDir = { xyz[0], xyz[1], xyz[2] };
            for (int i = 0; i < cameraDirection.ArrayData.Length; i++)
            {
                if (cameraDirection.ArrayData[i] > 0)
                {
                    reflectedDir[i] = -reflectedDir[i];
                }
            }
            return reflectedDir;
        }

        public List<MathVector> rayVectors()
        {
            List<MathVector> rayVectors = new List<MathVector>();
            rayVectors.Add(cameraDirection);
            return rayVectors;
        }
    }
}
