using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using SRTM;
using Gavaghan.Geodesy;
using DiffractionLossLib;

namespace UnitTests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var srtmData = new SRTMData(@"C:\temp\srtm-cache");

            int expected = 136;

            // -33.47330 151.31728
            int? result = srtmData.GetElevation(-33.47330, 151.31728);

            Assert.AreEqual(expected, result);
        }

        [TestMethod]
        public void DebugHeight()
        {
            var srtmData = new SRTMData(@"C:\temp\srtm-cache");

            // -38.26192626 144.6653395
            int? DebugHeight  = srtmData.GetElevation(-38.26192626, 144.6653395);


            int expected = 136;

            // -33.47330 151.31728
            int? result = srtmData.GetElevation(-33.47330, 151.31728);
            
            Assert.AreEqual(expected, result);
        }
    }

    [TestClass]
    public class testDiffractionLossLib : DiffractionLossCalculator
    {
        [TestMethod]
        public void TestFixHeights()
        {
            distanceBetweenPoints = 100.0;
            List<Point> points = new List<Point>();
            points.Add(new Point(0.0, 0.0, 65535));
            points.Add(new Point(0.0, 0.0, 65535));
            points.Add(new Point(0.0, 0.0, 10));
            points.Add(new Point(0.0, 0.0, 20));
            points.Add(new Point(0.0, 0.0, 65535));
            points.Add(new Point(0.0, 0.0, 65535));
            points.Add(new Point(0.0, 0.0, 50));
            points.Add(new Point(0.0, 0.0, 60));
            points.Add(new Point(0.0, 0.0, 65535));

            double[] expected = new double[] { 10, 10, 10, 20, 30, 40, 50, 60, 60 };

            FixHeights(ref points);

            for (int i = 0; i < points.Count; i++)
            {
                Assert.AreEqual(expected[i], points[i].height);
            }

        }

        [TestMethod]
        public void Case01()
        {
            Validate(50, 8500,
                @".\Path1.csv", 30, 30, 1000,
                32.8428476648279, 13.7484048434102, 15.3485837014164, 34.4430265228342); 
        }

        [TestMethod]
        public void Case02()
        {
            Validate(50, 8500,
                @".\Path1.csv", 50, 10, 2500,
                37.229036000185, 18.5970600965698, 23.69730690348, 42.3292828070952);
        }

        [TestMethod]
        public void Case03()
        {
            Validate(50, 8500,
                @".\Path1.csv", 20, 20, 600,
                31.2259581326557, 15.7937377674364, 20.9138486399512, 36.3460690051704);
        }

        [TestMethod]
        public void Case04()
        {
            // This test case is currently expected to fail
            Validate(50, 8500,
                @".\Path1.csv", 40, 50, 200,
                24.877926126549, 11.509818574572, 14.3293364079598, 27.6974439599368);
        }

        [TestMethod]
        public void Case05()
        {
            Validate(50, 8500,
                @".\Path1.csv", 70, 5, 150,
                24.7822392826581, 14.9303334670415, 33.1388369270132, 42.9907427426298);
        }

        [TestMethod]
        public void Case06()
        {
            Validate(50, 8500,
                @".\Path2.csv", 30, 30, 1000,
                36.2041930451087, 35.8281650880508, 69.4543415125398, 69.8303694695977);
        }

        [TestMethod]
        public void Case07()
        {
            Validate(50, 8500,
                @".\Path2.csv", 50, 10, 2500,
                44.8124092793815, 39.6687193449871, 85.5386374926075, 90.6823274270018);
        }

        [TestMethod]
        public void Case08()
        {
            Validate(50, 8500,
                @".\Path2.csv", 20, 20, 600,
                34.4391793874178, 34.2950306805613, 66.3164765135772, 66.4606252204338);
        }

        [TestMethod]
        public void Case09()
        {
            Validate(50, 8500,
                @".\Path2.csv", 40, 50, 200,
                28.3736814899882, 27.7400956765977, 45.7547347972686, 46.3883206106591);
        }

        [TestMethod]
        public void Case10()
        {
            // This test case is currently expected to fail
            Validate(50, 8500,
                @".\Path2.csv", 70, 5, 150,
                34.6174603249337, 26.5855999722714, 43.3536183901149, 51.3854787427772);
        }

        [TestMethod]
        public void Case11()
        {
            Validate(50, 8500,
                @".\Path3.csv", 30, 30, 1000,
                17.6204063067208, 0.0, 0.0, 17.6204063067208);
        }

        [TestMethod]
        public void Case12()
        {
            Validate(50, 8500,
                @".\Path3.csv", 50, 10, 2500,
                25.101690696411, 0.0, 0.0, 25.101690696411);
        }

        [TestMethod]
        public void Case13()
        {
            Validate(50, 8500,
                @".\Path3.csv", 20, 20, 600,
                20.254887328581, 0.0, 0.0, 20.254887328581);
        }

        [TestMethod]
        public void Case14()
        {
            Validate(50, 8500,
                @".\Path3.csv", 40, 50, 200,
                10.1899084952214, 0.0, 0.0, 10.1899084952214);
        }

        [TestMethod]
        public void Case15()
        {
            // This test case is currently expected to fail
            Validate(50, 8500,
                @".\Path3.csv", 70, 5, 150,
                16.2759310648717, 6.17409080988023, 10.6376522203642, 20.7394924753557);
        }

        [TestMethod]
        public void Case16()
        {
            Validate(50, 8500,
                @".\Path4.csv", 30, 30, 1000,
                7.8709851349471, 0.0, 0.0, 7.8709851349471);
        }

        [TestMethod]
        public void Case17()
        {
            Validate(50, 8500,
                @".\Path4.csv", 50, 10, 2500,
                0.0, 0.0, 0.0, 0.0);
        }

        [TestMethod]
        public void Case18()
        {
            // This test case is currently expected to fail
            Validate(50, 8500,
                @".\Path4.csv", 20, 20, 600,
                15.3087639746324, 0.0, 0.0, 15.3087639746324);
        }

        [TestMethod]
        public void Case19()
        {
            Validate(50, 8500,
                @".\Path4.csv", 40, 50, 200,
                6.5267304425924, 0.0, 0.0, 6.5267304425924);
        }

        [TestMethod]
        public void Case20()
        {
            Validate(50, 8500,
                @".\Path4.csv", 70, 5, 150,
                0.0, 0.0, 0.0, 0.0);
        }

        [TestMethod]
        public void Path5()
        {
            double expectedLoss = 1.52919291257699;
;
            List<Point> points = new List<Point>();
            SetupLibrary(50, 8500, @".\Path5.csv", ref points);
            double TxAntennaeHeight = 5;
            double RxAntennaeHeight = 5;
            double frequency = 1000;

            double L_ba = CalculateActualTerrain(points, TxAntennaeHeight, RxAntennaeHeight, frequency);

            double h_mts;
            double h_mrs;
            double L_bs = CalculateSmoothProfile(points, TxAntennaeHeight, RxAntennaeHeight, frequency, out h_mts, out h_mrs);

            double L_sph = CalculateSphericalEarth(h_mts, h_mrs, frequency);

            double L = CalculateLoss(L_ba, L_bs, L_sph);

            Assert.IsTrue(InTolerance(expectedLoss, L_ba, Math.Pow(10, -7)));
        }

        
        private void Validate(double distanceBetweenPoints, double effectiveEarthRadius,
            string pathData, double TxAntennaeHeight, double RxAntennaeHeight, double frequency,
            double expectedL_ba, double expectedL_bs, double expectedL_sph, double expectedL)
        {             
            List<Point> points = new List<Point>();
            SetupLibrary(distanceBetweenPoints, effectiveEarthRadius, pathData, ref points);

            double L_ba = CalculateActualTerrain(points, TxAntennaeHeight, RxAntennaeHeight, frequency);

            double h_mts;
            double h_mrs;
            double L_bs = CalculateSmoothProfile(points, TxAntennaeHeight, RxAntennaeHeight, frequency, out h_mts, out h_mrs);

            double L_sph = CalculateSphericalEarth(h_mts, h_mrs, frequency);

            double L = CalculateLoss(L_ba, L_bs, L_sph);

            Assert.IsTrue(InTolerance(expectedL_ba, L_ba, Math.Pow(10, -7)));
            Assert.IsTrue(InTolerance(expectedL_bs, L_bs, Math.Pow(10, -7)));
            Assert.IsTrue(InTolerance(expectedL_sph, -L_sph, Math.Pow(10, -2)));
            Assert.IsTrue(InTolerance(expectedL, L, 2));
        }

        private static Point newPointFromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            Point p = new Point();

            p.height = Convert.ToInt32(values[1]);

            return p;
        }

        private void SetupLibrary(double gap, double earthRadius, string data, ref List<Point> points)
        {
            UseDefaultValues();
            mode = LibraryMode.Test;
            geoCalc = new GeodeticCalculator();
            distanceBetweenPoints = gap;
            effectiveEarthRadius = earthRadius;

            var csv = File.ReadAllLines(data);

            points = csv
                        .Skip(1)
                        .Select(p => newPointFromCsv(p))
                        .ToList();

            double d = Convert.ToDouble(csv.Last().Split(',')[0]) * 1000.0;

            path = new GeodeticCurve(d, new Angle(0.0), new Angle(180.0));

            double d_secondLast = Convert.ToDouble(csv[csv.Length - 2].Split(',')[0]) * 1000.0;

            distanceBetweenLastPoints = d - d_secondLast;
        }

        private bool InTolerance(double expected, double actual, double tolerance)
        {
            return (actual > expected - tolerance && actual < expected + tolerance) ? true : false;
        }

    }
}
