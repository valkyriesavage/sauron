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

using Bespoke.Common.Osc;
using Transmitter;

namespace SauronSWPlugin
{
    [ComVisible(true)]
    [ProgId(SWTASKPANE_PROGID)]
    public partial class SWTaskpaneHost : UserControl
    {
        public const string SWTASKPANE_PROGID = "Sauron.SWTaskPane_SwPlugin";
        public const string BASE_SW_FOLDER = "C:\\Users\\Valkyrie\\projects\\sauron\\solidworks";
        public SldWorks swApp;
        public ModelDoc2 swDoc = null;
        public AssemblyDoc swAssembly = null;
        public SelectionMgr swSelectionMgr = null;
        public ConfigurationManager swConfMgr = null;
        public FeatureManager swFeatureMgr = null;
        public Configuration swConf = null;
        public ISketchManager swSketchMgr = null;
        public Measure measure = null;
        public MathUtility mathUtils = null;

        public bool testing = false;

        private Component2 mainBody = null;
        private String[] ourComponents = { "button-4post", "dial", "joystick-all-pieces", "slider", "scroll-wheel", "dpad" };

        private static readonly int Port = 5001;
        IPEndPoint sourceEndPoint;
        OscServer server;

        private Camera camera = null;

        private List<Component2> allSolidComponents = null;
        private List<ComponentIdentifier> componentsForSensing = new List<ComponentIdentifier>();

        public SWTaskpaneHost()
        {
            InitializeComponent();
            initOSC();
        }

        private void initOSC()
        {
            sourceEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            server = new OscServer(TransportType.Udp, IPAddress.Loopback, Port + 1);
            server.FilterRegisteredMethods = false;
            server.MessageReceived += new EventHandler<OscMessageReceivedEventArgs>(receivedMessage);
            server.ConsumeParsingExceptions = false;
            server.Start();
        }

        private void sendMessage(String address, String data)
        {
            if (data == null)
            {
                return;
            }

            OscBundle bundle = new OscBundle(sourceEndPoint);

            OscMessage message = new OscMessage(sourceEndPoint, address);
            message.Append(data);
            bundle.Append(message);

            bundle.Send();
        }

        private void receivedMessage(object sender, OscMessageReceivedEventArgs e)
        {
            OscMessage message = e.Message;
            String address = message.Address;
            float data = float.Parse((string)message.Data[0], System.Globalization.CultureInfo.InvariantCulture);

            foreach (ComponentIdentifier ci in componentsForSensing)
            {
                if (ci.isAddressedAs(address))
                {
                    // TODO
                    ci.component.Select(false);
                }
            }
        }

        private void getModelDoc()
        {
            swDoc = ((ModelDoc2)(swApp.ActiveDoc));
            if (swDoc.GetType() == (int)swDocumentTypes_e.swDocASSEMBLY)
            {
                swAssembly = ((AssemblyDoc)(swDoc));
            }
            swSelectionMgr = ((SelectionMgr)(swDoc.SelectionManager));
            swConfMgr = ((ConfigurationManager)swDoc.ConfigurationManager);
            swConf = ((Configuration)swConfMgr.ActiveConfiguration);
            swSketchMgr = ((ISketchManager)swDoc.SketchManager);
            measure = swDoc.Extension.CreateMeasure();
            measure.ArcOption = 1; // minimum distance
            mathUtils = swApp.IGetMathUtility();
            swFeatureMgr = ((FeatureManager)swDoc.FeatureManager);

            mainBody = findComponent(swConf.GetRootComponent3(true), "body");

            allSolidComponents = allComponents();

            swAssembly.EditAssembly();
        }

