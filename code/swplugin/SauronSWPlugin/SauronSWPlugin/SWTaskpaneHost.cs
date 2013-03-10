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
        public bool DEBUG = true;
        
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
        private String[] ourComponentNames = { "button-4post", "dial", "joystick-all-pieces", "slider", "scroll-wheel", "dpad" };

        private static readonly int SENDPORT = 5001;
        private static readonly int RECEIVEPORT = 5002;
        IPEndPoint solidworksPlugin;
        IPEndPoint openFrameworks;
        OscServer server;

        private Camera camera = null;

        private List<Component2> allSolidComponents = null;
        private List<ComponentIdentifier> ourComponents = null;
        private List<IFeature> generatedMirrorExtrusions = null;

        public SWTaskpaneHost()
        {
            InitializeComponent();
            initOSC();
            if (!DEBUG)
            {
                processFeatures.Enabled = false;
                print.Enabled = false;
                register.Enabled = false;
                test_mode.Enabled = false;
            }
        }

        private void initOSC()
        {
            solidworksPlugin = new IPEndPoint(IPAddress.Loopback, SENDPORT);
            openFrameworks = new IPEndPoint(IPAddress.Loopback, SENDPORT);
            server = new OscServer(TransportType.Udp, IPAddress.Loopback, RECEIVEPORT);
            server.FilterRegisteredMethods = false;
            server.MessageReceived += new EventHandler<OscMessageReceivedEventArgs>(receivedMessage);
            server.ConsumeParsingExceptions = false;
            server.Start();
        }

        private void sendMessage(String address, String data)
        {
            OscBundle bundle = new OscBundle(solidworksPlugin);
            OscMessage message = new OscMessage(solidworksPlugin, address, data);
            bundle.Append(message);
            bundle.Send(openFrameworks);
        }

        private void receivedMessage(object sender, OscMessageReceivedEventArgs e)
        {
            OscMessage message = e.Message;
            string address = message.Address;
            //float data = Float.Parse(message.Data[0]);

            foreach (ComponentIdentifier ci in ourComponents)
            {
                if (ci.isAddressedAs(address))
                {
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

            allSolidComponents = getAllComponents();
            ourComponents = getAllOurComponents();
            if (generatedMirrorExtrusions == null)
            {
                generatedMirrorExtrusions = new List<IFeature>();
            }

            swAssembly.EditAssembly();

            InstructionsGenerator.swApp = swApp;
            InstructionsGenerator.swDoc = swDoc;
            InstructionsGenerator.swAssembly = swAssembly;
            InstructionsGenerator.swSelectionMgr = swSelectionMgr;
        }

        private void getFOV()
        {
            if (camera == null)
            {
                Component2 fieldOfView = findComponent(swConf.GetRootComponent3(true), Camera.FOV);
                camera = Camera.createCamera(fieldOfView, swApp, swDoc, swAssembly, swSelectionMgr, mathUtils);
            }
            camera.drawInitalRay();
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
            return measure.Distance;  // TODO measure.Projection ?
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

        private void createMirrorExtrusion(Face2 intersectedFace, MathPoint point)
        {
            double mirrorWidth = inchesToMeters(1);
            double[] pointLocation = (double[]) point.ArrayData;

            // get the normal of the face.. this will tell us how to orient the rectangle
            double[] ridiculousReturn = intersectedFace.GetSurface().EvaluateAtPoint(pointLocation[0],
                                                                                     pointLocation[1],
                                                                                     pointLocation[2]);
            double[] surfaceNormal = { ridiculousReturn[0], ridiculousReturn[1], ridiculousReturn[2] };
            
            // select the main body, because that is what we want to draw on
            mainBody.Select(false);
            int status = 0;
            swAssembly.EditPart2(true, false, ref status);
            swSelectionMgr.SetSelectionPoint2(0, -1, pointLocation[0], pointLocation[1], pointLocation[2]);
            swSketchMgr.InsertSketch(true);
            double[] projectedPointDescription = (double[]) intersectedFace.GetClosestPointOn(pointLocation[0], pointLocation[1], pointLocation[2]);
            swSketchMgr.CreatePoint(projectedPointDescription[0], projectedPointDescription[1], projectedPointDescription[2]);
            
            swSketchMgr.CreateCenterRectangle(projectedPointDescription[0], projectedPointDescription[1], projectedPointDescription[2],
                                              projectedPointDescription[0] + mirrorWidth * (1 - surfaceNormal[0]),
                                              projectedPointDescription[1] + mirrorWidth * (1 - surfaceNormal[1]),
                                              projectedPointDescription[2] + mirrorWidth * (1 - surfaceNormal[2]));
            
            swFeatureMgr.FeatureExtrusion2(true, false, false,
                                           0, 0, 0.00254, 0.00254,
                                           false, false, false, false,
                                           0, 0,
                                           false, false, false, false, true, true, true,
                                           0, 0,
                                           false);

            
            if(generatedMirrorExtrusions == null) {
                generatedMirrorExtrusions = new List<IFeature>();  
            }
            generatedMirrorExtrusions.Add(swSelectionMgr.GetSelectedObject6(1, -1) as IFeature);

            swDoc.ClearSelection2(true);

        }

        private bool intersectsComponents()
        {
            /*
             * see https://forum.solidworks.com/message/348151
             */

            List<IBody2> bodies = new List<IBody2>();

            foreach (ComponentIdentifier ci in ourComponents)
            {
                object vBodyInfo;
                object[] componentBodies = (object[])ci.component.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
                for (int i = 0; i < componentBodies.Length; i++)
                {
                    bodies.Add((Body2)componentBodies[i]);
                }
            }

            double[] rayVectorOrigins = { 0, 0, 0 };//camera.rayVectorOrigins();
            double[] rayVectorDirections = { 0, 0, 1 };//camera.rayVectorDirections();

            int numIntersectionsFound = (int)swDoc.RayIntersections((object)bodies.ToArray(),
                                                                    (object)rayVectorOrigins,
                                                                    (object)rayVectorDirections,
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS),
                                                                    (double).0000001,
                                                                    (double).0000001);

            alert("done!  numIntersectionsFound = " + numIntersectionsFound);

            double[] points = (double[])swDoc.GetRayIntersectionsPoints();

            swDoc.SketchManager.Insert3DSketch(true);
            swDoc.SketchManager.AddToDB = true;
            for (int i = 0; i < points.Length; i += 9)
            {
                double[] pt = new double[] { points[i + 3], points[i + 4], points[i + 5] };
                swDoc.SketchManager.CreatePoint(pt[0], pt[1], pt[2]);
            }
            swDoc.SketchManager.AddToDB = false;
            swDoc.SketchManager.Insert3DSketch(true);

            /*
            if (numIntersectionsFound > 0)
            {
                double[] horrifyingReturn = swDoc.GetRayIntersectionsPoints();
                int lengthOfOneReturn = 9;
                alert(horrifyingReturn + "");
                alert(horrifyingReturn.Length + "");
                for (int i = 0; i < horrifyingReturn.Length/lengthOfOneReturn; i++)
                {
                    double bodyIndex = horrifyingReturn[i * lengthOfOneReturn + 0];
                    double rayIndex = horrifyingReturn[i * lengthOfOneReturn + 1];
                    double intersectionType = horrifyingReturn[i * lengthOfOneReturn + 2];
                    double x = horrifyingReturn[i * lengthOfOneReturn + 3];
                    double y = horrifyingReturn[i * lengthOfOneReturn + 4];
                    double z = horrifyingReturn[i * lengthOfOneReturn + 5];
                    double nx = horrifyingReturn[i * lengthOfOneReturn + 6];
                    double ny = horrifyingReturn[i * lengthOfOneReturn + 7];
                    double nz = horrifyingReturn[i * lengthOfOneReturn + 8];

                    alert("our entire horrifying return " + bodyIndex + "," + rayIndex + "," + intersectionType + "," + x + "," + y + "," + z + "," + nx + "," + ny + "," + nz);
                    visualizeRay("cameraRay-" + randomString(15), camera.rayVectors().ElementAt((int)rayIndex), camera.rayVectorSources().ElementAt((int)rayIndex));
                }

                return true;
            }*/

            return false;
        }

        private bool hitMainBodyFirst(double[] rayCoordinates, int rayIndex, double[] horrifyingReturn)
        {
            return false;
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

                while (intersectedFace != null && intersectionPoint == null)
                {
                    intersectionPoint = intersectedFace.GetProjectedPointOn(rayPoint, rayDir);
                    if (intersectionPoint != null && intersectionPoint.ArrayData != null)
                    {
                        double[] intersectCoords = (double[]) intersectionPoint.ArrayData;
                        //swApp.SendMsgToUser2("we intersected with " + swComponent.Name + " at " + intersectCoords[0] + "," + intersectCoords[1] + "," + intersectCoords[2], 1, 1);
                    }
                    intersectedFace = intersectedFace.IGetNextFace();
                }

                if (intersectionPoint != null)
                {
                    createMirrorExtrusion(intersectedFace, intersectionPoint);
                    break;
                }
                i++;
            }

            return (intersectionPoint != null);
        }

        private void visualizeRay(String name, MathVector ray, IMathPoint origin)
        {
            double[] rayData = (double[])ray.ArrayData;
            double[] originData = (double[])origin.ArrayData;
            swDoc.Insert3DSketch2(true);
            swDoc.CreateLine2(originData[0], originData[1], originData[2], rayData[0], rayData[1], rayData[2]);
            swDoc.Insert3DSketch2(true);
            Feature sketch = swSelectionMgr.GetSelectedObject6(1, 0);
            sketch.Name = name;
            swDoc.ClearSelection2(true);
        }

        private MathPoint determineRayProjectionPoint(MathPoint raySource, MathVector rayVector, Face2 face)
        {
            // TODO!
            return raySource;
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

            bool intersecting = false;

            foreach (MathVector cameraRayVector in camera.rayVectors())
            {
                // let's see if it works without reflection
                intersecting = intersectsComponentSomewhere(swComponent, camera.centreOfVision, cameraRayVector, out intersectionPoint, out intersectedFace);
            }

            if (!intersecting)
            {
                // move on and try reflection
                MathPoint reflectionPoint = null;
                MathVector reflectionVector = null;

                // we want to reflect only off of the main body, not other bodies
                vBodies = mainBody.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
                bodiesInfo = (int[])vBodyInfo;

                foreach (MathVector cameraRayVector in camera.rayVectors())
                {
                    foreach (object vBody in vBodies)
                    {
                        Body2 body = (Body2)vBody;
                        Face2 face = body.GetFirstFace();

                        while (face != null && !intersecting)
                        {
                            // TODO : take out all the shit we are sendmsgtouser2-ing
                            Surface surface = face.IGetSurface();
                            if (surface == null)
                            {
                                continue;
                            }

                            // note that we can't do this!  agh!  damnable memory leaks...
                            //reflectionPoint = face.GetProjectedPointOn(camera.centreOfVision, cameraRayVector);
                            reflectionPoint = determineRayProjectionPoint(camera.centreOfVision, cameraRayVector, face); 

                            // TODO : try making the ray longer and using getClosestPoint
                            // TODO : try making the ray longer and finding the intersection??
                            
                            if (reflectionPoint == null)
                            {
                                continue;
                            }

                            double[] reflectionPointData = (double[])reflectionPoint.ArrayData;
                            swApp.SendMsgToUser2("looks like we got reflection point " + reflectionPointData[0] + "," + reflectionPointData[1] + "," + reflectionPointData[2], 1, 1);

                            double[] ridiculousReturn = surface.EvaluateAtPoint(reflectionPoint.ArrayData[0],
                                                                                reflectionPoint.ArrayData[1],
                                                                                reflectionPoint.ArrayData[2]);
                            double[] surfaceNormal = { ridiculousReturn[0], ridiculousReturn[1], ridiculousReturn[2] };
                            swApp.SendMsgToUser2("looks like we got surface normal " + surfaceNormal[0] + "," + surfaceNormal[1] + "," + surfaceNormal[2], 1, 1);
                            double[] xyz = (double[])cameraRayVector.ArrayData;
                            double[] reflectionDir = camera.calculateReflectionDir(xyz, surfaceNormal);
                            reflectionVector = mathUtils.CreateVector(reflectionDir);
                            swApp.SendMsgToUser2("looks like we got reflection ray " + reflectionDir[0] + "," + reflectionDir[1] + "," + reflectionDir[2], 1, 1);

                            visualizeRay("reflection", reflectionVector, reflectionPoint);

                            intersecting = intersectsComponentSomewhere(swComponent, reflectionPoint, reflectionVector, out intersectionPoint, out intersectedFace);

                            face = face.IGetNextFace();
                        }

                        if (intersecting)
                        {
                            swApp.SendMsgToUser2("intersected!", 1, 1);
                            // put a mirror on the face at reflectionPoint
                            createMirrorExtrusion(intersectedFace, reflectionPoint);
                            break;
                        }
                    }
                    if (intersecting)
                    {
                        break;
                    }
                }
                
            }

            return intersecting;
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

        private List<ComponentIdentifier> getAllOurComponents()
        {
            List<ComponentIdentifier> found = new List<ComponentIdentifier>();
            Component2 swComp = swConf.GetRootComponent3(true);
            object[] vChildComp;
            Component2 swChildComp;
            int i;

            vChildComp = (object[])swComp.GetChildren();
            for (i = 0; i < vChildComp.Length; i++)
            {
                swChildComp = (Component2)vChildComp[i];
                foreach (String compName in ourComponentNames)
                {
                    if (swChildComp.Name2.StartsWith(compName) && !swChildComp.Equals(mainBody))
                    {
                        ComponentIdentifier ci = new ComponentIdentifier(swChildComp);
                        found.Add(ci);
                    }
                }
            }
            return found;
        }

        private bool isBody(Component2 swComponent) {
            object bodiesInfo = null;
            swComponent.GetBodies3((int)swBodyType_e.swSolidBody, out bodiesInfo);
            return bodiesInfo != null && ((int[])bodiesInfo).Length > 0;
        }

        private List<Component2> getAllComponents()
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

        // TODO : enforce an ordering on the clicks of the buttons

        private void processFeatures_Click(object sender, EventArgs e)
        {
            print.Enabled = true;
            getModelDoc();
            getFOV();

            if (camera.fieldOfView == null)
            {
                return;
            }

            foreach (ComponentIdentifier c in ourComponents)
            {
                IConfiguration newConfig = createNewConfiguration(c.component);
                // TODO change me back!  I need to lengthen flags first, but not while we test raytracing
                bool success = false;//lengthenFlag(c.component, newConfig);
                if (!success)
                {
                    // try placing a mirror for it
                    success = placeMirror(c.component, newConfig);
                    if (!success)
                    {
                        // cry
                        swApp.SendMsgToUser2("we can't seem to see component " + c.component.Name + " you will have to move it", 1, 1);
                    }
                }
            }

            // cast rays from the camera and see what we get!  hopefully some bounding boxes.  :)

            swAssembly.EditAssembly();
        }

        private void test_mode_Click(object sender, EventArgs e)
        {
            getModelDoc();
            String exit_test = "stop testing";
            String enter_test = "start testing!";
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
            test_mode.Enabled = true;
            getModelDoc();
            foreach (ComponentIdentifier c in ourComponents)
            {
                c.component.Select(false);

                // we want to process this appropriately: send it to Colin
                sendMessage(c.OSC_string, "start");

                // now ask the user to register it
                swApp.SendMsgToUser2("please actuate " + c.component.Name + ", then click OK",
                                     (int)swMessageBoxIcon_e.swMbQuestion,
                                     (int)swMessageBoxBtn_e.swMbOk);

                sendMessage(c.OSC_string, "done");
            }
        }

        private void insertCameraAt(double x, double y, double z)
        {
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

            swAssembly.AddComponent(FOV_file, x, y, z);
            swApp.CloseDoc(FOV_file);
            swAssembly.ForceRebuild();
        }

        private void insertCamera()
        {
            insertCameraAt(0, 0, 0);
        }

        private void insert_camera_Click(object sender, EventArgs e)
        {
            processFeatures.Enabled = true;

            getModelDoc();
            insertCamera();
            getFOV();
            camera.fieldOfView.Select(false);
            swAssembly.SetComponentTransparent(true);
            camera.fieldOfView.DeSelect();
            swAssembly.EditAssembly();
        }

        private void print_Click(object sender, EventArgs e)
        {
            register.Enabled = true;
            getModelDoc();
            getFOV();

            camera.fieldOfView.SetSuppression2((int)swComponentSuppressionState_e.swComponentSuppressed);
            Camera.removeRayIfPresent(swDoc);
            int errors = 0, warnings = 0;
            string fileName = "C:\\Users\\Valkyrie\\Dropbox\\Sauron\\toPrint\\" + "SAURON-AUTO-" + randomString() + ".STL";
            swDoc.Extension.SaveAs(fileName, 0, 0, null, ref errors, ref warnings);

            mainBody.Select(false);
            swAssembly.SetComponentTransparent(true);
            InstructionsGenerator.createInstructions(ourComponents, generatedMirrorExtrusions, fileName);

            camera.fieldOfView.SetSuppression2((int)swComponentSuppressionState_e.swComponentFullyResolved);
        }

        private void testPart_Click(object sender, EventArgs e)
        {
            getModelDoc();
            getFOV();

            intersectsComponents();
        }

        private void alert(string text)
        {
            swApp.SendMsgToUser2(text, 1, 1);
        }
    }
}
