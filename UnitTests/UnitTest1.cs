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
    }

    [TestClass]
    public class testDiffractionLossLib : DiffractionLossCalculator
    {
        private const int rounding = 7;

        [TestMethod]
        public void Case01()
        {
            double expectedLoss = 32.84284766;

            SetupLibrary(50, 8500, @".\Path1.csv");
            double TxAntennaeHeight = 30;
            double RxAntennaeHeight = 30;
            double frequency = 1000;

            double L_b = DoCalculations(TxAntennaeHeight, RxAntennaeHeight, frequency);
            
            Assert.AreEqual(Math.Round(expectedLoss, rounding), Math.Round(L_b, rounding));
        }

        [TestMethod]
        public void Case02()
        {
            double expectedLoss = 37.229036;

            SetupLibrary(50, 8500, @".\Path1.csv");
            double TxAntennaeHeight = 50;
            double RxAntennaeHeight = 10;
            double frequency = 2500;

            double L_b = DoCalculations(TxAntennaeHeight, RxAntennaeHeight, frequency);

            Assert.AreEqual(Math.Round(expectedLoss, rounding), Math.Round(L_b, rounding));
        }

        [TestMethod]
        public void Case10()
        {
            double expectedLoss = 17.62040631;

            SetupLibrary(50, 8500, @".\Path3.csv");
            double TxAntennaeHeight = 30;
            double RxAntennaeHeight = 30;
            double frequency = 1000;

            double L_b = DoCalculations(TxAntennaeHeight, RxAntennaeHeight, frequency);

            Assert.AreEqual(Math.Round(expectedLoss, rounding), Math.Round(L_b, rounding));
        }

        [TestMethod]
        public void Case16()
        {
            double expectedLoss = 7.870985135;

            SetupLibrary(50, 8500, @".\Path4.csv");
            double TxAntennaeHeight = 30;
            double RxAntennaeHeight = 30;
            double frequency = 1000;

            double L_b = DoCalculations(TxAntennaeHeight, RxAntennaeHeight, frequency);

            Assert.AreEqual(Math.Round(expectedLoss, rounding), Math.Round(L_b, rounding));
        }

        [TestMethod]
        public void Case17()
        {
            double expectedLoss = 0.0;

            SetupLibrary(50, 8500, @".\Path4.csv");
            double TxAntennaeHeight = 50;
            double RxAntennaeHeight = 10;
            double frequency = 2500;

            double L_b = DoCalculations(TxAntennaeHeight, RxAntennaeHeight, frequency);

            Assert.AreEqual(Math.Round(expectedLoss, rounding), Math.Round(L_b, rounding));
        }

        [TestMethod]
        public void Path5()
        {
            double expectedLoss = 1.52919291257699;
;

            SetupLibrary(50, 8500, @".\Path5.csv");
            double TxAntennaeHeight = 5;
            double RxAntennaeHeight = 5;
            double frequency = 1000;

            /* 
             * This is simple path I created to compute manually and debug my output
             * 
             * 
             * 
             */

            double L_b = DoCalculations(TxAntennaeHeight, RxAntennaeHeight, frequency);

            Assert.AreEqual(Math.Round(expectedLoss, rounding), Math.Round(L_b, rounding));
        }

        private static Point newPointFromCsv(string csvLine)
        {
            string[] values = csvLine.Split(',');
            Point p = new Point();

            p.height = Convert.ToInt32(values[1]);

            return p;
        }

        private void SetupLibrary(double gap, double earthRadius, string data)
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

    }
}
