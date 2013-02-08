using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Net;
using System.Text;
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
        public SldWorks swApp;
        public ModelDoc2 swDoc = null;
        public AssemblyDoc swAssembly = null;
        public SelectionMgr swSelectionMgr = null;
        public ConfigurationManager swConfMgr = null;
        public Configuration swConf = null;

        public bool testing = false;
        public bool registering = false;

        private String[] ourComponents = {"button-4post","dial","joystick-all-pieces","slider","scroll-wheel","dpad"};

        private static readonly int Port = 5103;
        private static readonly string AliveMethod = "/osctest/alive";
        IPEndPoint sourceEndPoint;

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

        private double inchesToMeters(double inches)
        {
            return inches/2.54/100;
        }

        private string randomString(int strlen)
        {
            string ret = "";
            Random rand = new Random();
            for (int i = 0; i < strlen; i++)
            {
                int r = rand.Next(26) + 65;
                ret = ret + (char)r;
            }

            return ret;
        }

        private void lengthenExtrusion(Feature feature, Component2 swComponent, string configuration)
        {
            Random rand = new Random();
            String[] configurationContainer = { configuration };
            ExtrudeFeatureData2 extrusion = feature.GetDefinition();
            extrusion.SetDepth(true, inchesToMeters(rand.Next(20)));
            extrusion.SetChangeToConfigurations((int)swInConfigurationOpts_e.swThisConfiguration, configurationContainer);
            feature.ModifyDefinition(extrusion, swDoc, swComponent);
            swDoc.ForceRebuild3(true);
            swComponent.DeSelect();
        }

        public void TraverseFeatures(Feature swFeat, List<FeatureIdentifier> toModify)
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

        public void TraverseComponentFeatures(Component2 swComp, List<FeatureIdentifier> found)
        {
            Feature swFeat;
            swFeat = (Feature)swComp.FirstFeature();
            TraverseFeatures(swFeat, found);
            foreach (FeatureIdentifier fi in found)
            {
                if (fi.component == null)
                {
                    fi.component = swComp;
                }
            }
        }

        private void FindModifiable(Component2 swComp, List<FeatureIdentifier> found)
        {
            object[] vChildComp;
            Component2 swChildComp;
            int i;

            vChildComp = (object[])swComp.GetChildren();
            for (i = 0; i < vChildComp.Length; i++)
            {
                swChildComp = (Component2)vChildComp[i];
                if (swChildComp.Name2.StartsWith("button-4post"))
                {
                    TraverseComponentFeatures(swChildComp, found);
                }
            }
        }

        private string createAndSetConfiguration(Component2 swComponent)
        {
            string configName = "SAURON-" + randomString(15);
            swDoc.AddConfiguration2(configName, "automatically generated configuration", "", true, false, false, true, 256);

            // set that configuration to be active
            swComponent.ReferencedConfiguration = configName;

            return configName;
        }

        private void processFeature_Click(object sender, EventArgs e)
        {
            getModelDoc();
            List<FeatureIdentifier> found = new List<FeatureIdentifier>();

            // first traverse the tree, searching for features within "button-4post" called "flag", but in general
            FindModifiable(swConf.GetRootComponent3(true), found);
            
            foreach (FeatureIdentifier f in found) {
                string configuration = createAndSetConfiguration(f.component);
                lengthenExtrusion(f.feature, f.component, configuration);
            }
        }

        private void stuff()
        {
            // a place for me to copy/paste from macros and not hurt anything
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
            // This piece of code is good for getting the names of all the components in the document
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
        }

        private void testPart_Click(object sender, EventArgs e)
        {
            getModelDoc();

            List<FeatureIdentifier> found = new List<FeatureIdentifier>();

            // first traverse the tree, searching for features within "button-4post" called "flag", but in general
            FindModifiable(swConf.GetRootComponent3(true), found);

            foreach (FeatureIdentifier f in found)
            {
                string configuration = createAndSetConfiguration(f.component);
                lengthenExtrusion(f.feature, f.component, configuration);
            }
        }
    }
}