        private void getFOV()
        {
            Component2 fieldOfView = findComponent(swConf.GetRootComponent3(true), Camera.FOV);
            
            // figure out the rays' source, i.e. the centre of the camera sensor
            // see https://forum.solidworks.com/thread/60491
            swDoc.Extension.SelectByID2("centre of vision@" + fieldOfView.GetSelectByIDString(),
                                        "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
            IFeature centreOfVisionFeature = swSelectionMgr.GetSelectedObject6(1, -1) as IFeature;
            IRefPoint centreOfVisionPoint = centreOfVisionFeature.GetSpecificFeature2() as IRefPoint;
            IMathPoint centreOfVision = centreOfVisionPoint.GetRefPoint() as IMathPoint;
            swDoc.ClearSelection2(true);

            // now figure out the direction the camera points
            // this is translated from http://help.solidworks.com/2012/English/api/sldworksapi/get_the_normal_and_origin_of_a_reference_plane_using_its_transform_example_vb.htm
            swDoc.Extension.SelectByID2("distance of measurement@" + fieldOfView.GetSelectByIDString(),
                                        "PLANE", 0, 0, 0, false, 0, null, 0);
            IFeature cameraNormalFeature = swSelectionMgr.GetSelectedObject6(1, -1);
            RefPlane cameraNormalPlane = cameraNormalFeature.GetSpecificFeature2();
            MathTransform cameraTransform = cameraNormalPlane.Transform;
            //TODO : test me!
            //MathTransform reversedCameraTransform = mathUtils.ICreateTransform(-1 * cameraTransform.ArrayData);
            double[] canonicalRay = { 0, 0, 1 };
            MathVector normalVector = mathUtils.CreateVector(canonicalRay);
            MathVector cameraDirection = normalVector.MultiplyTransform(cameraTransform);
            swDoc.ClearSelection2(true);

            camera = new Camera(fieldOfView, mathUtils.CreatePoint((double[])centreOfVision.ArrayData), cameraDirection);

            double[] cameraDirData = (double[])cameraDirection.ArrayData;
            double[] cameraOriginData = (double[])centreOfVision.ArrayData;
            swDoc.Insert3DSketch2(true);
            swDoc.CreateLine2(cameraOriginData[0], cameraOriginData[1], cameraOriginData[2], cameraDirData[0], cameraDirData[1], cameraDirData[2]);
            swDoc.Insert3DSketch2(true);
            Feature sketch = swSelectionMgr.GetSelectedObject6(1, 0);
            sketch.Name = "camera ray";
            swDoc.ClearSelection2(true);

            /*
            swApp.SendMsgToUser2("camera is at " + cameraOriginData[0] + "," + cameraOriginData[1] + "," + cameraOriginData[2] + " with direction " + cameraDirData[0] + "," + cameraDirData[1] + "," + cameraDirData[2], 1, 1); 
            */       
        }

        private double inchesToMeters(double inches)
        {
            return inches * 2.54 / 100;
        }

        private double metersToInches(double meters)
        {
            return meters * 100 / 2.54;
        }

        private string randomString()
        {
            return randomString(15);
        }

        private string randomString(int strlen)
        {
            string ret = "";
            Random rand = new Random();
            for (int i = 0; i < strlen; i++)
            {
                int r = rand.Next(25) + 65;
                ret = ret + (char)r;
            }

            return ret;
        }

        private string stripIdentifier(string partname)
        {
            Regex endsWithId = new Regex(@"(\d+)$",
                                         RegexOptions.Compiled |
                                         RegexOptions.CultureInvariant);
            Match match = endsWithId.Match(partname);
            if (match.Success)
            {
                partname = partname.Remove(partname.LastIndexOf('-'));
            }
            return partname;
        }

        private void updateExtrudeDepth(IFeature feat, double depth)
        {
            IComponent2 comp = (feat as IEntity).IGetComponent2();

            IDisplayDimension dispDim = (IDisplayDimension)feat.GetFirstDisplayDimension();
            IDimension dim = dispDim.IGetDimension();

            dim.SetSystemValue3(depth,
                (int)swSetValueInConfiguration_e.swSetValue_InSpecificConfigurations,
                new string[] { comp.ReferencedConfiguration });

            swFeatureMgr.EditRollback((int)swMoveRollbackBarTo_e.swMoveRollbackBarToEnd, feat.Name);

            swAssembly.EditRebuild();
            swDoc.ForceRebuild3(false);
        }

        private double distanceBetween(Component2 component, Component2 otherComponent)
        {
            double distance;
            double pt1, pt2;
            // does not work ((Entity)component).GetDistance(otherComponent, bmin, null, out (object)pt1, out (object)pt2, out distance);
            distance = swDoc.IClosestDistance(component, otherComponent, out pt1, out pt2);
            component.Select(false);
            otherComponent.Select(true);
            measure.Calculate(null);
            return measure.Distance;
        }

        private bool lengthenFlag(Component2 swComponent, IConfiguration configToEdit)
        {
            double threshold = inchesToMeters(.05);
            IFeature extrusionFeature = swComponent.FeatureByName("flag");
            
            if (extrusionFeature == null)
            {
                return false;
            }

            ExtrudeFeatureData2 extrusion = extrusionFeature.GetDefinition();
            extrusion.AccessSelections(swAssembly, swComponent);

            // try getting the distance between them via the measurement API and setting the extrusion to that length
            double originalDepth = extrusion.GetDepth(true);
            double defaultDepth = inchesToMeters(.15);

            double distance = distanceBetween(swComponent, camera.fieldOfView);
            updateExtrudeDepth(extrusionFeature, distance + originalDepth);

            if (!(distanceBetween(swComponent, camera.fieldOfView) < threshold))
            {
                updateExtrudeDepth(extrusionFeature, defaultDepth);
                return false;
            }

            // we are intersecting successfully, but we need to make sure to check we aren't intersecting anything else...
            foreach (Component2 otherComponent in allSolidComponents)
            {
                if (otherComponent.Equals(swComponent) || otherComponent.Equals(camera.fieldOfView) || otherComponent.Equals(mainBody))
                {
                    continue;
                }
                if (distanceBetween(swComponent, otherComponent) < threshold)
                {
                    updateExtrudeDepth(extrusionFeature, defaultDepth);
                    swApp.SendMsgToUser2("backed off " + swComponent.Name + " : we intersected with " + otherComponent.Name, 1, 1);
                    return false;
                }
            }
            
            return true;
        }

        private void createMirrorExtrusion(Component2 swComponent, Face2 intersectedFace, MathPoint point)
        {
            double mirrorWidth = inchesToMeters(1);
            double[] pointLocation = (double[]) point.ArrayData;

            MathVector normal = mathUtils.CreateVector(new double[] { 0, 0, 1 });
            swComponent.Select(false);
            swSelectionMgr.SetSelectionPoint2(0, -1, pointLocation[0], pointLocation[1], pointLocation[2]);
            swSketchMgr.InsertSketch(true);
            double[] projectedPointDescription = (double[]) intersectedFace.GetClosestPointOn(pointLocation[0], pointLocation[1], pointLocation[2]);
            swSketchMgr.CreatePoint(projectedPointDescription[0], projectedPointDescription[1], projectedPointDescription[2]);
            ISketchSegment[] mirrorRect = swSketchMgr.CreateCenterRectangle(pointLocation[0], pointLocation[1], pointLocation[2],
                                                                            pointLocation[0] + mirrorWidth, pointLocation[1] + mirrorWidth, pointLocation[2]);
            
            swFeatureMgr.FeatureExtrusion2(true, false, false,
                                           0, 0, 0.00254, 0.00254,
                                           false, false, false, false,
                                           0, 0,
                                           false, false, false, false, true, true, true,
                                           0, 0,
                                           false);

            swDoc.ClearSelection2(true);
        }

        private bool intersectsComponentSomewhere(Component2 swComponent, MathPoint rayPoint, MathVector rayDir, out MathPoint intersectionPoint, out Face2 intersectedFace)
        {
            /**  
             * Translated partially from https://forum.solidworks.com/message/269272 
             * 
             * TODO : only look to see if we are intersecting the FLAG THING.  we don't care about the rest of the object!
             */

            intersectionPoint = null;
            intersectedFace = null;

            object vBodyInfo;
            int[] bodiesInfo = null;

            object[] vBodies = swComponent.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
            bodiesInfo = (int[])vBodyInfo;
            int i = 0;

            foreach (object vBody in vBodies)
            {
                Body2 body = (Body2)vBody;
                intersectedFace = body.GetFirstFace();

                if (!body.Name.Contains("flag"))
                {
                    continue;
                }

                while (intersectedFace != null && intersectionPoint == null)
                {
                    intersectionPoint = intersectedFace.GetProjectedPointOn(rayPoint, rayDir);
                    if (intersectionPoint != null && intersectionPoint.ArrayData != null)
                    {
                        double[] intersectCoords = (double[]) intersectionPoint.ArrayData;
                        swApp.SendMsgToUser2("HUZFUCKINGZAH, we intersected with " + swComponent.Name + " at " + intersectCoords[0] + "," + intersectCoords[1] + "," + intersectCoords[2], 1, 1);
                    }
                    intersectedFace = intersectedFace.IGetNextFace();
                }

                if (intersectionPoint != null)
                {
                    createMirrorExtrusion(swComponent, intersectedFace, intersectionPoint);
                    break;
                }
                i++;
            }

            return (intersectionPoint != null);
        }

        private bool placeMirror(Component2 swComponent, IConfiguration configToEdit)
        {
            // intutively, what we want to do:
            // look to see if any camera rays hit the object as-is, then no worries
            // if not, look to see if any camera rays reflecting off the main body hit the object
            //   if yes, put a mirror in that spot
            //   if no, we fail and cry

            object vBodyInfo;
            object[] vBodies;
            int[] bodiesInfo = null;
            
            MathPoint intersectionPoint = null;
            Face2 intersectedFace = null;

            foreach (MathVector cameraRayVector in camera.rayVectors())
            {
                if (intersectsComponentSomewhere(swComponent, camera.centreOfVision, cameraRayVector, out intersectionPoint, out intersectedFace))
                {
                    break;
                }
            }

            if (intersectionPoint == null)
            {
                swApp.SendMsgToUser2("moving on to trying reflection", 1, 1);
                MathPoint reflectionPoint = null;
                MathVector reflectionVector = null;
                // then we need to reflect to see this component
                vBodies = mainBody.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
                bodiesInfo = (int[])vBodyInfo;

                bool intersecting = false;

                foreach (MathVector cameraRayVector in camera.rayVectors())
                {
                    foreach (object vBody in vBodies)
                    {
                        Body2 body = (Body2)vBody;
                        Face2 face = body.GetFirstFace();

                        while (face != null && !intersecting)
                        {
                            Surface surface = face.IGetSurface();
                            if (surface == null)
                            {
                                swApp.SendMsgToUser2("our surface is null?", 1, 1);
                            }

                            reflectionPoint = surface.GetProjectedPointOn(camera.centreOfVision, cameraRayVector);

                            if (reflectionPoint == null)
                            {
                                continue;
                            }

                            double[] ridiculousReturn = surface.EvaluateAtPoint(reflectionPoint.ArrayData[0],
                                                                                reflectionPoint.ArrayData[1],
                                                                                reflectionPoint.ArrayData[2]);
                            double[] surfaceNormal = { ridiculousReturn[0], ridiculousReturn[1], ridiculousReturn[2] };
                            double[] xyz = (double[])cameraRayVector.ArrayData;
                            double[] reflectionDir = camera.calculateReflectionDir(xyz);
                            reflectionVector = mathUtils.CreateVector(reflectionDir);

                            bool intersected = intersectsComponentSomewhere(swComponent, reflectionPoint, reflectionVector, out intersectionPoint, out intersectedFace);

                            face = face.IGetNextFace();
                        }

                        if (intersecting)
                        {
                            // put a mirror on the face at reflectionPoint
                            createMirrorExtrusion(swComponent, intersectedFace, reflectionPoint);
                            break;
                        }
                    }
                    if (intersecting)
                    {
                        break;
                    }
                }
                
            }

            return intersectionPoint != null;
        }

        private Component2 findComponent(Component2 swComp, String searchFor)
        {
            object[] vChildComp;
            Component2 swChildComp;
            int i;

            vChildComp = (object[])swComp.GetChildren();
            for (i = 0; i < vChildComp.Length; i++)
            {
                swChildComp = (Component2)vChildComp[i];
                if (swChildComp.Name2.Contains(searchFor))
                {
                    return swChildComp;
                }
            }
            return null;
        }

        private FeatureIdentifier findFeature(Component2 swComp, String componentName, String featureName)
        {
            object[] vChildComp;
            Component2 swChildComp;
            int i;

            vChildComp = (object[])swComp.GetChildren();
            for (i = 0; i < vChildComp.Length; i++)
            {
                swChildComp = (Component2)vChildComp[i];
                if (swChildComp.Name2.Equals(componentName))
                {
                    Feature f = swChildComp.FeatureByName(featureName);
                    if (f != null)
                    {
                        FeatureIdentifier fi = new FeatureIdentifier();
                        fi.component = swChildComp;
                        fi.feature = f;
                        return fi;
                    }
                }
            }
            return null;
        }

        private void findModifiable(Component2 swComp, List<FeatureIdentifier> found)
        {
            object[] vChildComp;
            Component2 swChildComp;
            int i;

            vChildComp = (object[])swComp.GetChildren();
            for (i = 0; i < vChildComp.Length; i++)
            {
                swChildComp = (Component2)vChildComp[i];
                foreach (String compName in ourComponents)
                {
                    if (swChildComp.Name2.Contains(compName))
                    {
                        Feature flag = swChildComp.FeatureByName("flag");
                        if (flag != null)
                        {
                            FeatureIdentifier fi = new FeatureIdentifier();
                            fi.component = swChildComp;
                            fi.feature = flag;
                            found.Add(fi);
                        }
                    }
                }
            }
        }

        private bool isBody(Component2 swComponent) {
            object bodiesInfo = null;
            swComponent.GetBodies3((int)swBodyType_e.swSolidBody, out bodiesInfo);
            return bodiesInfo != null && ((int[])bodiesInfo).Length > 0;
        }

        private List<Component2> allComponents()
        {
            List<Component2> allComponents = new List<Component2>();
            Component2 swComp = swConf.GetRootComponent3(true);
            object[] vChildComp;
            Component2 swChildComp;
            int i;

            vChildComp = (object[])swComp.GetChildren();
            for (i = 0; i < vChildComp.Length; i++)
            {
                swChildComp = (Component2)vChildComp[i];
                if (isBody(swChildComp))
                {
                    allComponents.Add(swChildComp);
                }
            }

            return allComponents;
        }

        private IConfiguration createNewConfiguration(Component2 swComponent)
        {
            string componentName = stripIdentifier(swComponent.Name);
            string newConfigName = "SAURON-" + randomString();

            IModelDoc2 component = swComponent.GetModelDoc2();
            IConfiguration newConfig = component.AddConfiguration3(newConfigName,
                                                                   "automatically generated configuration",
                                                                   "",
                                                                   0);
            swComponent.ReferencedConfiguration = newConfig.Name;
            swDoc.EditRebuild3();

            return newConfig;
        }

        private void processFeatures_Click(object sender, EventArgs e)
        {
            // do the basic setup for the function
            getModelDoc();
            getFOV();
            if (camera.fieldOfView == null)
            {
                swApp.SendMsgToUser2("no camera detected!", 1, 1);
                return;
            }

            // traverse the tree, searching for components with our names; these are ours
            List<FeatureIdentifier> found = new List<FeatureIdentifier>();
            findModifiable(swConf.GetRootComponent3(true), found);

            foreach (FeatureIdentifier f in found)
            {
                IConfiguration newConfig = createNewConfiguration(f.component);
                componentsForSensing.Add(new ComponentIdentifier(f.component));
                bool success = lengthenFlag(f.component, newConfig);
                if (!success)
                {
                    // try placing a mirror for it
                    success = placeMirror(f.component, newConfig);
                    if (!success)
                    {
                        // cry
                        swApp.SendMsgToUser2("we can't seem to see component " + f.component.Name + " you will have to move it", 1, 1);
                    }
                }
                // TODO : TAKE ME OUT!  I AM FOR TESTING PURPOSES ONLY!
                //break;
            }

            // cast rays from the camera and see what we get!  hopefully some bounding boxes.  :)

            swAssembly.EditAssembly();
        }

        private void stuff()
        {
            // a place for me to copy/paste from macros and not hurt anything
            getModelDoc();


        }

        private void test_mode_Click(object sender, EventArgs e)
        {
            getModelDoc();
            String exit_test = "exit test mode";
            String enter_test = "enter test mode";
            if (testing)
            {
                testing = false;
                test_mode.Text = enter_test;
                sendMessage("/test/", "end");
            }
            else
            {
                testing = true;
                test_mode.Text = exit_test;
                sendMessage("/test/", "start");
            }
        }

        private void register_Click(object sender, EventArgs e)
        {
            getModelDoc();
            foreach (ComponentIdentifier c in componentsForSensing)
            {
                // we want to process this appropriately: send it to Colin
                sendMessage(c.OSC_string, "register");

                // now ask the user to register it
                swApp.SendMsgToUser2("please actuate " + c.component.Name + ", then click OK",
                                     (int)swMessageBoxIcon_e.swMbQuestion,
                                     (int)swMessageBoxBtn_e.swMbOk);

                sendMessage(c.OSC_string, "done");
            }
        }

        private void insert_camera_Click(object sender, EventArgs e)
        {
            getModelDoc();

            string FOV_file = "C:\\Users\\Valkyrie\\projects\\sauron\\solidworks\\point-grey-fov.SLDPRT";
            int loadErrs = 0;
            int loadWarns = 0;
            swApp.OpenDoc6(FOV_file,
                           (int)swDocumentTypes_e.swDocPART,
                           (int)swOpenDocOptions_e.swOpenDocOptions_Silent,
                           "",
                           ref loadErrs, ref loadWarns);
            if (loadErrs > 0)
            {
                swApp.SendMsgToUser2("something went wrong, try again",
                                     (int)swMessageBoxIcon_e.swMbWarning,
                                     (int)swMessageBoxBtn_e.swMbOk);
            }
            double x = 0;
            double y = 0;
            double z = 0;
            swAssembly.AddComponent(FOV_file, x, y, z);
            swApp.CloseDoc(FOV_file);
            swAssembly.ForceRebuild();

            getFOV();
            camera.fieldOfView.Select2(false, 1 << 0);
            swAssembly.SetComponentTransparent(true);
            camera.fieldOfView.DeSelect();
            swAssembly.EditAssembly();
        }

        private void testPart_Click(object sender, EventArgs e)
        {
            getModelDoc();
            getFOV();
            foreach (Component2 comp in allSolidComponents)
            {
                swApp.SendMsgToUser2(comp.Name, 1, 1);
            }
        }
    }
}
