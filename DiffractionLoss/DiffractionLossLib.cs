using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ExcelDna.Integration;
using Excel = Microsoft.Office.Interop.Excel;

using Gavaghan.Geodesy;
using SRTM;

namespace DiffractionLossLib
{
    public class DiffractionLoss
    {
        private GeodeticCalculator geoCalc;
        private bool recomputeRequired;

        [ExcelFunction(Description = "My first .NET function")]
        public static string SayHello(string name)
        {
            MessageBox.Show("Hello " + name);

            return "Hello " + name;
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
               
        [ExcelFunction(Description = "Array value function", IsMacroType = true))]
        public static double[,] MakeList()//object _list)
        {
            //var list = _list as double[];
            double[,] list = new double[2,12];

            //for (var i = 0; i < list.Length; i++)
            for (int i = 0; i < 12; i++)
            {
                //list[i] = list[i] * list[i]; 
                list[0, i] = i;
                list[1, i] = i * i;
            }

            var output = XlCall.Excel(XlCall.xlUDF, "Resize", list);

            return list;
        }

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

        [ExcelFunction(Description = "Get height from SRTM data")]
        public static int GetHeight(double lat, double lon)
        {
            var srtmData = new SRTMData(@"C:\temp\srtm-cache");

            return srtmData.GetElevation(lat, lon) ?? -1;
        }

    }
}
