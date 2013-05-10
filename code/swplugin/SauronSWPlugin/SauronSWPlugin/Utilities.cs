using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System.Runtime.InteropServices;

namespace SauronSWPlugin
{
    class Utilities
    {
        public static SldWorks swApp;
        public static ModelDoc2 swDoc = null;
        public static AssemblyDoc swAssembly = null;
        public static SelectionMgr swSelectionMgr = null;
        public static ConfigurationManager swConfMgr = null;
        public static FeatureManager swFeatureMgr = null;
        public static Configuration swConf = null;
        public static ISketchManager swSketchMgr = null;
        public static Measure measure = null;
        public static MathUtility mathUtils = null;

        public static double inchesToMeters(double inches)
        {
            return inches * 2.54 / 100;
        }

        public static double metersToInches(double meters)
        {
            return meters * 100 / 2.54;
        }

        public static string randomString()
        {
            return randomString(15);
        }

        public static string randomString(int strlen)
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

        public static string stripIdentifier(string partname)
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

        public static string stringify(double[] someArray)
        {
            string ret = "";
            for (int i = 0; i < someArray.Length; i++)
            {
                ret += i + ",";
            }
            ret = ret.Remove(ret.Length - 1);
            return ret;
        }

        public static double distanceFormula(double[] pt1, double[] pt2)
        {
            double total = 0;
            for (int i = 0; i < Math.Min(pt1.Length, pt2.Length); i++)
            {
                total += Math.Pow(pt1[i] - pt2[i], 2);
            }

            return Math.Sqrt(total);
        }

        public static IMathPoint getNamedPoint(string name, Component2 parent)
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

        public static double distanceBetween(Component2 component, Component2 otherComponent)
        {
            component.Select(false);
            otherComponent.Select(true);
            measure.Calculate(null);
            return metersToInches(measure.Distance);
        }

        private double[] putInComponentSpace(ReflectionPoint rp, Component2 component)
        {
            // TODO : debug!!
            MathTransform invertMainBodyXForm = component.Transform2.Inverse();
            return rp.location.IMultiplyTransform(invertMainBodyXForm).ArrayData;
        }

        public static bool isBody(Component2 swComponent)
        {
            object bodiesInfo = null;
            swComponent.GetBodies3((int)swBodyType_e.swSolidBody, out bodiesInfo);
            return bodiesInfo != null && ((int[])bodiesInfo).Length > 0;
        }

        public static void alert(string text)
        {
            swApp.SendMsgToUser2(text, 1, 1);
        }
    }
}
