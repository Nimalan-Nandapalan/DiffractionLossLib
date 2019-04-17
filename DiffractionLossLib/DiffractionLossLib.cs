using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Gavaghan.Geodesy;
using SRTM;

namespace DiffractionLossLib
{
    public class DiffractionLossCalculator
    {
        private Configuration config;

        private List<Point> points;

        private GeodeticCalculator geoCalc;
        private Ellipsoid ellipsoid;
        private double distanceBetweenPoints;

        private SRTMData srtmData;

        private const double defaultDistanceBetweenPoints = 100;
        private Ellipsoid defaultEllipsoid = Ellipsoid.WGS84;
        private const string defaultSrtmCache = @"C:\temp\srtm-cache";

        public DiffractionLossCalculator()
        {
            geoCalc = new GeodeticCalculator();

            points = new List<Point>();

            try
            {
                config = ConfigurationManager.OpenExeConfiguration(this.GetType().Assembly.Location);
            }
            catch (Exception ex)
            {
                throw ex;
            }

            if (config != null)
            {
                KeyValueConfigurationElement element;

                element = config.AppSettings.Settings["distanceBetweenPoints"];
                if (element != null)
                {
                    try
                    {
                        distanceBetweenPoints = double.Parse(element.Value);
                    }
                    catch
                    {
                        distanceBetweenPoints = defaultDistanceBetweenPoints;
                    }
                }

                element = config.AppSettings.Settings["srtmCache"];
                if (element != null)
                {
                    try
                    {
                        srtmData = new SRTMData(element.Value); 
                    }
                    catch
                    {

                        srtmData = new SRTMData(defaultSrtmCache);
                    }
                }
                
                element = config.AppSettings.Settings["ellipsoid"];
                if (element != null)
                {
                    try
                    {
                        switch (element.Value)
                        {
                            case "WGS84":
                                ellipsoid = Ellipsoid.WGS84;
                                break;
                            case "GRS80":
                                ellipsoid = Ellipsoid.GRS80;
                                break;
                            case "GRS67":
                                ellipsoid = Ellipsoid.GRS67;
                                break;
                            case "ANS":
                                ellipsoid = Ellipsoid.ANS;
                                break;
                            case "WGS72":
                                ellipsoid = Ellipsoid.WGS72;
                                break;
                            case "Clarke1858":
                                ellipsoid = Ellipsoid.Clarke1858;
                                break;
                            case "Clarke1880":
                                ellipsoid = Ellipsoid.Clarke1880;
                                break;
                            case "Sphere":
                                ellipsoid = Ellipsoid.Sphere;
                                break;

                            default:
                                ellipsoid = defaultEllipsoid;
                                break;
                        }                       
                        
                    }
                    catch
                    {
                        ellipsoid = defaultEllipsoid;
                    }
                }
            }
            else
            {
                distanceBetweenPoints = defaultDistanceBetweenPoints;
                ellipsoid = defaultEllipsoid;
                srtmData = new SRTMData(defaultSrtmCache);
            }
        }

        public List<Point> CalculateIntermediatePoints(GlobalCoordinates start, GlobalCoordinates end)
        {
            points.Clear();

            int? startHeight = srtmData.GetElevation(start.Latitude.Degrees, start.Longitude.Degrees);
            points.Add(new Point(start, startHeight));

            GeodeticCurve path = geoCalc.CalculateGeodeticCurve(ellipsoid, start, end);
            Angle azimuth = path.Azimuth.Degrees;
            double totalDistance = path.EllipsoidalDistance;

            int numberOfPoints = (Convert.ToInt32(Math.Floor(totalDistance / distanceBetweenPoints)));

            for (int i = 0; i < numberOfPoints; i++)
            {
                GlobalCoordinates nextPoint = geoCalc.CalculateEndingGlobalCoordinates(ellipsoid, points[i].coordinate, azimuth, distanceBetweenPoints, out azimuth);
                int? height = srtmData.GetElevation(nextPoint.Latitude.Degrees, nextPoint.Longitude.Degrees);

                points.Add(new Point(nextPoint, height));
            }

            int? endHeight = srtmData.GetElevation(end.Latitude.Degrees, end.Longitude.Degrees);
            points.Add(new Point(end, endHeight));

            return points;
        }

        public double GetDistanceBetweenPoints()
        {
            return distanceBetweenPoints;
        }
    }
}
