﻿using System;
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

        public SldWorks swApp;
        public ModelDoc2 swDoc;
        public AssemblyDoc swAssembly;
        public SelectionMgr swSelectionMgr;
        public MathUtility mathUtils;

        private static String CAMERA_RAY_NAME = "camera ray";

        private List<MathVector> castRayVectors = null;
        private List<MathPoint> castRayCentres = null;

        public static Camera createCamera(Component2 fieldOfView, SldWorks swApp, ModelDoc2 swDoc, AssemblyDoc swAssembly, SelectionMgr swSelectionMgr, MathUtility mathUtils)
        {
            IMathPoint centreOfVision = getCentreOfVision(fieldOfView, swDoc, swSelectionMgr, mathUtils);

            if (centreOfVision == null)
            {
                swApp.SendMsgToUser2("you need to insert the camera first!", 1, 1);
                return null;
            }

            RefPlane cameraNormalPlane = getCameraNormalPlane(fieldOfView, swDoc, swSelectionMgr);

            // note that we reverse this transform because our plane normal is pointing the wrong direction :(
            MathTransform cameraTransform = Camera.reverseTransform(cameraNormalPlane.Transform, mathUtils);
            MathVector cameraDirection = getCameraDirection(cameraTransform, mathUtils);
            drawRayForCamera(cameraDirection, centreOfVision, swDoc, swSelectionMgr);

            Camera camera = new Camera(fieldOfView, mathUtils.CreatePoint((double[])centreOfVision.ArrayData), cameraDirection);
            camera.swApp = swApp;
            camera.swDoc = swDoc;
            camera.swAssembly = swAssembly;
            camera.swSelectionMgr = swSelectionMgr;
            camera.mathUtils = mathUtils;

            return camera;
        }

        private static MathVector getCameraDirection(MathTransform cameraTransform, MathUtility mathUtils)
        {
            double[] canonicalRay = { 0, 0, 1 };
            MathVector normalVector = mathUtils.CreateVector(canonicalRay);
            MathVector cameraDirection = normalVector.MultiplyTransform(cameraTransform);
            return cameraDirection;
        }

        private static IMathPoint getNamedPoint(string name, Component2 fieldOfView, ModelDoc2 swDoc, SelectionMgr swSelectionMgr, MathUtility mathUtils)
        {
            swDoc.ClearSelection2(true);
            swDoc.Extension.SelectByID2(name + "@" + fieldOfView.GetSelectByIDString(),
                                        "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
            IFeature namedPointFeature = swSelectionMgr.GetSelectedObject6(1, -1) as IFeature;

            if (namedPointFeature == null)
            {
                return null;
            }

            Measure measure = swDoc.Extension.CreateMeasure();
            measure.Calculate(null);
            IMathPoint namedPoint = mathUtils.CreatePoint(new double[] { measure.X, measure.Y, measure.Z });

            return namedPoint;
        }

        private static IMathPoint getCentreOfVision(Component2 fieldOfView, ModelDoc2 swDoc, SelectionMgr swSelectionMgr, MathUtility mathUtils)
        {
            return getNamedPoint("centre of vision", fieldOfView, swDoc, swSelectionMgr, mathUtils);    
        }

        private static IMathPoint getDirectionReference(Component2 fieldOfView, ModelDoc2 swDoc, SelectionMgr swSelectionMgr, MathUtility mathUtils)
        {
            return getNamedPoint("direction reference", fieldOfView, swDoc, swSelectionMgr, mathUtils);    
        }

        private static RefPlane getCameraNormalPlane(Component2 fieldOfView, ModelDoc2 swDoc, SelectionMgr swSelectionMgr)
        {
            // now figure out the direction the camera points
            // this is translated from http://help.solidworks.com/2012/English/api/sldworksapi/get_the_normal_and_origin_of_a_reference_plane_using_its_transform_example_vb.htm
            swDoc.Extension.SelectByID2("distance of measurement@" + fieldOfView.GetSelectByIDString(),
                                        "PLANE", 0, 0, 0, false, 0, null, 0);
            IFeature cameraNormalFeature = swSelectionMgr.GetSelectedObject6(1, -1);
            RefPlane cameraNormalPlane = cameraNormalFeature.GetSpecificFeature2();
            return cameraNormalPlane;
        }

        private static void drawRayForCamera(MathVector cameraDirection, IMathPoint centreOfVision, ModelDoc2 swDoc, SelectionMgr swSelectionMgr)
        {
            swDoc.ClearSelection2(true);
            Camera.removeRayIfPresent(swDoc);
            double[] cameraDirData = (double[])cameraDirection.ArrayData;
            double[] cameraOriginData = (double[])centreOfVision.ArrayData;
            swDoc.Insert3DSketch2(true);
            swDoc.CreateLine2(cameraOriginData[0], cameraOriginData[1], cameraOriginData[2],
                              cameraOriginData[0] + cameraDirData[0], cameraOriginData[1] + cameraDirData[1], cameraOriginData[2] + cameraDirData[2]);
            swDoc.Insert3DSketch2(true);
            Feature sketch = swSelectionMgr.GetSelectedObject6(1, 0);
            sketch.Name = CAMERA_RAY_NAME;
            swDoc.ClearSelection2(true);
        }

        public static void removeRayIfPresent(ModelDoc2 swDoc)
        {
            swDoc.Extension.SelectByID2(CAMERA_RAY_NAME, "SKETCH", 0, 0, 0, false, 0, null, 0);
            swDoc.Extension.DeleteSelection2((int)swDeleteSelectionOptions_e.swDelete_Children +  (int)swDeleteSelectionOptions_e.swDelete_Absorbed);
        }

        public void drawRayForCamera()
        {
            centreOfVision = (MathPoint)Camera.getCentreOfVision(fieldOfView, swDoc, swSelectionMgr, mathUtils);
            Camera.drawRayForCamera(cameraDirection, centreOfVision, swDoc, swSelectionMgr);
        }

        public void removeRayIfPresent()
        {
            Camera.removeRayIfPresent(swDoc);
        }

        public Camera(Component2 fieldOfView, MathPoint centreOfVision, MathVector cameraDirection)
        {
            this.fieldOfView = fieldOfView;
            this.centreOfVision = centreOfVision;
            this.cameraDirection = cameraDirection;
        }

        public void drawInitalRay()
        {
            Camera.removeRayIfPresent(swDoc);

            centreOfVision = (MathPoint) Camera.getCentreOfVision(fieldOfView, swDoc, swSelectionMgr, mathUtils);

            if (centreOfVision == null)
            {
                swApp.SendMsgToUser2("you need to insert the camera first!", 1, 1);
            }

            IMathPoint otherReference = Camera.getDirectionReference(fieldOfView, swDoc, swSelectionMgr, mathUtils);
            
            double[] centreData = (double[])centreOfVision.ArrayData;
            double[] otherData = (double[])otherReference.ArrayData;

            double[] cameraDir = {otherData[0] - centreData[0], otherData[1] - centreData[1], otherData[2] - centreData[2]};
            cameraDirection = mathUtils.CreateVector(cameraDir);
            Camera.drawRayForCamera(cameraDirection, centreOfVision, swDoc, swSelectionMgr);

            castRayCentres = null;
            castRayVectors = null;
        }

        public MathVector getRayFromNamedPoint(string name)
        {
            IMathPoint namedPoint = Camera.getNamedPoint(name, fieldOfView, swDoc, swSelectionMgr, mathUtils);
            double[] otherData = (double[])namedPoint.ArrayData;
            double[] centreData = (double[])centreOfVision.ArrayData;

            double[] cameraDir = { otherData[0] - centreData[0], otherData[1] - centreData[1], otherData[2] - centreData[2] };
            return mathUtils.CreateVector(cameraDir);
        }

        public double[] calculateReflectionDir(double[] rayDir, double[] surfaceNormal)
        {
            double[] reflectedDir = { 0, 0, 0 };
            double dotProduct = rayDir[0] * surfaceNormal[0] + rayDir[1] * surfaceNormal[1] + rayDir[2] * surfaceNormal[2];
            for (int i = 0; i < 3; i++)
            {
                reflectedDir[i] = -((2 * dotProduct * surfaceNormal[i]) - rayDir[i]);
            }
           
            return reflectedDir;
        }

        public List<MathVector> rayVectors()
        {
            if (castRayVectors == null)
            {
                castRayVectors = new List<MathVector>();
                castRayVectors.Add(cameraDirection);

                // TODO important: direction reference {12,13,14,15} are the bounding corners of the shape!  we can just interpolate from there
                /*for (int i = 12; i < 16; i++)
                {
                    castRayVectors.Add(getRayFromNamedPoint("direction reference " + i).multiplyTransform(fieldOfView.Transform2));
                }*/
            }
            return castRayVectors;
        }

        public List<MathPoint> rayVectorSources()
        {
            rayVectors();
            if (castRayCentres == null)
            {
                castRayCentres = new List<MathPoint>();
                for (int i = 0; i < castRayVectors.Count; i++)
                {
                    castRayCentres.Add(centreOfVision);
                }
            }
            return castRayCentres;
        }

        public double[] rayVectorDirections()
        {
            rayVectors();
            double[] rayDirections = new double[castRayVectors.Count*3];
            for (int i = 0; i < castRayVectors.Count; i++)
            {
                double[] rayData = castRayVectors.ElementAt(i).ArrayData;
                rayDirections[3 * i] = rayData[0];
                rayDirections[3 * i + 1] = rayData[1];
                rayDirections[3 * i + 2] = rayData[2];
            }
            return rayDirections;
        }

        public double[] rayVectorOrigins()
        {
            rayVectorSources();
            double[] rayCentres = new double[castRayCentres.Count * 3];
            for (int i = 0; i < castRayVectors.Count; i++)
            {
                double[] centreData = castRayCentres.ElementAt(i).ArrayData;
                rayCentres[3 * i] = centreData[0];
                rayCentres[3 * i + 1] = centreData[1];
                rayCentres[3 * i + 2] = centreData[2];
            }
            return rayCentres;
        }

        public static MathTransform reverseTransform(MathTransform transform, MathUtility mathUtility)
        {
            double[] transformData = (double[])transform.ArrayData;
            for (int i = 0; i < transformData.GetLength(0); i++)
            {
                transformData[i] = -transformData[i];
            }
            return mathUtility.CreateTransform(transformData);
        }
    }
}
