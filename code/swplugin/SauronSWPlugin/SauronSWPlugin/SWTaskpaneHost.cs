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
        public const string OUTPUT_STL_FOLDER = "C:\\Users\\Valkyrie\\Dropbox\\Sauron\\toPrint\\";
        public const string FOV_FILE = BASE_SW_FOLDER + "\\point-grey-fov.SLDPRT";
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
        private String[] ourComponentNames = { "button-4post", "dial", "joystick-all-pieces",
                                               "slider", "scroll-wheel", "dpad" };

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

            if (mainBody == null)
            {
                mainBody = findComponent(swConf.GetRootComponent3(true), "base");
            }

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

            swAssembly.EditAssembly();
        }

        private IMathPoint getNamedPoint(string name, Component2 parent)
        {
            swDoc.ClearSelection2(true);
            swDoc.Extension.SelectByID2(name + "@" + parent.GetSelectByIDString(),
                                        "DATUMPOINT", 0, 0, 0, false, 0, null, 0);
            IFeature namedPointFeature = swSelectionMgr.GetSelectedObject6(1, -1) as IFeature;

            if (namedPointFeature == null)
            {
                return null;
            }

            measure.Calculate(null);
            IMathPoint namedPoint = mathUtils.CreatePoint(new double[] { measure.X, measure.Y, measure.Z });

            return namedPoint;
        }

        private double distanceFromFlagToCamera(Component2 component)
        {
            swFeatureMgr.EditRollback((int)swMoveRollbackBarTo_e.swMoveRollbackBarToEnd, "");
            swAssembly.EditRebuild();

            IMathPoint centreOfFlagBase = getNamedPoint("centre of flag base", component).MultiplyTransform(component.Transform2);
            IMathPoint centreOfFlagTip = getNamedPoint("centre of flag top", component).MultiplyTransform(component.Transform2);

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
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS),
                                                                    (double).0000001,
                                                                    (double).0000001);

            if (numIntersectionsFound == 0)
            {
                return 0;
            }

            double[] horrifyingReturn = (double[])swDoc.GetRayIntersectionsPoints();
            
            double[] intersectionPoint = new double[3];
            double x = horrifyingReturn[3];
            double y = horrifyingReturn[4];
            double z = horrifyingReturn[5];
            intersectionPoint[0] = x;
            intersectionPoint[1] = y;
            intersectionPoint[2] = z;
            
            double distanceFromFlag = distanceFormula(intersectionPoint, dataTip);

            return Math.Min(distanceFromFlag, distanceBetween(component, camera.fieldOfView));
        }

        private double distanceFormula(double[] pt1, double[] pt2)
        {
            double total = 0;
            for (int i = 0; i < Math.Min(pt1.Length, pt2.Length); i++)
            {
                total += Math.Pow(pt1[i] - pt2[i], 2);
            }

            return Math.Sqrt(total);
        }

        private double distanceBetween(Component2 component, Component2 otherComponent)
        {
            component.Select(false);
            otherComponent.Select(true);
            measure.Calculate(null);
            return measure.Distance;
        }

        private bool lengthenFlag(Component2 swComponent)
        {
            double threshold = inchesToMeters(.01);
            IFeature extrusionFeature = swComponent.FeatureByName("flag");
            
            if (extrusionFeature == null)
            {
                return false;
            }

            ExtrudeFeatureData2 extrusion = extrusionFeature.GetDefinition();
            extrusion.AccessSelections(swAssembly, swComponent);

            double originalDepth = extrusion.GetDepth(true);
            double defaultDepth = inchesToMeters(.15);

            updateExtrudeDepth(extrusionFeature, defaultDepth);

            double distance = distanceFromFlagToCamera(swComponent);

            updateExtrudeDepth(extrusionFeature, distance + originalDepth);

            if (!(distanceBetween(swComponent, camera.fieldOfView) < threshold))
            {
                // back off ; they are separated in some way that we can't extrude through (e.g. angles are wrong)
                alert("we can't extrude to make it there for " + swComponent.Name);
                updateExtrudeDepth(extrusionFeature, originalDepth);
                return false;
            }

            // we are intersecting successfully, but we need to make sure to check we aren't intersecting anything else...
            foreach (Component2 otherComponent in allSolidComponents)
            {
                if (otherComponent.Name.Equals(swComponent.Name) || otherComponent.Name.Equals(camera.fieldOfView.Name))
                {
                    continue;
                }
                if (distanceBetween(swComponent, otherComponent) < threshold)
                {
                    alert("we extruded through something else for " + swComponent.Name);
                    updateExtrudeDepth(extrusionFeature, originalDepth);
                    return false;
                }
            }
            
            return true;
        }

        private double[] putInMainBodySpace(ReflectionPoint rp)
        {
            MathVector offsetOfMainBody = mathUtils.CreateVector(new double[] { mainBody.Transform2.ArrayData[9],
                                                                                mainBody.Transform2.ArrayData[10],
                                                                                mainBody.Transform2.ArrayData[11] });
            return rp.location.ISubtractVector(offsetOfMainBody).ArrayData;
        }

        private void createMirrorExtrusion(ReflectionPoint reflectionPoint)
        {
            swDoc.SketchManager.Insert3DSketch(true);
            swDoc.SketchManager.AddToDB = true;
            swDoc.SketchManager.CreatePoint(reflectionPoint.xyz[0], reflectionPoint.xyz[1], reflectionPoint.xyz[2]);
            swDoc.SketchManager.AddToDB = false;
            swDoc.SketchManager.Insert3DSketch(true);

            return;
            
            /*
            // TODO make this work!  need to debug putInMainBodySpace, above
            double mirrorWidth = inchesToMeters(1);
            double[] pointLocation = putInMainBodySpace(reflectionPoint);

            double[] surfaceNormal = reflectionPoint.nxnynz;
            
            // select the main body, because that is what we want to draw on
            mainBody.Select(false);
            int status = 0;
            swAssembly.EditPart2(true, false, ref status);
            swSelectionMgr.SetSelectionPoint2(1, -1, pointLocation[0], pointLocation[1], pointLocation[2]);
            swSketchMgr.InsertSketch(true);

            swSketchMgr.CreateCenterRectangle(pointLocation[0], pointLocation[1], pointLocation[2],
                                              pointLocation[0] + .5 * mirrorWidth * (1 - surfaceNormal[0]),
                                              pointLocation[1] + .5 * mirrorWidth * (1 - surfaceNormal[1]),
                                              pointLocation[2] + .5 * mirrorWidth * (1 - surfaceNormal[2]));
            
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
            swAssembly.EditAssembly();
            */
        }

        private void moveToPosition(Component2 component, double position)
        {
            /*
             * Buttons have 1 and 0 positions
             * Sliders move along the track from 1 to 0 in .25 steps
             * Dials move in a circle from 1 to 0 in .25 (90 degree) steps
             * Joysticks move x 1-0 and y 1-0
             * Dpads move up (1), down (.75), left (.5), right (.25)
             */

            // for now, we will just try to move around the buttons, sliders, and dials, because that is least impossible!
            if (component.Name2.StartsWith("joystick") || component.Name2.StartsWith("dpad"))
            {
                return;
            }

            // Sorry, this is terrible.  first, we select the componenta and what it's mated to (the main body) with mark 1
            component.Select2(false, 1);
            mainBody.Select2(true, 1);
            // now we select the mate we are trying to modify
            Mate2 mate = findAndSelectMate(component);

            bool flip = false;
            double distance = 0;
            double distanceAbsUpperLimit = 0;
            double distanceAbsLowerLimit = 0;
            double gearRatioNumerator = 0;
            double gearRatioDenominator = 0;
            double angle = 0;
            double angleAbsUpperLimit = 0;
            double angleAbsLowerLimit = 0;
            int errorStatus = 0;
            swAssembly.EditMate2((int)swMateType_e.swMateDISTANCE, (int)swMateAlign_e.swMateAlignCLOSEST,
                                 flip, distance, distanceAbsUpperLimit, distanceAbsLowerLimit,
                                 gearRatioNumerator, gearRatioDenominator,
                                 angle, angleAbsUpperLimit, angleAbsLowerLimit,
                                 out errorStatus);
        }

        private bool rawRaysSeeComponentInCurrentConfig(Component2 component)
        {
            /*
             * see https://forum.solidworks.com/message/348151
             * 
             * TODO : need to determine if a ray hit anything *else* before, too, not just mainbody
             */

            List<IBody2> bodies = new List<IBody2>();

            object vBodyInfo;
            object[] componentBodies = (object[])component.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
            for (int i = 0; i < componentBodies.Length; i++)
            {
                IBody2 tempBody = ((Body2)componentBodies[i]).ICopy();
                tempBody.ApplyTransform(component.Transform2);
                bodies.Add(tempBody);
            }

            double[] rayVectorOrigins = camera.rayVectorOrigins();
            double[] rayVectorDirections = camera.rayVectorDirections();

            int numIntersectionsFound = (int)swDoc.RayIntersections((object)bodies.ToArray(),
                                                                    (object)rayVectorOrigins,
                                                                    (object)rayVectorDirections,
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS),
                                                                    (double).0000001,
                                                                    (double).0000001);

            if (numIntersectionsFound == 0)
            {
                return false;
            }

            double[] horrifyingReturn = (double[])swDoc.GetRayIntersectionsPoints();

            for (int i = 0; i < horrifyingReturn.Length; i += 9)
            {
                double[] pt = new double[] { horrifyingReturn[i + 3], horrifyingReturn[i + 4], horrifyingReturn[i + 5] };
            }

            int lengthOfOneReturn = 9;
            for (int i = 0; i < numIntersectionsFound; i++)
            {
                int rayIndex = (int)horrifyingReturn[i * lengthOfOneReturn + 1];
                double x = horrifyingReturn[i * lengthOfOneReturn + 3];
                double y = horrifyingReturn[i * lengthOfOneReturn + 4];
                double z = horrifyingReturn[i * lengthOfOneReturn + 5];

                double[] rayOrigin = camera.rayVectorSources().ElementAt(rayIndex).ArrayData;
                double[] rayDirection = camera.rayVectors().ElementAt(rayIndex).ArrayData;
                double[] hitPoint = new double[] { x, y, z };

                if (!hitMainBodyBefore(rayOrigin, rayDirection, hitPoint) && !hitSomethingElseBefore(rayOrigin, rayDirection, hitPoint))
                {
                    if (visualize.Checked)
                    {
                        visualizeRay(mathUtils.CreateVector(rayDirection), mathUtils.CreatePoint(rayOrigin));
                    }
                    return true;
                }
            }

            return false;
        }

        private bool rawRaysCanSeeComponentDirectly(Component2 component)
        {
            
            double pos = 1;
            bool canSeeInAllConfigs = true;
            while (pos > 0)
            {
                canSeeInAllConfigs &= rawRaysSeeComponentInCurrentConfig(component);
                moveToPosition(component, pos);

                if (component.Name2.StartsWith("button"))
                {
                    pos = 0;
                }
                else if (component.Name2.StartsWith("dial"))
                {
                    pos -= .33;
                }
                else
                {
                    pos -= .25;
                }

                if (!canSeeInAllConfigs)
                {
                    break;
                }
            }

            return canSeeInAllConfigs;
        }

        private double distanceAlongRay(double[] rayOrigin, double[] xyz)
        {
            return Math.Sqrt(Math.Pow(rayOrigin[0] - xyz[0], 2) + Math.Pow(rayOrigin[1] - xyz[1], 2) + Math.Pow(rayOrigin[2] - xyz[2], 2));
        }

        private bool hitMainBodyBefore(double[] rayOrigin, double[] rayDirection, double[] hitPointToCompareTo)
        {
            List<IBody2> bodies = new List<IBody2>();

            object vBodyInfo;
            object[] componentBodies = (object[])mainBody.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
            for (int i = 0; i < componentBodies.Length; i++)
            {
                IBody2 tempBody = ((Body2)componentBodies[i]).ICopy();
                tempBody.ApplyTransform(mainBody.Transform2);
                bodies.Add(tempBody);
            }

            int numIntersectionsFound = (int)swDoc.RayIntersections((object)bodies.ToArray(),
                                                                    (object)rayOrigin,
                                                                    (object)rayDirection,
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS | swRayPtsOpts_e.swRayPtsOptsENTRY_EXIT),
                                                                    (double).0000001,
                                                                    (double).0000001);

            double[] horrifyingReturn = (double[])swDoc.GetRayIntersectionsPoints();

            if (numIntersectionsFound == 0)
            {
                return false;
            }

            int lengthOfOneReturn = 9;

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

                if (distanceAlongRay(rayOrigin, new double[] { x, y, z }) < distanceAlongRay(rayOrigin, hitPointToCompareTo))
                {
                    return true;
                }
            }

            return false;
        }

        private bool hitSomethingElseBefore(double[] rayOrigin, double[] rayDirection, double[] hitPointToCompareTo)
        {
            foreach (ComponentIdentifier ci in ourComponents)
            {
                double[] someHit = whereRayHitsComponent(ci.component, mathUtils.CreatePoint(rayOrigin), mathUtils.CreateVector(rayDirection));
                if(someHit == null) {
                    continue;
                }

                if (distanceAlongRay(rayOrigin, someHit) < distanceAlongRay(rayOrigin, hitPointToCompareTo))
                {
                    return true;
                }
            }
            return false;
        }

        private bool rayHitsComponent(Component2 component, MathPoint rayOrigin, MathVector rayDirection)
        {
            return whereRayHitsComponent(component, rayOrigin, rayDirection) != null;
        }

        private double[] whereRayHitsComponent(Component2 component, MathPoint rayOrigin, MathVector rayDirection)
        {
            List<IBody2> bodies = new List<IBody2>();

            object vBodyInfo;
            object[] componentBodies = (object[])component.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
            for (int i = 0; i < componentBodies.Length; i++)
            {
                IBody2 tempBody = ((Body2)componentBodies[i]).ICopy();
                tempBody.ApplyTransform(component.Transform2);
                bodies.Add(tempBody);
            }

            int numIntersectionsFound = (int)swDoc.RayIntersections((object)bodies.ToArray(),
                                                                    (object)rayOrigin.ArrayData,
                                                                    (object)rayDirection.ArrayData,
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS),
                                                                    (double).0000001,
                                                                    (double).0000001);

            if (numIntersectionsFound == 0)
            {
                return null;
            }

            double[] horrifyingReturn = (double[])swDoc.GetRayIntersectionsPoints();

            int lengthOfOneReturn = 9;

            for (int i = 0; i < numIntersectionsFound; i++)
            {
                double x = horrifyingReturn[i * lengthOfOneReturn + 3];
                double y = horrifyingReturn[i * lengthOfOneReturn + 4];
                double z = horrifyingReturn[i * lengthOfOneReturn + 5];

                double[] hitPoint = new double[] { x, y, z };

                if (!hitMainBodyBefore(rayOrigin.ArrayData, rayDirection.ArrayData, hitPoint))
                {
                    return hitPoint;
                }
            }

            return null;
        }

        private ReflectionPoint reflectedRayCanSeeComponent(Component2 component)
        {
            List<IBody2> bodies = new List<IBody2>();

            object vBodyInfo;
            object[] componentBodies = (object[])mainBody.GetBodies3((int)swBodyType_e.swSolidBody, out vBodyInfo);
            for (int i = 0; i < componentBodies.Length; i++)
            {
                IBody2 tempBody = ((Body2)componentBodies[i]).ICopy();
                tempBody.ApplyTransform(mainBody.Transform2);
                bodies.Add(tempBody);
            }

            double[] rayOrigins = new double[camera.rayVectorOrigins().Length];
            camera.rayVectorOrigins().CopyTo(rayOrigins, 0);
            double[] rayDirections = new double[camera.rayVectorDirections().Length];
            camera.rayVectorDirections().CopyTo(rayDirections, 0);

            int numIntersectionsFound = (int)swDoc.RayIntersections((object)bodies.ToArray(),
                                                                    (object)rayOrigins,
                                                                    (object)rayDirections,
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS | swRayPtsOpts_e.swRayPtsOptsENTRY_EXIT),
                                                                    (double).0000001,
                                                                    (double).0000001);

            double[] horrifyingReturn = (double[])swDoc.GetRayIntersectionsPoints();

            if (numIntersectionsFound == 0)
            {
                // we didn't hit the main body at all...??
                return null;
            }

            int lengthOfOneReturn = 9;

            for (int i = 0; i < numIntersectionsFound; i++)
            {
                byte intersectionType = (byte)horrifyingReturn[i * lengthOfOneReturn + 2];

                if((intersectionType & (byte)swRayPtsResults_e.swRayPtsResultsENTER) == 0) {
                    // we need it to be just entry rays
                    continue;
                }

                int rayIndex = (int)horrifyingReturn[i * lengthOfOneReturn + 1];

                double x = horrifyingReturn[i * lengthOfOneReturn + 3];
                double y = horrifyingReturn[i * lengthOfOneReturn + 4];
                double z = horrifyingReturn[i * lengthOfOneReturn + 5];
                double nx = horrifyingReturn[i * lengthOfOneReturn + 6];
                double ny = horrifyingReturn[i * lengthOfOneReturn + 7];
                double nz = horrifyingReturn[i * lengthOfOneReturn + 8];

                ReflectionPoint rp = new ReflectionPoint(mathUtils, new double[] { x, y, z }, new double[] { nx, ny, nz });
                double[] reflectionDir = camera.calculateReflectionDir(camera.rayVectors().ElementAt(rayIndex).ArrayData, rp.nxnynz);
                MathVector reflectedRay = mathUtils.CreateVector(reflectionDir);

                if (rayHitsComponent(component, rp.location, reflectedRay))
                {
                    if (visualize.Checked)
                    {
                        visualizeRay(rp.location, reflectedRay);
                    }
                    return rp;
                }
            }

            return null;
        }

        private void visualizeRay(MathVector ray, IMathPoint origin)
        {
            double[] rayData = (double[])ray.ArrayData;
            double[] originData = (double[])origin.ArrayData;
            swDoc.Insert3DSketch2(true);
            swDoc.SketchManager.AddToDB = true;
            swDoc.CreateLine2(originData[0], originData[1], originData[2],
                              originData[0] + rayData[0], originData[1] + rayData[1], originData[2] + rayData[2]);
            swDoc.SketchManager.AddToDB = false;
            swDoc.Insert3DSketch2(true);
        }

        private void visualizeRay(IMathPoint origin, MathVector ray)
        {
            visualizeRay(ray, origin);
        }

        private bool placeMirror(Component2 swComponent)
        {
            // intutively, what we want to do:
            // look to see if any camera rays hit the object as-is, then no worries
            // if not, look to see if any camera rays reflecting off the main body hit the object
            //   if yes, put a mirror in that spot
            //   if no, we fail and cry

            if (rawRaysCanSeeComponentDirectly(swComponent))
            {
                return true;
            }

            ReflectionPoint rp = reflectedRayCanSeeComponent(swComponent);

            if (rp != null)
            {
                createMirrorExtrusion(rp);
                return true;
            }

            return false;
        }

        private Mate2 findAndSelectMate(Component2 matedComponent)
        {
            // TODO : need to label all these fucking mates... in the assemblies
            swDoc.Extension.SelectByID2(matedComponent.Name2 + "-SAURON", "MATE", 0, 0, 0, false, 0, null, 0);
            Mate2 mate = (Mate2) swSelectionMgr.GetSelectedObject6(1, -1);
            return mate;
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

        private void processFeatures_Click(object sender, EventArgs e)
        {
            print.Enabled = true;
            getModelDoc();
            getFOV();

            if (camera.fieldOfView == null)
            {
                alert("you need to add the camera before you can do this");
                return;
            }

            foreach (ComponentIdentifier c in ourComponents)
            {
                createNewConfiguration(c.component);
                bool success = lengthenFlag(c.component);
                if (!success)
                {
                    // try placing a mirror for it
                    success = placeMirror(c.component);
                    if (!success)
                    {
                        // cry
                        alert("we can't seem to see component " + c.component.Name + " you will have to move it or tweak it manually");
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
            int loadErrs = 0;
            int loadWarns = 0;
            swApp.OpenDoc6(FOV_FILE,
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

            swAssembly.AddComponent(FOV_FILE, x, y, z);
            swApp.CloseDoc(FOV_FILE);
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
            camera.removeRayIfPresent();
            int errors = 0, warnings = 0;
            string fileName = OUTPUT_STL_FOLDER + "SAURON-AUTO-" + randomString() + ".STL";
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

            moveToPosition(ourComponents.ElementAt(0).component, 0);
        }

        private void alert(string text)
        {
            swApp.SendMsgToUser2(text, 1, 1);
        }

        private string stringify(double[] someArray)
        {
            string ret = "";
            for (int i = 0; i < someArray.Length; i++)
            {
                ret += i + ",";
            }
            ret = ret.Remove(ret.Length - 1);
            return ret;
        }
    }
}
