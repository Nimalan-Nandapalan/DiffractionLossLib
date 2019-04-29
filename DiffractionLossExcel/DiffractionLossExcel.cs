using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ExcelDna.Integration;
using Excel = Microsoft.Office.Interop.Excel;

using DiffractionLossExcell.Helpers.ExcelDNA;

using Gavaghan.Geodesy;
using SRTM;

using DiffractionLossLib;

namespace DiffractionLossExcel
{
    public class DiffractionLossExcell
    {
        private static string foo { get; set; }
        private static DiffractionLossCalculator diffLossCalc;

        #region test functions
        [ExcelFunction(Description = "My first .NET function")]
        public static string SayHello(string name)
        {
            MessageBox.Show($"Hello {name}");

            return "Hello " + name;
        }

        [ExcelFunction(Description = "My first .NET function")]
        public static string FooA(string name)
        {
            MessageBox.Show($"foo is {foo}");

            foo = "potato";

            return foo;
        }

        [ExcelFunction(Description = "My first .NET function")]
        public static string FooB(string name)
        {
            foo = "not a Potato";

            return foo;
        }

        [ExcelFunction(Description = "Sets cell below to 'Potato'",  IsMacroType = true)]
        public static void Potato( [ExcelArgument(AllowReference = true)] object _arg)
        {
            Excel.Application app = (Excel.Application)ExcelDnaUtil.Application;
            Excel.Range range = app.ActiveCell;

            ExcelReference targetCell;

            targetCell = _arg as ExcelReference;
            //targetCell = new ExcelReference(range.Row - 1 + 1, range.Column - 1);

            ExcelAsyncUtil.QueueAsMacro( () => { targetCell.SetValue("Potato"); } );

            //return $"=Potato({targetCell.ToString()})";
            return;
        }
               
        [ExcelFunction(Description = "Array value function")]
        public static object MakeList()
        {
            object[,] list = new object[12,2];

            for (int i = 0; i < 12; i++)
            { 
                list[i, 0] = i;
                list[i, 1] = i * i;
            }

            return ArrayResizer.Resize(list);
        }
        #endregion

        #region DiffractionLossLib functions

        [ExcelFunction(Description = "Generate a list of points beteen two points at fixed distances")]
        public static object GetProfilePoints(double txLat, double txLon, double rxLat, double rxLon)
        {
            /* 
                Test data to paste into excel:
                    
                    start	-33.83953500	151.20694600
                    end	-33.87644513	151.22115513

                    =GetProfilePoints(B1,C1,B2,C2)

            */

            if (diffLossCalc == null)
                diffLossCalc = new DiffractionLossCalculator();

            GlobalCoordinates start = new GlobalCoordinates(new Angle(txLat), new Angle(txLon));
            GlobalCoordinates end = new GlobalCoordinates(new Angle(rxLat), new Angle(rxLon));

            var points = diffLossCalc.GenerateIntermediateProfilePoints(start, end);
            var pointsArray = new object[points.Count, 3];

            for (int i = 0; i < points.Count; i++)
            {
                pointsArray[i, 0] = points[i].coordinate.Latitude.Degrees;
                pointsArray[i, 1] = points[i].coordinate.Longitude.Degrees;
                pointsArray[i, 2] = points[i].height;
            }
            
            return ArrayResizer.Resize(pointsArray);
        }

        [ExcelFunction(Description = "Bullington Method")]
        public static double CalculateDiffractionLoss(double txLat, double txLon, double TxAntennaeHeight, double rxLat, double rxLon, double RxAntennaeHeight, double frequency)
        {
            if (diffLossCalc == null)
                diffLossCalc = new DiffractionLossCalculator();

            GlobalCoordinates start = new GlobalCoordinates(new Angle(txLat), new Angle(txLon));
            GlobalCoordinates end = new GlobalCoordinates(new Angle(rxLat), new Angle(rxLon));

            double result = diffLossCalc.CalculateDiffractionLoss(start, TxAntennaeHeight, end, RxAntennaeHeight, frequency);

            return result;
        }

