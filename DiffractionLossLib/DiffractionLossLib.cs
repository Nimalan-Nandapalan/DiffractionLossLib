using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Gavaghan.Geodesy;
using SRTM;

namespace DiffractionLossLib
{
    class DiffractionLossCalculator
    {
        private Point[] points;

        private GeodeticCalculator geoCalc;
        private Ellipsoid ellipsoid;

        private SRTMData srtmData;

        public DiffractionLossCalculator(Ellipsoid _ellipsoid, string _srtmCacheLocation)
        {
            geoCalc = new GeodeticCalculator();
            ellipsoid = _ellipsoid;

            srtmData = new SRTMData(_srtmCacheLocation);
        }

        // distance between points in meters
        public Point[] CalculateIntermediatePoints(GlobalCoordinates start, GlobalCoordinates end, double distanceBetweenPoints)
        {
            var startHeight = srtmData.GetElevation(start.Latitude.Degrees, start.Longitude.Degrees);
            points.Append(new Point(start, startHeight));

            GeodeticCurve path = geoCalc.CalculateGeodeticCurve(ellipsoid, start, end);
            //path.Azimuth.Degrees;

            Angle endBearing;

            GlobalCoordinates dest = geoCalc.CalculateEndingGlobalCoordinates(reference, start, azimuth, distance, out endBearing);

            return dest.Longitude.Degrees;

            return points;
        }

    
    }
}
