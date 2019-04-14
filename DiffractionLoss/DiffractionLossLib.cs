using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using ExcelDna.Integration;

using Gavaghan.Geodesy;
using SRTM;

namespace DiffractionLossLib
{
    public class DiffractionLoss
    {
        [ExcelFunction(Description = "My first .NET function")]
        public static string SayHello(string name)
        {
            MessageBox.Show("Hello " + name);

            return "Hello " + name;
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
