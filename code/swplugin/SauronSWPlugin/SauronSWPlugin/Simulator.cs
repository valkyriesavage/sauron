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
    class Simulator
    {
        public static SldWorks swApp;
        public static ModelDoc2 swDoc = null;
        public static AssemblyDoc swAssembly = null;
        public static Component2 mainBody = null;
        public static FeatureManager swFeatureMgr = null;
        public static List<ComponentIdentifier> ourComponents = null;

        private static double intersectionThreshold = .02;

        public static double getMateValue(Component2 component)
        {
            Feature mate = swAssembly.FeatureByName(component.Name2 + "-distance");

            if (mate == null)
            {
                return -1;
            }

            IDisplayDimension dispDim = (IDisplayDimension)mate.GetFirstDisplayDimension();
            IDimension dim = dispDim.IGetDimension();

            double[] someRet = dim.GetSystemValue3((int)swSetValueInConfiguration_e.swSetValue_InSpecificConfigurations,
                new string[] { component.ReferencedConfiguration });

            return someRet[0];
        }

        public static void moveToPosition(Component2 component, int position, double originalPosition)
        {
            /*
             * Buttons have 1 and 0 positions
             * Sliders move along the track from 1 to 0 in 4 steps
             * Dials move in a circle from 0 to 2*pi in 4 steps
             */

            // for now, we will just try to move around the buttons, sliders, and dials, because that is least impossible!
            if (position == -1)
            {
                return;
            }

            // see discussions at
            // https://forum.solidworks.com/message/341157#341157#341157

            double newDistance = 0;

            if (component.Name2.StartsWith("button"))
            {
                if (position == 1)
                {
                    newDistance = Utilities.inchesToMeters(.02);
                }
                else
                {
                    newDistance = originalPosition;
                }
            }
            else if (component.Name2.StartsWith("slider"))
            {
                newDistance = position / 4.0 * originalPosition;
            }
            else if (component.Name2.StartsWith("dial"))
            {
                newDistance = position * Math.PI / 2;
            }

            if (position == 0)
            {
                newDistance = originalPosition;
            }

            Feature mate = swAssembly.FeatureByName(component.Name2 + "-distance");

            if (mate == null)
            {
                return;
            }

            IDisplayDimension dispDim = (IDisplayDimension)mate.GetFirstDisplayDimension();
            IDimension dim = dispDim.IGetDimension();

            dim.SetSystemValue3(newDistance,
                (int)swSetValueInConfiguration_e.swSetValue_InSpecificConfigurations,
                new string[] { component.ReferencedConfiguration });

            swAssembly.EditRebuild();
            swDoc.ForceRebuild3(false);

            swAssembly.EditAssembly();
        }

        public static bool somethingIntersects(Component2 component)
        {
            foreach (ComponentIdentifier ci in ourComponents)
            {
                if (ci.component.Name2.Equals(component.Name2))
                {
                    continue;
                }
                if (Utilities.distanceBetween(component, ci.component) < intersectionThreshold)
                {
                    return true;
                }
            }
            return Utilities.distanceBetween(component, mainBody) < intersectionThreshold;
        }

        public static bool cameraIntersects(Component2 component)
        {
            return Utilities.distanceBetween(component, component) < intersectionThreshold;
        }

        private static bool isSimulationComplete(string componentName, int positionsTested)
        {
            return ((componentName.StartsWith("button") && positionsTested == 2)
                    || (componentName.StartsWith("slider") && positionsTested == 4)
                    || (componentName.StartsWith("dial") && positionsTested == 4));

        }

        public static bool extendFlagOverRangeOfMotion(Component2 component)
        {
            // what we want to do is lengthen the flag (if we can), then move it through all positions.
            // if we get to a position where we can't see it anymore, we want to lengthen the flag again and
            // restart the simulation.

            Extrusion e = new Extrusion(component, RayTracer.camera, swApp, swDoc, swAssembly, swFeatureMgr);
            if (e.isValid())
            {
                bool completeSimulation = false;
                int positionsTested = 0;

                double originalMateValue = getMateValue(component);

                while (!completeSimulation && !somethingIntersects(component))
                {
                    // we can extrude this thing
                    if (!cameraIntersects(component))
                    {
                        e.extendBy(e.distanceFromFlagToCamera());
                        completeSimulation = false;
                        positionsTested = 0;
                        
                        if (somethingIntersects(component))
                        {
                            break;
                        }
                    }
                    positionsTested += 1;
                    
                    // move to a new position
                    moveToPosition(component, positionsTested, originalMateValue);

                    completeSimulation = isSimulationComplete(component.Name2, positionsTested);
                }

                moveToPosition(component, 0, originalMateValue);

                if (!completeSimulation)
                {
                    e.restoreToDefaultDepth();
                }
                else
                {
                    return true;
                }
            }
            else
            {
                // we have something like a scroll wheel or trackball
                // these things don't need to be simulated
                return RayTracer.rawRaysSeeComponentInCurrentConfig(component);
            }

            return false;
        }

        public static bool reflectRaysOverRangeOfMotion(Component2 component)
        {
            // what we want to do is reflect rays while moving through all positions.
            // if we get to a position where we can't see it anymore, we want to call it a day
            // TODO : we could probably try lengthening the flag here and checking

            Extrusion e = new Extrusion(component, RayTracer.camera, swApp, swDoc, swAssembly, swFeatureMgr);
            if (e.isValid())
            {
                bool completeSimulation = false;
                int positionsTested = 0;

                double originalMateValue = getMateValue(component);

                while (!completeSimulation && (RayTracer.reflectedRayCanSeeComponent(component) != null))
                {
                    positionsTested += 1;

                    // move to a new position
                    moveToPosition(component, positionsTested, originalMateValue);

                    completeSimulation = isSimulationComplete(component.Name2, positionsTested);
                }

                moveToPosition(component, 0, originalMateValue);
                
                if (completeSimulation) {
                    return true;
                }
            }
            else
            {
                // we have something like a scroll wheel or trackball
                // these things don't need to be simulated
                return (RayTracer.reflectedRayCanSeeComponent(component) != null);
            }

            return false;
        }
    }
}
