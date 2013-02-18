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
        public Measure measure = null;

        public bool testing = false;

        private String[] ourComponents = { "button-4post", "dial", "joystick-all-pieces", "slider", "scroll-wheel", "dpad" };
        private String FOV = "fov";

        private static readonly int Port = 5103;
        IPEndPoint sourceEndPoint;

        private Component2 fieldOfView;

        private List<ComponentIdentifier> componentsForSensing = new List<ComponentIdentifier>();

        public SWTaskpaneHost()
        {
            InitializeComponent();
            initOSC();
        }

        private void initOSC()
        {
            sourceEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
        }

        private void sendMessage(String address, String data) {
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
            measure = swDoc.Extension.CreateMeasure();
            measure.ArcOption = 1; // minimum distance
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

        private void lengthenExtrusion(Feature extrusionFeature, Component2 swComponent, IConfiguration configToEdit)
        {
            String[] configurationContainer = { configToEdit.Name };
            ExtrudeFeatureData2 extrusion = extrusionFeature.GetDefinition();
            extrusion.AccessSelections(swAssembly, swComponent);

            // try getting faces and determining if the features intersect using IntersectSurface
            int faceCount = extrusionFeature.GetFaceCount();
            Face2 face = extrusionFeature.IGetFaces2(ref faceCount);

            /*double point1, point2, distance;
            distance = swDoc.IClosestDistance(fieldOfView, swComponent, out point1, out point2);
            extrusion.SetDepth(true, metersToInches(distance));*/

            /*extrusion.SetEndCondition(true, (int)swEndConditions_e.swEndCondUpToBody);
            FeatureIdentifier FOV_trapezoid = findFeature(swConf.GetRootComponent3(true), fieldOfView.Name, "FOV");
            extrusion.SetEndConditionReference(true, FOV_trapezoid.feature);*/

            extrusion.SetChangeToConfigurations((int)swInConfigurationOpts_e.swThisConfiguration, configurationContainer);
            extrusionFeature.ModifyDefinition(extrusion, swDoc, swComponent);
            
            swDoc.EditRebuild3();
            swDoc.ForceRebuild3(true);
            swComponent.DeSelect();
        }

        public void traverseFeatures(Feature swFeat, List<FeatureIdentifier> toModify, String featureName="flag")
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

            // traverse the tree, searching for components with features called "flag"
            List<FeatureIdentifier> found = new List<FeatureIdentifier>();
            findModifiable(swConf.GetRootComponent3(true), found);
            
            foreach (FeatureIdentifier f in found) {
                IConfiguration newConfig = createNewConfiguration(f.component);
                lengthenExtrusion(f.feature, f.component, newConfig);
                componentsForSensing.Add(new ComponentIdentifier(f.component));
            }

            // cast rays from the camera and see what we get!
            
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
            swAssembly.AddComponent(FOV_file,
                                    x, y, z);
            swApp.CloseDoc(FOV_file);
            swAssembly.ForceRebuild();

            getFOV();
            fieldOfView.Select2(false, 1<<0);
            swAssembly.SetComponentTransparent(true);
            fieldOfView.DeSelect();
        }

        private void testPart_Click(object sender, EventArgs e)
        {
            getModelDoc();
            getFOV();

            List<FeatureIdentifier> found = new List<FeatureIdentifier>();

            // traverse the tree, searching for components with features called "flag"
            findModifiable(swConf.GetRootComponent3(true), found);

            foreach (FeatureIdentifier f in found)
            {
                IConfiguration configToEdit = createNewConfiguration(f.component);
                String[] configurationContainer = { configToEdit.Name };
                ExtrudeFeatureData2 extrusion = f.feature.GetDefinition();
                extrusion.AccessSelections(swAssembly, f.component);

                FeatureIdentifier FOV_trapezoid = findFeature(swConf.GetRootComponent3(true), fieldOfView.Name, "FOV");
                int refType = 0;
                swApp.SendMsgToUser2("looking at " + f.component.Name + "->" + f.feature.Name, 1, 1);
                swApp.SendMsgToUser2("end condition is " + extrusion.GetEndCondition(true).ToString() + " with upToBody being " + (int)swEndConditions_e.swEndCondUpToBody, 1, 1);
                Object p = extrusion.GetEndConditionReference(true, out refType);
                swApp.SendMsgToUser2(p.ToString() + " with type " + refType, 1, 1);

                extrusion.ReleaseSelectionAccess();

                swDoc.EditRebuild3();
                swDoc.ForceRebuild3(false);
            }
        }
    }
}