        #endregion

        #region Geodetic functions
        [ExcelFunction(Description = "Get Azimuth between two points")]
        public static double GetAzimuth(double txLat, double txLon, double rxLat, double rxLon)
        {
            GeodeticCalculator geoCalc = new GeodeticCalculator();
            Ellipsoid reference = Ellipsoid.WGS84;

            GlobalCoordinates start = new GlobalCoordinates(new Angle(txLat), new Angle(txLon));
            GlobalCoordinates end = new GlobalCoordinates(new Angle(rxLat), new Angle(rxLon));

            GeodeticCurve path = geoCalc.CalculateGeodeticCurve(reference, start, end);

            return path.Azimuth.Degrees;
        }

        [ExcelFunction(Description = "Get distance between two points")]
        public static double GetDistance(double txLat, double txLon, double rxLat, double rxLon)
        {
            GeodeticCalculator geoCalc = new GeodeticCalculator();
            Ellipsoid reference = Ellipsoid.WGS84;

            GlobalCoordinates start = new GlobalCoordinates(new Angle(txLat), new Angle(txLon));
            GlobalCoordinates end = new GlobalCoordinates(new Angle(rxLat), new Angle(rxLon));

            GeodeticCurve path = geoCalc.CalculateGeodeticCurve(reference, start, end);

            return path.EllipsoidalDistance;
        }

        [ExcelFunction(Description = "Next Longitude based on Vincenty's Algorithm")]
        public static double NextLon(double lat, double lon, double az, double distance)
        {
            GeodeticCalculator geoCalc = new GeodeticCalculator();
            Ellipsoid reference = Ellipsoid.WGS84;

            GlobalCoordinates start = new GlobalCoordinates(new Angle(lat), new Angle(lon));
            Angle azimuth = new Angle(az);

            Angle endBearing;

            GlobalCoordinates dest = geoCalc.CalculateEndingGlobalCoordinates(reference, start, azimuth, distance, out endBearing);

            return dest.Longitude.Degrees;
        }

        [ExcelFunction(Description = "Next Latitude based on Vincenty's Algorithm")]
        public static double NextLat(double lat, double lon, double az, double distance)
        {
            GeodeticCalculator geoCalc = new GeodeticCalculator();
            Ellipsoid reference = Ellipsoid.WGS84;

            GlobalCoordinates start = new GlobalCoordinates(new Angle(lat), new Angle(lon));
            Angle azimuth = new Angle(az);

            Angle endBearing;

            GlobalCoordinates dest = geoCalc.CalculateEndingGlobalCoordinates(reference, start, azimuth, distance, out endBearing);

            return dest.Latitude.Degrees;
        }

        [ExcelFunction(Description = "Next Coordinates based on Vincenty's Algorithm")]
        public static object NextPoint(double lat, double lon, double az, double distance)
        {
            GeodeticCalculator geoCalc = new GeodeticCalculator();
            Ellipsoid reference = Ellipsoid.WGS84;

            GlobalCoordinates start = new GlobalCoordinates(new Angle(lat), new Angle(lon));
            Angle azimuth = new Angle(az);

            Angle endBearing;

            GlobalCoordinates dest = geoCalc.CalculateEndingGlobalCoordinates(reference, start, azimuth, distance, out endBearing);

            var point = new object[1, 2];

            point[0, 0] = dest.Latitude.Degrees;
            point[0, 1] = dest.Longitude.Degrees;

            return ArrayResizer.Resize(point);
        }

        #endregion

        #region SRTM functions
        [ExcelFunction(Description = "Get height from SRTM data")]
        public static int GetHeight(double lat, double lon, string path)
        {
            var srtmData = new SRTMData(path);

            return srtmData.GetElevation(lat, lon) ?? -1;
        }
        #endregion  

    }
}
