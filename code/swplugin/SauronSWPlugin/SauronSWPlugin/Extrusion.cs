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
    class Extrusion
    {
        SldWorks swApp;
        ModelDoc2 swDoc;
        AssemblyDoc swAssembly;
        FeatureManager swFeatureMgr;

        Feature extrusion;
        Component2 belongsTo;
        Camera camera;
        double defaultDepth;

        public Extrusion(Component2 belongsTo, Camera camera, SldWorks swApp, ModelDoc2 swDoc, AssemblyDoc swAssembly, FeatureManager swFeatureMgr)
        {
            IFeature extrusionFeature = belongsTo.FeatureByName("flag");

            if (extrusionFeature == null)
            {
                Utilities.alert("this component has no extrudeable feature");
                return;
            }

            this.belongsTo = belongsTo;
            this.extrusion = (Feature) extrusionFeature;
            this.camera = camera;
            setDefaultDepth();

            this.swApp = swApp;
            this.swDoc = swDoc;
            this.swAssembly = swAssembly;
            this.swFeatureMgr = swFeatureMgr;
        }

        public bool isValid()
        {
            return this.belongsTo != null;
        }

        private void setDefaultDepth()
        {
            string component = belongsTo.Name2;
            defaultDepth = Utilities.inchesToMeters(0.1);
        }

        public void updateExtrusionDepth(double depthInInches)
        {
            if (depthInInches < 0)
            {
                return;
            }

            double depthInMeters = Utilities.inchesToMeters(depthInInches);

            IDisplayDimension dispDim = (IDisplayDimension)extrusion.GetFirstDisplayDimension();
            IDimension dim = dispDim.IGetDimension();

            dim.SetSystemValue3(depthInMeters,
                (int)swSetValueInConfiguration_e.swSetValue_InSpecificConfigurations,
                new string[] { belongsTo.ReferencedConfiguration });

            swFeatureMgr.EditRollback((int)swMoveRollbackBarTo_e.swMoveRollbackBarToEnd, extrusion.Name);

            swAssembly.EditRebuild();
            swDoc.EditRebuild3();
            swDoc.ForceRebuild3(false);

            swAssembly.EditAssembly();
        }

        public void restoreToDefaultDepth()
        {
            updateExtrusionDepth(defaultDepth);
        }

        public void extendBy(double additionalDepthInInches)
        {
            ExtrudeFeatureData2 extrusionFeature = extrusion.GetDefinition();
            extrusionFeature.AccessSelections(swAssembly, belongsTo);

            double currentDepth = Utilities.metersToInches(extrusionFeature.GetDepth(true));

            updateExtrusionDepth(currentDepth + additionalDepthInInches);
        }

        public double distanceFromFlagToCamera()
        {
            if (Utilities.distanceBetween(belongsTo, camera.fieldOfView) < 0)
            {
                return Utilities.distanceBetween(belongsTo, camera.fieldOfView);
            }

            swFeatureMgr.EditRollback((int)swMoveRollbackBarTo_e.swMoveRollbackBarToEnd, "");
            swAssembly.EditRebuild();

            IMathPoint centreOfFlagBase = Utilities.getNamedPoint("centre of flag base", belongsTo);
            IMathPoint centreOfFlagTip = Utilities.getNamedPoint("centre of flag top", belongsTo);

            double[] dataBase = centreOfFlagBase.ArrayData;
            double[] dataTip = centreOfFlagTip.ArrayData;

            double[] flagDir = new double[] { dataTip[0] - dataBase[0],
                                              dataTip[1] - dataBase[1],
                                              dataTip[2] - dataBase[2] };

            List<IBody2> bodies = new List<IBody2>();

            object vBodyInfo;
            object[] componentBodies = (object[])camera.fieldOfView.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
            for (int i = 0; i < componentBodies.Length; i++)
            {
                IBody2 tempBody = ((Body2)componentBodies[i]).ICopy();
                tempBody.ApplyTransform(camera.fieldOfView.Transform2);
                bodies.Add(tempBody);
            }

            int numIntersectionsFound = (int)swDoc.RayIntersections((object)bodies.ToArray(),
                                                                    (object)dataTip,
                                                                    (object)flagDir,
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS | swRayPtsOpts_e.swRayPtsOptsENTRY_EXIT),
                                                                    (double).0000001,
                                                                    (double).0000001);

            if (numIntersectionsFound == 0)
            {
                return 0;
            }

            double[] horrifyingReturn = (double[])swDoc.GetRayIntersectionsPoints();

            int lengthOfOneReturn = 9;
            double[] intersectionPoint = new double[3];

            for (int i = 0; i < numIntersectionsFound; i++)
            {
                byte intersectionType = (byte)horrifyingReturn[i * lengthOfOneReturn + 2];

                if ((intersectionType & (byte)swRayPtsResults_e.swRayPtsResultsENTER) == 0)
                {
                    // we need it to be just entry rays
                    continue;
                }

                double x = horrifyingReturn[i * lengthOfOneReturn + 3];
                double y = horrifyingReturn[i * lengthOfOneReturn + 4];
                double z = horrifyingReturn[i * lengthOfOneReturn + 5];

                // check to see if the distance along the ray is shorter?  we want the shortest distance!

                intersectionPoint[0] = x;
                intersectionPoint[1] = y;
                intersectionPoint[2] = z;
                break;
            }

            return Utilities.distanceFormula(intersectionPoint, dataTip);
        }
    }
}
