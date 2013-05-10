using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
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
    [ComVisible(true)]
    [ProgId(SWTASKPANE_PROGID)]
    public partial class SWTaskpaneHost : UserControl
    {
        public bool DEBUG = false;
        
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
        private String[] ourComponentNames = { "button", "dial", "joystick",
                                               "slider", "scroll-wheel", "dpad", "trackball" };


        private Camera camera = null;
        private OSCCommunicator oscCommunicator = null;

        private List<Component2> allSolidComponents = null;
        private List<ComponentIdentifier> ourComponents = null;
        private List<IFeature> generatedMirrorExtrusions = null;

        public SWTaskpaneHost()
        {
            InitializeComponent();
            initOSC();
        }

        private void initOSC()
        {
            oscCommunicator = new OSCCommunicator(componentMotionDetected);
        }

        private void componentMotionDetected(String addressedTo)
        {
            foreach (ComponentIdentifier ci in ourComponents)
            {
                if (ci.isAddressedAs(addressedTo))
                {
                    // do something with the component
                    ci.component.Select(false);
                }
            }
        }

        private void setup()
        {
            getModelDoc();
            getFOV();
            camera.infiniteCamera();
        }

        private void teardown()
        {
            camera.regularCamera();
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
                if (mainBody == null)
                {
                    Utilities.alert("we need a component with 'body' in its name to be the main body");
                }
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

            RayTracer.swApp = swApp;
            RayTracer.swDoc = swDoc;
            RayTracer.swAssembly = swAssembly;
            RayTracer.swSelectionMgr = swSelectionMgr;
            RayTracer.swSketchMgr = swSketchMgr;
            RayTracer.mathUtils = mathUtils;
            RayTracer.visualize = visualize.Checked;
            RayTracer.ourComponents = ourComponents;
            RayTracer.camera = camera;

            Simulator.swApp = swApp;
            Simulator.swDoc = swDoc;
            Simulator.swAssembly = swAssembly;
            Simulator.swFeatureMgr = swFeatureMgr;
            Simulator.mainBody = mainBody;
            Simulator.ourComponents = ourComponents;

            Utilities.swApp = swApp;
            Utilities.swDoc = swDoc;
            Utilities.swAssembly = swAssembly;
            Utilities.swSelectionMgr = swSelectionMgr;
            Utilities.swConfMgr = swConfMgr;
            Utilities.swConf = swConf;
            Utilities.swFeatureMgr = swFeatureMgr;
            Utilities.swSketchMgr = swSketchMgr;
            Utilities.measure = measure;
            Utilities.mathUtils = mathUtils;
        }

        private void getFOV()
        {
            if (camera == null)
            {
                Component2 fieldOfView = findComponent(swConf.GetRootComponent3(true), Camera.FOV);
                camera = Camera.createCamera(fieldOfView, swApp, swDoc, swAssembly, swSelectionMgr, mathUtils);
                RayTracer.camera = camera;
            }
            camera.swDoc = swDoc;
            camera.swAssembly = swAssembly;
            camera.swSelectionMgr = swSelectionMgr;
            camera.mathUtils = mathUtils;
            camera.swApp = swApp;
            camera.drawInitalRay();
        }


        private bool lengthenFlag(Component2 swComponent)
        {
            return Simulator.extendFlagOverRangeOfMotion(swComponent);
        }

        private void createMirrorExtrusion(ReflectionPoint reflectionPoint)
        {
            RayTracer.startSketch();
            RayTracer.visualizePoint(reflectionPoint.location);
            /*swDoc.SketchManager.CreateCenterRectangle(reflectionPoint.xyz[0], reflectionPoint.xyz[1], reflectionPoint.xyz[2],
                                                      reflectionPoint.xyz[0] + (inchesToMeters(1) * (1 - reflectionPoint.nxnynz[0])),
                                                      reflectionPoint.xyz[1] + (inchesToMeters(1) * (1 - reflectionPoint.nxnynz[1])),
                                                      reflectionPoint.xyz[2] + (inchesToMeters(1) * (1 - reflectionPoint.nxnynz[2])));*/
            RayTracer.finishSketch("place mirror for " + reflectionPoint.component.Name2);

            return;
            /*
            double mirrorWidth = inchesToMeters(1);
            double[] pointLocation = putInMainBodySpace(reflectionPoint);

            double[] surfaceNormal = reflectionPoint.nxnynz;
            
            // select the main body, because that is what we want to draw on
            mainBody.Select(false);
            int status = 0;
            swAssembly.EditPart2(true, false, ref status);
            swSelectionMgr.SetSelectionPoint2(1, -1, pointLocation[0], pointLocation[1], pointLocation[2]);
            swSketchMgr.AddToDB = true;
            swSketchMgr.InsertSketch(true);

            swSketchMgr.CreateCenterRectangle(pointLocation[0], pointLocation[1], pointLocation[2],
                                              pointLocation[0] + .5 * mirrorWidth * (1 - surfaceNormal[0]),
                                              pointLocation[1] + .5 * mirrorWidth * (1 - surfaceNormal[1]),
                                              pointLocation[2] + .5 * mirrorWidth * (1 - surfaceNormal[2]));

            swSketchMgr.AddToDB = false;
            
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
            swAssembly.EditAssembly();*/
            
        }

        private void highlightComponents()
        {
            double[] notVisible = new double[9] { 1, 0, 0, 1, 1, 1, 0.3, 0, 0 };
            double[] visible = new double[9] { 0, 1, 0, 1, 1, 1, 0.3, 0, 0 };
            foreach (ComponentIdentifier ci in ourComponents)
            {
                object bodyInfo;
                object[] bodies = (object[])ci.component.GetBodies3((int)swBodyType_e.swAllBodies, out bodyInfo);
                if (bodies != null && bodies.Length > 0)
                {
                    for (int i = 0; i < bodies.Length; i++)
                    {
                        Body2 body = (Body2)bodies[i];
                        if (ci.visibleToRawRays)
                        {
                            body.MaterialPropertyValues2 = visible;
                        }
                        else
                        {
                            body.MaterialPropertyValues2 = notVisible;
                        }
                    }
                }
            }
        }

        private bool placeMirror(Component2 swComponent)
        {
            // intutively, what we want to do:
            // look to see if any camera rays hit the object as-is, then no worries
            // if not, look to see if any camera rays reflecting off the main body hit the object
            //   if yes, put a mirror in that spot
            //   if no, we fail and cry

            if (!Simulator.reflectRaysOverRangeOfMotion(swComponent))
            {
                return false;
            }

            // TODO : ideally we would collect all the points that we reflect
            // from and make a mirror that shows all of them
            ReflectionPoint rp = RayTracer.reflectedRayCanSeeComponent(swComponent);
            createMirrorExtrusion(rp);

            return true;
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
                    if (swChildComp.Name2.StartsWith(compName) && !swChildComp.Equals(mainBody) && !swChildComp.IsSuppressed())
                    {
                        ComponentIdentifier ci = new ComponentIdentifier(swChildComp);
                        found.Add(ci);
                    }
                }
            }
            return found;
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
                if (Utilities.isBody(swChildComp))
                {
                    allComponents.Add(swChildComp);
                }
            }

            return allComponents;
        }

        private IConfiguration createNewConfiguration(Component2 swComponent)
        {
            string componentName = Utilities.stripIdentifier(swComponent.Name);
            string newConfigName = "SAURON-" + Utilities.randomString();

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
            setup();

            if (camera.fieldOfView == null)
            {
                Utilities.alert("you need to add the camera before you can do this");
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
                        Utilities.alert("we can't seem to see component " + c.component.Name + " you will have to move it or tweak it manually");
                    }
                    else
                    {
                        if(DEBUG)
                            Utilities.alert("we reflected to find " + c.component.Name2);
                    }
                }
                else
                {
                    if(DEBUG)
                        Utilities.alert("we found " + c.component.Name2 + " with extrusion");
                }
            }

            // TODO : cast rays from the camera and see what we get!  hopefully some bounding boxes.  :)

            swAssembly.EditAssembly();

            teardown();
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
                oscCommunicator.sendMessage("/test/", "end");
            }
            else
            {
                testing = true;
                test_mode.Text = exit_test;
                oscCommunicator.sendMessage("/test/", "start");
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
                oscCommunicator.sendMessage(c.OSC_string, "start");

                // now ask the user to register it
                swApp.SendMsgToUser2("please actuate " + c.component.Name + ", then click OK",
                                     (int)swMessageBoxIcon_e.swMbQuestion,
                                     (int)swMessageBoxBtn_e.swMbOk);

                oscCommunicator.sendMessage(c.OSC_string, "done");
            }
        }

        private void insertCamera()
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

            swAssembly.AddComponent(FOV_FILE, 0, 0, 0);
            swApp.CloseDoc(FOV_FILE);
            swAssembly.ForceRebuild();
        }

        private void insert_camera_Click(object sender, EventArgs e)
        {
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
            getModelDoc();
            getFOV();

            camera.fieldOfView.SetSuppression2((int)swComponentSuppressionState_e.swComponentSuppressed);
            camera.removeRayIfPresent();
            int errors = 0, warnings = 0;
            string fileName = OUTPUT_STL_FOLDER + "SAURON-AUTO-" + Utilities.randomString() + ".STL";
            swDoc.Extension.SaveAs(fileName, 0, 0, null, ref errors, ref warnings);

            mainBody.Select(false);
            swAssembly.SetComponentTransparent(true);
            InstructionsGenerator.createInstructions(ourComponents, generatedMirrorExtrusions, fileName);

            camera.fieldOfView.SetSuppression2((int)swComponentSuppressionState_e.swComponentFullyResolved);
        }

        private void quick_check_Click(object sender, EventArgs e)
        {
            setup();
            RayTracer.findComponentsVisible();
            highlightComponents();
            teardown();
        }
    }
}