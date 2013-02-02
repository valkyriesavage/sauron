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

namespace SauronSWPlugin
{
    [ComVisible(true)]
    [ProgId(SWTASKPANE_PROGID)]
    public partial class SWTaskpaneHost : UserControl
    {
        public const string SWTASKPANE_PROGID = "Sauron.SWTaskPane_SwPlugin";
        public SldWorks swApp;

        public bool testing = false;
        public bool registering = false;

        public SWTaskpaneHost()
        {
            InitializeComponent();
        }

        private double inchesToMeters(double inches)
        {
            return inches/2.54/100;
        }

        private void processFeature_Click(object sender, EventArgs e)
        {
            ModelDoc2 swDoc = null;
            bool updateEditRebuild = true;
            swDoc = ((ModelDoc2)(swApp.ActiveDoc));
            SketchPoint refPoint = swDoc.FeatureManager.InsertReferencePoint((int)swRefPointType_e.swRefPointFaceCenter,
                                                                             (int)swRefPointAlongCurveType_e.swRefPointAlongCurveEvenlyDistributed,
                                                                             0,
                                                                             1);
            swDoc.SketchManager.InsertSketch(updateEditRebuild);
            double diameter = inchesToMeters(0.05);
            swDoc.SketchManager.CreateCircle(refPoint.X, refPoint.Y, refPoint.Z,
                                             refPoint.X + diameter, refPoint.Y + diameter, refPoint.Z);
            bool singleEnded = true;
            bool flipSideToCut = false;
            bool flipDirectionExtrude = false;
            int endSideOne = (int)swEndConditions_e.swEndCondBlind;
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


            swDoc.FeatureManager.FeatureExtrusion2(singleEnded, flipSideToCut, flipDirectionExtrude,
                                                   endSideOne, endSideTwo, depthSideOne, depthSideTwo,
                                                   draftSideOne, draftSideTwo, draftDirSideOne, draftDirSideTwo, draftAngleSideOne, draftAngleSideTwo,
                                                   offsetReverseOne, offsetReverseTwo,
                                                   translateSurfaceOne, translateSurfaceTwo,
                                                   merge, true, true, 0, 0, false);
        
        }

        private void test_mode_Click(object sender, EventArgs e)
        {
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
            String exit_registration = "done registering";
            String enter_registration = "register on print";
            if (registering)
            {
                registering = false;
                test_mode.Text = enter_registration;
            }
            else
            {
                registering = true;
                test_mode.Text = exit_registration;
            }
        }

    }
}
