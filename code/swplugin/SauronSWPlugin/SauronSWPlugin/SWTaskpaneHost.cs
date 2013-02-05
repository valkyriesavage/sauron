using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System.Runtime.InteropServices;

//using Bespoke.Common;

namespace SauronSWPlugin
{
    [ComVisible(true)]
    [ProgId(SWTASKPANE_PROGID)]
    public partial class SWTaskpaneHost : UserControl
    {
        public const string SWTASKPANE_PROGID = "Sauron.SWTaskPane_SwPlugin";
        public SldWorks swApp;
        public ModelDoc2 swDoc = null;

        public bool testing = false;
        public bool registering = false;

        private String[] ourComponents = {"button-4post","dial","joystick-all-pieces","slider","scroll-wheel","dpad"};

        public SWTaskpaneHost()
        {
            InitializeComponent();
        }

        private void getModelDoc() {
            swDoc = ((ModelDoc2)(swApp.ActiveDoc));
        }

        private double inchesToMeters(double inches)
        {
            return inches/2.54/100;
        }

        private void processFeature_Click(object sender, EventArgs e)
        {
            getModelDoc();

            // this ideally creates a configuration of the part... so that we can modify it without clobbering
            // all the other instances of the same part
            bool boolstatus = swDoc.Extension.SelectByID2("button-4post.SLDPRT", "COMPONENT", 0, 0, 0, false, 0, null, 0);
            boolstatus = swDoc.AddConfiguration2("button-4post-somenamething", "", "", true, false, false, true, 256);
            
            // this piece of code will draw a circle at the center of the selected plane
            bool updateEditRebuild = true;
            SketchPoint refPoint = swDoc.FeatureManager.InsertReferencePoint((int)swRefPointType_e.swRefPointFaceCenter,
                                                                             (int)swRefPointAlongCurveType_e.swRefPointAlongCurveEvenlyDistributed,
                                                                             0,
                                                                             1);
            swDoc.SketchManager.InsertSketch(updateEditRebuild);
            double diameter = inchesToMeters(0.05);
            swDoc.SketchManager.CreateCircle(refPoint.X, refPoint.Y, refPoint.Z,
                                             refPoint.X + diameter, refPoint.Y + diameter, refPoint.Z);

            // this piece of code will theoretically extrude the circle...
            bool singleEnded = true;
            bool flipSideToCut = false;
            bool flipDirectionExtrude = false;
            int endSideOne = (int)swEndConditions_e.swEndCondThroughNext;
            int endSideTwo = (int)swEndConditions_e.swEndCondBlind;
            double depthSideOne = inchesToMeters(3.5);
            double depthSideTwo = 0;
            bool draftSideOne = false;
            bool draftSideTwo = false;
            bool draftDirSideOne = false;
            bool draftDirSideTwo = false;
            double draftAngleSideOne = 0;
            double draftAngleSideTwo = 0;
            bool offsetReverseOne = false;
            bool offsetReverseTwo = false;
            bool translateSurfaceOne = false;
            bool translateSurfaceTwo = false;
            bool merge = true;
            bool useScope = true;
            bool useAutoSelect = true;
            int startCondition = (int)swStartConditions_e.swStartOffset;
            double offset = 0;
            bool flipOffset = false;

            Feature extrusion = swDoc.FeatureManager.FeatureExtrusion2(singleEnded, flipSideToCut, flipDirectionExtrude,
                                                                       endSideOne, endSideTwo, depthSideOne, depthSideTwo,
                                                                       draftSideOne, draftSideTwo, draftDirSideOne, draftDirSideTwo, draftAngleSideOne, draftAngleSideTwo,
                                                                       offsetReverseOne, offsetReverseTwo,
                                                                       translateSurfaceOne, translateSurfaceTwo,
                                                                       merge, useScope, useAutoSelect, startCondition, offset, flipOffset);


            // this is copy/pasted from a recorded macro which creates an extrusion at the center of the bottom of the button
            // and then fillets it
            boolstatus = swDoc.Extension.SelectByID2("", "FACE", 0.0036873572009881173, -0.011430000000018481, 0.00042013598232415461, false, 0, null, 0);
            swDoc.SketchManager.InsertSketch(true);
            swDoc.ClearSelection2(true);
            SketchSegment skSegment = null;
            skSegment = ((SketchSegment)(swDoc.SketchManager.CreateCircle(0, 0, 0, -0.001332, -0.002868, 0)));
            swDoc.ClearSelection2(true);
            swDoc.SketchManager.InsertSketch(true);
            swDoc.SketchManager.InsertSketch(true);
            swDoc.ClearSelection2(true);
            boolstatus = swDoc.Extension.SelectByID2("Sketch5", "SKETCH", 0, 0, 0, false, 0, null, 0);
            Feature myFeature = null;
            myFeature = ((Feature)(swDoc.FeatureManager.FeatureExtrusion2(true, false, false, 0, 0, 0.0127, 0.0127, false, false, false, false, 0.017453292519943334, 0.017453292519943334, false, false, false, false, true, true, true, 0, 0, false)));
            swDoc.ISelectionManager.EnableContourSelection = false;
            boolstatus = swDoc.DeSelectByID("Boss-Extrude2", "BODYFEATURE", 0, 0, 0);
            boolstatus = swDoc.Extension.SelectByID2("", "EDGE", -0.0013848236446278861, -0.011469979298169619, -0.0021007600423104122, true, 1, null, 0);
            swDoc.ClearSelection2(true);
            boolstatus = swDoc.Extension.SelectByID2("", "EDGE", -0.0013848236446278861, -0.011469979298169619, -0.0021007600423104122, false, 1, null, 0);
            Array radiiArray0 = null;
            double[] radiis0 = new double[1];
            Array setBackArray0 = null;
            double[] setBacks0 = new double[0];
            Array pointArray0 = null;
            double[] points0 = new double[0];
            radiiArray0 = radiis0;
            setBackArray0 = setBacks0;
            pointArray0 = points0;
            myFeature = ((Feature)(swDoc.FeatureManager.FeatureFillet(195, 0.00254, 0, 0, radiiArray0, setBackArray0, pointArray0)));
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
            }
            else
            {
                testing = true;
                test_mode.Text = exit_test;
            }
        }

        private void register_Click(object sender, EventArgs e)
        {
            getModelDoc();
            String exit_registration = "done registering";
            String enter_registration = "register on print";
            if (registering)
            {
                registering = false;
                register.Text = enter_registration;
            }
            else
            {
                registering = true;
                register.Text = exit_registration;
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
                            // how do we get the actual part once we have the name?
                            //bool boolstatus = swDoc.Extension.SelectByID2("button-4post.SLDPRT", "COMPONENT", 0, 0, 0, false, 0, null, 0);
                        }
                    }
                }
            }
            
            /*if (swApp.ActiveDoc != null) // && swApp.ActiveDoc.IS AN ASSEMBLY? )
            {
                int index = 0;
                int ignoreMarks = -1;
                Feature selected = swApp.ActiveDoc.SelectionManager.getSelectedObject6(index, ignoreMarks);
                swApp.SendMsgToUser2(selected.Name, (int)swMessageBoxIcon_e.swMbInformation, (int)swMessageBoxBtn_e.swMbOk);
            }*/
        }

        private void insert_camera_Click(object sender, EventArgs e)
        {
            getModelDoc();
        }
    }
}
