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
        GlobalCoordinates coordinate;
        int? height;


        public Point()
        {
            coordinate = new GlobalCoordinates();
            height = null;
        }

        public Point(GlobalCoordinates _coordinate, int? _height)
        {
            GlobalCoordinates coordinate = _coordinate;
            height = _height;
        }

        public Point(double lat, double lon, int? _height)
        {
            GlobalCoordinates coordinate = new GlobalCoordinates(new Angle(lat), new Angle(lon));
            height = _height;
        }
    }
}
