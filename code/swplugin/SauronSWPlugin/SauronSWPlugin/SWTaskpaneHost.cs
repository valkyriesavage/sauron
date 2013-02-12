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

        public bool testing = false;

        private String[] ourComponents = { "button-4post", "dial", "joystick-all-pieces", "slider", "scroll-wheel", "dpad" };
        private String FOV = "fov";

        private static readonly int Port = 5103;
        private static readonly string AliveMethod = "/osctest/alive";
        IPEndPoint sourceEndPoint;

        private Component2 fieldOfView;

        public SWTaskpaneHost()
        {
            InitializeComponent();
            initOSC();
        }

        private void initOSC()
        {
            sourceEndPoint = new IPEndPoint(IPAddress.Loopback, Port);
        }

        private void sendMessage(String data) {
            if (data == null)
            {
                return;
            }

            OscBundle bundle = new OscBundle(sourceEndPoint);

            OscMessage message = new OscMessage(sourceEndPoint, AliveMethod);
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
        }

        private void getFOV()
        {
            fieldOfView = findComponent(swConf.GetRootComponent3(true), FOV);
        }

        private double inchesToMeters(double inches)
        {
            return inches/2.54/100;
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

        private void lengthenExtrusion(Feature feature, Component2 swComponent, IConfiguration configToEdit)
        {
            Random rand = new Random();
            String[] configurationContainer = { "default" };
            ExtrudeFeatureData2 extrusion = feature.GetDefinition();
            extrusion.AccessSelections(swAssembly, swComponent);
            swComponent.Select2(false, 1<<0);
            fieldOfView.Select2(true, 1<<1);
            extrusion.SetEndCondition(true, (int)swEndConditions_e.swEndCondUpToBody);
            extrusion.SetEndConditionReference(true, fieldOfView);
            extrusion.SetChangeToConfigurations((int)swInConfigurationOpts_e.swThisConfiguration, configurationContainer);
            feature.ModifyDefinition(extrusion, swDoc, swComponent);
            swDoc.ForceRebuild3(true);
            swComponent.DeSelect();
            fieldOfView.DeSelect();
        }

        public void traverseFeatures(Feature swFeat, List<FeatureIdentifier> toModify)
        {
            while ((swFeat != null))
            {
                if (swFeat.Name.Equals("flag"))
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

        private string copyDocumentForNewConfiguration(Component2 swComponent)
        {
            string componentName = stripIdentifier(swComponent.Name);

            string newDocName = "SAURON-" + componentName + "-" + randomString() + ".SLDPRT";
            string newDocFolder = String.Format("SAURON-{0}", componentName);
            System.IO.Directory.CreateDirectory(newDocFolder);
            
            string newDocLocation = String.Format("{0}\\{1}\\{2}", BASE_SW_FOLDER, newDocFolder, newDocName);
            string oldDocLocation = String.Format("{0}\\{1}.SLDPRT", BASE_SW_FOLDER, componentName);
            System.IO.File.Copy(oldDocLocation, newDocLocation, true);

            // select the part, then replace it in the assembly with the new copy
            swComponent.Select(false);
            swApp.SendMsgToUser2(String.Format("please replace part {0} \n with generated part \n {1} \n in \n {2}", swComponent.Name, newDocName, newDocFolder),
                                 1, 1);
            //swAssembly.ReplaceComponents(newDocLocation, "default", false, true);

            return newDocName;
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

        private void create_dupes_Click(object sender, EventArgs e)
        {
            getModelDoc();
            getFOV();

            List<FeatureIdentifier> found = new List<FeatureIdentifier>();

            // traverse the tree, searching for components with features called "flag"
            findModifiable(swConf.GetRootComponent3(true), found);

            foreach (FeatureIdentifier f in found)
            {
            }
        }

        private void processFeature_Click(object sender, EventArgs e)
        {
            getModelDoc();
            getFOV();

            List<FeatureIdentifier> found = new List<FeatureIdentifier>();

            // traverse the tree, searching for components with features called "flag"
            findModifiable(swConf.GetRootComponent3(true), found);
            
            foreach (FeatureIdentifier f in found) {
                IConfiguration newConfig = createNewConfiguration(f.component);
                lengthenExtrusion(f.feature, f.component, newConfig);
            }
        }

        private void stuff()
        {
            // a place for me to copy/paste from macros and not hurt anything
            getModelDoc();

            // copy/paste to extrude the buttons up to the body of the controller...
            // it merges them, though, sadly, and doesn't seem to be complete
            bool boolstatus = false;
            boolstatus = swDoc.Extension.SelectByID2("flag@button-4post-4@xbox-controller",
                                                     "BODYFEATURE",
                                                     0,
                                                     0,
                                                     0,
                                                     false,
                                                     0,
                                                     null,
                                                     0);
            boolstatus = swDoc.Extension.SelectByID2("base@button-4post-4@xbox-controller",
                                                     "SOLIDBODY",
                                                     0,
                                                     0,
                                                     0,
                                                     true,
                                                     0,
                                                     null,
                                                     0);
            boolstatus = swDoc.Extension.SelectByID2("right/left holes@xbox-body-joystick-4-buttons-dpad-1@xbox-controller",
                                                     "SOLIDBODY",
                                                     -0.037559268647044064,
                                                     0.070797231901956081,
                                                     0.0096568896531579185,
                                                     true,
                                                     1,
                                                     null,
                                                     0);
            swDoc.ISelectionManager.EnableContourSelection = false;
            swAssembly.AssemblyPartToggle();
            swAssembly.EditAssembly();
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
                sendMessage("endtest");
            }
            else
            {
                testing = true;
                test_mode.Text = exit_test;
                sendMessage("starttest");
            }
        }

        private void register_Click(object sender, EventArgs e)
        {
            getModelDoc();
            bool traverse = false;
            bool searchFlag = false;
            bool addReadOnlyInfo = false;
            String[] allComponents = swDoc.GetDependencies2(traverse, searchFlag, addReadOnlyInfo);
            foreach (String compName in allComponents)
            {
                foreach (String ourComponent in ourComponents)
                {
                    if (ourComponent.Equals(compName))
                    {
                        swApp.SendMsgToUser2("please actuate " + compName + ", then click OK",
                                             (int)swMessageBoxIcon_e.swMbQuestion,
                                             (int)swMessageBoxBtn_e.swMbOk);
                        // we want to process this appropriately: send it to Colin
                        sendMessage(compName);
                        // how do we get the actual part once we have the name?
                        //bool boolstatus = swDoc.Extension.SelectByID2(compName, "COMPONENT", 0, 0, 0, false, 0, null, 0);
                    }
                }
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

            // tests that we want to perform on a single model, rather than in the assembly
        }
    }
}
