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
        public Configuration swConf = null;
        public ISketchManager swSketchMgr = null;
        public Measure measure = null;
        public MathUtility mathUtils = null;

        public bool testing = false;

        private Component2 mainBody = null;
        private String[] ourComponents = { "button-4post", "dial", "joystick-all-pieces", "slider", "scroll-wheel", "dpad" };
        private String FOV = "fov";

        private static readonly int Port = 5001;
        IPEndPoint sourceEndPoint;
        OscServer server;

        private Component2 fieldOfView;

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

            mainBody = findComponent(swConf.GetRootComponent3(true), "body");

            allSolidComponents = allComponents();

            swAssembly.EditAssembly();
        }

        private void getFOV()
        {
            fieldOfView = findComponent(swConf.GetRootComponent3(true), FOV);
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

        private void setDepthInConfig(Component2 swComponent, IFeature extrusionFeature, double depth)
        {
            /*ExtrudeFeatureData2 extrusion = (ExtrudeFeatureData2)extrusionFeature.GetDefinition();
            extrusion.AccessSelections(swAssembly, swComponent);
            
            extrusion.SetDepth(true, depth);
            extrusionFeature.ModifyDefinition(extrusion, swDoc, swComponent);
            
            extrusion.SetChangeToConfigurations((int)swInConfigurationOpts_e.swSpecifyConfiguration,
                                                new string[] { swComponent.ReferencedConfiguration });
            extrusion.SetChangeToConfigurations((int)swInConfigurationOpts_e.swThisConfiguration,
                                                new string[] { });

            extrusion.ReleaseSelectionAccess();*/

            IDisplayDimension dispDim = (IDisplayDimension)extrusionFeature.GetFirstDisplayDimension();
            IDimension dim = dispDim.IGetDimension();

            dim.SetSystemValue3(depth,
                                (int)swSetValueInConfiguration_e.swSetValue_InSpecificConfigurations,
                                new string[] { swComponent.ReferencedConfiguration });

            swDoc.EditRebuild3();
            swDoc.ForceRebuild3(true);
            swAssembly.EditRebuild();
            swAssembly.EditAssembly();
        }

        private bool lengthenFlag(Component2 swComponent)
        {
            double threshold = inchesToMeters(.1);
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

            double distance;
            swComponent.Select(false);
            fieldOfView.Select(true);
            measure.Calculate(null);
            distance = Math.Sqrt(measure.DeltaX * measure.DeltaX + measure.DeltaY * measure.DeltaY + measure.DeltaZ * measure.DeltaZ) + originalDepth;
            setDepthInConfig(swComponent, extrusionFeature, distance);

            swAssembly.EditRebuild();
            swDoc.ForceRebuild3(false);

            swComponent.Select(false);
            fieldOfView.Select(true);
            measure.Calculate(null);

            swAssembly.EditAssembly();
            swDoc.ClearSelection2(true);

            if (!(measure.Distance < threshold))
            {
                setDepthInConfig(swComponent, extrusionFeature, defaultDepth);
                swApp.SendMsgToUser2("backed off " + swComponent.Name + " : we aren't intersecting the FOV", 1, 1);
                return false;
            }

            // we are intersecting successfully, but we need to make sure to check we aren't intersecting anything else...
            foreach (Component2 otherComponent in allSolidComponents)
            {
                if (otherComponent.Equals(swComponent))
                {
                    continue;
                }
                otherComponent.Select(false);
                swComponent.Select(true);
                measure.Calculate(null);
                if (measure.Distance < threshold)
                {
                    setDepthInConfig(swComponent, extrusionFeature, defaultDepth);
                    swApp.SendMsgToUser2("backed off " + swComponent.Name + " : we intersected with " + otherComponent.Name, 1, 1);
                    return false;
                }
            }
            
            return true;
        }

        private void createPointSketchOnSurface(Component2 swComponent, MathPoint point)
        {
            swComponent.Select(false);
            swSketchMgr.InsertSketch(true);
            swSketchMgr.CreatePoint(point.ArrayData[0], point.ArrayData[1], point.ArrayData[2]);
            swSketchMgr.InsertSketch(true);
            swComponent.DeSelect();
        }

        private MathPoint intersectsComponentSomewhere(Component2 swComponent, MathPoint rayPoint, MathVector rayDir)
        {
            /**  
             * Translated partially from https://forum.solidworks.com/message/269272 
             * 
             * TODO : only look to see if we are intersecting the FLAG THING.  we don't care about the rest of the object!
             */

            MathPoint intersectionPoint = null;
            object vBodyInfo;
            int[] bodiesInfo = null;

            object[] vBodies = swComponent.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
            bodiesInfo = (int[])vBodyInfo;

            foreach (object vBody in vBodies)
            {
                Body2 body = (Body2)vBody;
                Face2 face = body.GetFirstFace();

                while (face != null && intersectionPoint == null)
                {
                    intersectionPoint = face.GetProjectedPointOn(rayPoint, rayDir);
                    if (intersectionPoint != null && intersectionPoint.ArrayData != null)
                    {
                        double[] intersectCoords = (double[]) intersectionPoint.ArrayData;
                        swApp.SendMsgToUser2("HUZFUCKINGZAH, we intersected with " + swComponent.Name + " at " + intersectCoords[0] + "," + intersectCoords[1] + "," + intersectCoords[2], 1, 1);
                    }
                    face = face.IGetNextFace();
                }

                if (intersectionPoint != null)
                {
                    createPointSketchOnSurface(swComponent, intersectionPoint);
                    break;
                }
            }

            return intersectionPoint;
        }

        private bool placeMirror(Component2 swComponent, IConfiguration configToEdit)
        {
            // intutively, what we want to do:
            // look to see if any camera rays hit the object as-is
            // if not, look to see if any camera rays reflecting off the main body hit the object
            //   if yes, put a mirror in that spot
            //   if no, we fail and cry

            double[] origin = { 0, 0, 0 };
            double[] direction = { 0, 1, 0 };
            bool selectionWorked = swDoc.Extension.SelectByID2("centre of vision@" + fieldOfView.GetSelectByIDString(),
                                                               "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
            if (selectionWorked)
            {
                SketchPoint centreOfVision = swSelectionMgr.GetSelectedObject6(1, -1);
                origin[0] = centreOfVision.X;
                origin[1] = centreOfVision.Y;
                origin[2] = centreOfVision.Z;
                centreOfVision.DeSelect();
                swApp.SendMsgToUser2("we got the centre of vision, at " + origin, 1, 1);

                // now figure out the direction... we can try to get the normal of the surface that the centre of vision is on? how do we do that?
            }
            else
            {
                swApp.SendMsgToUser2("we didn't get the centre of vision at centre of vision@" + fieldOfView.GetSelectByIDString(), 1, 1);
            }
           
            MathPoint cameraRaySource = mathUtils.CreatePoint(origin);
            MathVector cameraRayVector = mathUtils.CreateVector(direction);
            MathPoint intersectionPoint = null;

            object vBodyInfo;
            object[] vBodies;
            int[] bodiesInfo = null;
            
            // iterate over some possible rays we can cast
            /*for (int x = 0; x <= 1; x+=.5)
            {
                for (int y = 0; y <= 1; y+=.5)
                {
                    for (int z = 0; z <= 1; z+=.5)
                    {*/
                        intersectionPoint = intersectsComponentSomewhere(swComponent, cameraRaySource, cameraRayVector);
                        /*if (intersectionPoint != null) break;
                    }
                    if (intersectionPoint != null) break;
                }
                if (intersectionPoint != null) break;
            }*/

            if (intersectionPoint == null)
            {
                MathPoint reflectionPoint = null;
                MathVector reflectionVector = null;
                // then we need to reflect to see this component
                vBodies = mainBody.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
                bodiesInfo = (int[])vBodyInfo;

                /*for (int x = 0; x <= 1; x+=.5)
                {
                    for (int y = 0; y <= 1; y+=.5)
                    {
                        for (int z = 0; z <= 1; z+=.5)
                        {*/
                            foreach (object vBody in vBodies)
                            {                    
                                Body2 body = (Body2)vBody; 
                                Face2 face = body.GetFirstFace();

                                while (face != null && intersectionPoint == null)
                                {
                                    Surface surface = face.IGetSurface();
                                    if (surface == null)
                                    {
                                        swApp.SendMsgToUser2("our surface is null?", 1, 1);
                                    }

                                    reflectionPoint = surface.GetProjectedPointOn(cameraRaySource, cameraRayVector);
                                    
                                    if (reflectionPoint == null)
                                    {
                                        continue;
                                    }

                                    double[] ridiculousReturn = surface.EvaluateAtPoint(reflectionPoint.ArrayData[0],
                                                                                        reflectionPoint.ArrayData[1],
                                                                                        reflectionPoint.ArrayData[2]);
                                    double[] surfaceNormal = { ridiculousReturn[0], ridiculousReturn[1], ridiculousReturn[2] };
                                    double[] xyz = (double[]) cameraRayVector.ArrayData;
                                    double[] reflectionDir = { xyz[0], xyz[1], -xyz[2] };
                                    reflectionVector = mathUtils.CreateVector(reflectionDir);

                                    intersectionPoint = intersectsComponentSomewhere(swComponent, reflectionPoint, reflectionVector);

                                    face = face.IGetNextFace();
                                }

                                if (intersectionPoint != null)
                                {
                                    // put a mirror on the face at reflectionPoint
                                    createPointSketchOnSurface(swComponent, reflectionPoint);
                                    break;
                                }
                            }/*
                            if (intersectionPoint != null) break;
                        }
                        if (intersectionPoint != null) break;
                    }
                    if (intersectionPoint != null) break;
                }*/
                
            }

            return intersectionPoint != null;
        }

        public void traverseFeatures(Feature swFeat, List<FeatureIdentifier> toModify, String featureName = "flag")
        {
            while ((swFeat != null))
            {
                if (swFeat.Name.Equals(featureName))
                {
                    FeatureIdentifier fi = new FeatureIdentifier();
                    fi.feature = swFeat;
                    fi.component = null;
                    toModify.Add(fi);
                }

                swFeat = (Feature)swFeat.GetNextFeature();
            }
        }

        public void traverseComponentFeatures(Component2 swComp, List<FeatureIdentifier> found)
        {
            Feature swFeat;
            swFeat = (Feature)swComp.FirstFeature();
            traverseFeatures(swFeat, found);
            foreach (FeatureIdentifier fi in found)
            {
                if (fi.component == null)
                {
                    fi.component = swComp;
                }
            }
        }

        public void traverseComponentFeatures(Component2 swComp, List<FeatureIdentifier> found, String featureName)
        {
            Feature swFeat;
            swFeat = (Feature)swComp.FirstFeature();
            traverseFeatures(swFeat, found, featureName);
            FeatureIdentifier fi = found.ElementAt(0);
            if (fi.component != null)
            {
                fi.component = swComp;
            }
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
            List<FeatureIdentifier> found = new List<FeatureIdentifier>();
            int i;

            vChildComp = (object[])swComp.GetChildren();
            for (i = 0; i < vChildComp.Length; i++)
            {
                swChildComp = (Component2)vChildComp[i];
                if (swChildComp.Name2.Equals(componentName))
                {
                    traverseComponentFeatures(swChildComp, found, featureName);
                }
            }
            if (found.Count > 0)
            {
                return found.ElementAt(0);
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
                        traverseComponentFeatures(swChildComp, found);
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
            swDoc.ForceRebuild3(false);

            return newConfig;
        }

        private void processFeatures_Click(object sender, EventArgs e)
        {
            // do the basic setup for the function
            getModelDoc();
            getFOV();
            if (fieldOfView == null)
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
                bool success = false; //lengthenFlag(f.component);
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
                break;
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
            fieldOfView.Select2(false, 1 << 0);
            swAssembly.SetComponentTransparent(true);
            fieldOfView.DeSelect();
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
