using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Gavaghan.Geodesy;

namespace DiffractionLossLib
{
    public class Point
    {
        public GlobalCoordinates coordinate;
        public int? height;


        public Point()
        {
            coordinate = new GlobalCoordinates();
            height = null;
        }

        public Point(GlobalCoordinates _coordinate, int? _height)
        {
            coordinate = new GlobalCoordinates(_coordinate.Latitude, _coordinate.Longitude);
            height = _height;
        }

        public Point(double lat, double lon, int? _height)
        {
            coordinate = new GlobalCoordinates(new Angle(lat), new Angle(lon));
            height = _height;
        }
    }
}
