using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swcommands;
using SolidWorks.Interop.swconst;
using SolidWorks.Interop.swpublished;
using SolidWorksTools;
using System.Runtime.InteropServices;

namespace SauronSWPlugin
{
    class RayTracer
    {
        public static SldWorks swApp;
        public static ModelDoc2 swDoc = null;
        public static AssemblyDoc swAssembly = null;
        public static SelectionMgr swSelectionMgr = null;
        public static ISketchManager swSketchMgr = null;
        public static Measure measure = null;
        public static MathUtility mathUtils = null;

        public static Component2 mainBody = null;
        public static List<ComponentIdentifier> ourComponents = null;
        public static Camera camera = null;
        public static Simulator simulator = null;

        public static bool visualize = false;

        public static double distanceAlongRay(double[] rayOrigin, double[] xyz)
        {
            return Math.Sqrt(Math.Pow(rayOrigin[0] - xyz[0], 2) + Math.Pow(rayOrigin[1] - xyz[1], 2) + Math.Pow(rayOrigin[2] - xyz[2], 2));
        }

        public static bool hitMainBodyBefore(double[] rayOrigin, double[] rayDirection, double[] hitPointToCompareTo)
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

        public static bool hitSomethingElseBefore(double[] rayOrigin, double[] rayDirection, double[] hitPointToCompareTo)
        {
            foreach (ComponentIdentifier ci in ourComponents)
            {
                double[] someHit = whereRayHitsComponent(ci.component, mathUtils.CreatePoint(rayOrigin), mathUtils.CreateVector(rayDirection));
                if (someHit == null)
                {
                    continue;
                }

                if (distanceAlongRay(rayOrigin, someHit) < distanceAlongRay(rayOrigin, hitPointToCompareTo))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool rayHitsComponent(Component2 component, MathPoint rayOrigin, MathVector rayDirection)
        {
            return whereRayHitsComponent(component, rayOrigin, rayDirection) != null;
        }

        public static double[] whereRayHitsComponent(Component2 component, MathPoint rayOrigin, MathVector rayDirection)
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

        public static ReflectionPoint reflectedRayCanSeeComponent(Component2 component)
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

            double[] rayOrigins = camera.rayVectorOrigins();
            double[] rayDirections = camera.rayVectorDirections();

            int numIntersectionsFound = (int)swDoc.RayIntersections((object)bodies.ToArray(),
                                                                    (object)rayOrigins,
                                                                    (object)rayDirections,
                                                                    (int)(swRayPtsOpts_e.swRayPtsOptsTOPOLS | swRayPtsOpts_e.swRayPtsOptsNORMALS | swRayPtsOpts_e.swRayPtsOptsENTRY_EXIT),
                                                                    (double).0000001,
                                                                    (double).0000001);

            double[] horrifyingReturn = (double[])swDoc.GetRayIntersectionsPoints();

            if (numIntersectionsFound == 0)
            {
                return null;
            }

            int lengthOfOneReturn = 9;

            ReflectionPoint reflectionWorksAt = null;

            if (visualize)
            {
                startSketch();
            }

            for (int i = 0; i < numIntersectionsFound; i++)
            {
                byte intersectionType = (byte)horrifyingReturn[i * lengthOfOneReturn + 2];

                if ((intersectionType & (byte)swRayPtsResults_e.swRayPtsResultsENTER) == 0)
                {
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

                ReflectionPoint rp = new ReflectionPoint(mathUtils, new double[] { x, y, z }, new double[] { nx, ny, nz }, component);
                double[] reflectionDir = camera.calculateReflectionDir(camera.rayVectors().ElementAt(rayIndex).ArrayData, rp.nxnynz);
                MathVector reflectedRay = mathUtils.CreateVector(reflectionDir);

                if (rayHitsComponent(component, rp.location, reflectedRay))
                {
                    swDoc.SketchManager.CreatePoint(x, y, z);
                    if (visualize)
                    {
                        visualizeRay(camera.centreOfVision, mathUtils.CreateVector(new double[] { rayDirections[rayIndex * 3], rayDirections[rayIndex * 3 + 1], rayDirections[rayIndex * 3 + 2] }));
                        visualizeRay(rp.location, reflectedRay);
                    }
                    reflectionWorksAt = rp;
                }
            }

            if (visualize)
            {
                finishSketch("reflections for " + component.Name2);
            }

            return reflectionWorksAt;
        }

        public static void visualizeRay(MathVector ray, IMathPoint origin)
        {
            double[] rayData = (double[])ray.ArrayData;
            double[] originData = (double[])origin.ArrayData;

            swDoc.CreateLine2(originData[0], originData[1], originData[2],
                              originData[0] + rayData[0], originData[1] + rayData[1], originData[2] + rayData[2]);
        }

        public static void visualizeRay(IMathPoint origin, MathVector ray)
        {
            visualizeRay(ray, origin);
        }

        public static void visualizePoint(IMathPoint point)
        {
            double[] pointData = (double[])point.ArrayData;

            swDoc.CreatePoint2(pointData[0], pointData[1], pointData[2]);
        }

        public static void startSketch()
        {
            swDoc.Insert3DSketch2(true);
            swDoc.SketchManager.AddToDB = true;
            swDoc.SetDisplayWhenAdded(false);
        }

        public static void finishSketch()
        {
            finishSketch("SAURON-" + Utilities.randomString());
        }

        public static void finishSketch(string name)
        {
            swDoc.SketchManager.AddToDB = false;
            swDoc.Insert3DSketch2(true);
            swDoc.SetDisplayWhenAdded(true);

            Feature sketch = swSelectionMgr.GetSelectedObject6(1, 0);
            if (sketch == null)
            {
                // we didn't actually put anything in the sketch
                return;
            }
            sketch.Name = name;
            swDoc.ClearSelection2(true);
        }

        public static bool quickCheck(Component2 component)
        {
            IMathPoint source = Utilities.getNamedPoint("centre of flag top", component);
            if (source == null)
            {
                // then we need to pick some other kind of point... we'll try....
                Feature origin = component.FirstFeature();
                while (origin != null)
                {
                    if (origin.GetTypeName().Equals("OriginProfileFeature"))
                    {
                        origin.Select(false);
                        Measure measure = swDoc.Extension.CreateMeasure();
                        measure.Calculate(null);
                        source = mathUtils.CreatePoint(new double[] { measure.X, measure.Y, measure.Z });
                    }
                    origin = origin.GetNextFeature();
                }
            }
            return camera.isRayWithinFOV(source);
        }

        public static bool rawRaysSeeComponentInCurrentConfig(Component2 component)
        {
            /*
             * see https://forum.solidworks.com/message/348151
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
                    if (visualize)
                    {
                        startSketch();
                        visualizeRay(mathUtils.CreateVector(rayDirection), mathUtils.CreatePoint(rayOrigin));
                        finishSketch();
                    }
                    return true;
                }
            }

            return false;
        }

        public static void findComponentsVisible()
        {
            foreach (ComponentIdentifier ci in ourComponents)
            {
                ci.visibleToRawRays = quickCheck(ci.component);
            }
        }
    }
}
