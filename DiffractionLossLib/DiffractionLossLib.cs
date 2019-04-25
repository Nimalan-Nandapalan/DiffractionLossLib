using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;

using Gavaghan.Geodesy;
using SRTM;

namespace DiffractionLossLib
{
    public class DiffractionLossCalculator
    {
        #region Members

        private Configuration config;

        private GeodeticCalculator geoCalc;
        private Ellipsoid ellipsoid;
        private Ellipsoid defaultEllipsoid = Ellipsoid.WGS84;
        private double distanceBetweenPoints;
        private const double defaultDistanceBetweenPoints = 100;

        private SRTMData srtmData;
        private const string defaultSrtmCache = @"C:\temp\srtm-cache";

        private double effectiveEarthRadius;
        private const double defaultEffectiveEarthRadius = 8495;

        private const double bullingtonCutoff = -0.78;

        private List<Point> points;

        GeodeticCurve path;

        #endregion



        #region Primary Interface

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
                UseDefaultValues();
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

                element = config.AppSettings.Settings["effectiveEarthRadius"];
                if (element != null)
                {
                    try
                    {
                        effectiveEarthRadius = double.Parse(element.Value);
                    }
                    catch
                    {
                        effectiveEarthRadius = defaultEffectiveEarthRadius;
                    }
                }
            }
            else
            {
                UseDefaultValues();
            }
        }

        // Tx - transmitter i.e. start
        // Rx - receiver i.e. end
        // Antennae Heights in meters
        // frequency in MHz

        public double CalculateDiffractionLoss(double TxLat, double TxLon, double TxAntennaeHeight, double RxLat, double RxLon, double RxAntennaeHeight, double frequency)
        {
            GlobalCoordinates Tx = new GlobalCoordinates(new Angle(TxLat), new Angle(TxLon));
            GlobalCoordinates Rx = new GlobalCoordinates(new Angle(RxLat), new Angle(RxLon));

            return CalculateDiffractionLoss(Tx, TxAntennaeHeight, Rx, RxAntennaeHeight, frequency);
        }

        public double CalculateDiffractionLoss(GlobalCoordinates Tx, double TxAntennaeHeight, GlobalCoordinates Rx, double RxAntennaeHeight, double frequency)
        {
            GenerateIntermediateProfilePoints(Tx, Rx);

            // equation 49
            double S_tim = GetLargestIntermediateSlopeFromTx(TxAntennaeHeight);

            // equation 50
            double S_tr = GetTotalPathSlope(TxAntennaeHeight, RxAntennaeHeight);

            double L_uc;

            double L_b;

            if (S_tim < S_tr)
            {
                // case 1: the path is line of sight

                // equation 51
                double V_max = GetHighestDiffractionParameter(TxAntennaeHeight, RxAntennaeHeight, frequency);

                // equation 31/52
                L_uc = (V_max > bullingtonCutoff) ? GetBullingtonMethodLoss(V_max) : 0.0;
            }
            else
            {
                // case 2: the path is trans-horizon

                //equation 53
                double S_rim = GetLargestIntermediateSlopeFromRx(RxAntennaeHeight);

                // equation 54
                // d_b is distance from Tx to B in kilometers
                double d_b = GetDistanceFromTxToB(TxAntennaeHeight, RxAntennaeHeight, S_tim, S_rim);

                // equation 55
                double V_b = GetBullingtonPointDiffractionParameter(TxAntennaeHeight, RxAntennaeHeight, S_tim, d_b, frequency);

                // equation 31/56
                L_uc = GetBullingtonMethodLoss(V_b);
            }
                       
            // equation 57
            L_b = GetBullingtonMethodDiffractionLoss(L_uc);

            return L_b;


            // Developer's Note: I'm unsure of the calculations from this point

            // calculate L_bs

            // calculate L_sph

        }


        #endregion



        #region Equation Step Functions

        public List<Point> GenerateIntermediateProfilePoints(GlobalCoordinates start, GlobalCoordinates end)
        {
            points.Clear();

            int? startHeight = srtmData.GetElevation(start.Latitude.Degrees, start.Longitude.Degrees);
            points.Add(new Point(start, startHeight));

            path = geoCalc.CalculateGeodeticCurve(ellipsoid, start, end);
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

        // equation numbers refer to https://www.itu.int/dms_pubrec/itu-r/rec/p/R-REC-P.526-14-201801-I!!PDF-E.pdf

        // equation 49
        private double GetLargestIntermediateSlopeFromTx(double TxAntennaeHeight)
        {
            // slope = m/km
            double? S_tim = null; ;

            // first point is Tx
            double h_ts = Convert.ToDouble(points.First().height ?? 0) + TxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double C_e = 1.0 / effectiveEarthRadius;

            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count - 1; i++)
            {
                double h_i = Convert.ToDouble(points[i].height ?? 0.0);
                double d_i = (double)i * distanceBetweenPoints / 1000.0;

                double S_i = GetSlope(h_ts, h_i, C_e, d_i, d, d_i);

                S_tim = (S_i > S_tim || S_tim == null) ? S_i : S_tim;
            }

            return S_tim ?? 0;
        }

        // equation 50
        private double GetTotalPathSlope(double TxAntennaeHeight, double RxAntennaeHeight)
        {
            // first point is Tx
            double h_ts = Convert.ToDouble(points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = Convert.ToDouble(points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;

            double S_tr = (h_rs - h_ts) / d;

            return S_tr;
        }

        // equation 51
        private double GetHighestDiffractionParameter(double TxAntennaeHeight, double RxAntennaeHeight, double frequency)
        {
            double? V_max = null;

            // first point is Tx
            double h_ts = Convert.ToDouble(points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = Convert.ToDouble(points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double C_e = 1.0 / effectiveEarthRadius;
            double f = 300.0 / frequency;
            
            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count - 1; i++)
            {
                double h_i = Convert.ToDouble(points[i].height ?? 0.0);
                double d_i = (double)i * distanceBetweenPoints / 1000.0;

                double buldge = (500.0 * C_e * d_i * (d - d_i));

                double V_i = GetDiffractionParameter(h_i, h_ts, h_rs, d, d_i, buldge, f);

                V_max = (V_i > V_max || V_max == null) ? V_i : V_max;
            }

            return V_max ?? 0;
        }
        
        // equation 31/52
        private double GetBullingtonMethodLoss(double v)
        {
            double J = 6.9 + (20.0 * Math.Log( Math.Sqrt( Math.Pow(v - 0.1, 2.0) + 1 ) + v - 0.1 ));

            return J;
        }

        // equation 53
        private double GetLargestIntermediateSlopeFromRx(double RxAntennaeHeight)
        {
            // slope = m/km
            double? S_rim = null;

            // first point is Tx
            double h_rs = Convert.ToDouble(points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double C_e = 1.0 / effectiveEarthRadius;

            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count - 1; i++)
            {
                double h_i = Convert.ToDouble(points[i].height ?? 0.0);

                // d_(n-2) will likely be less than distanceBetweenPoints from Rx
                double d_i;
                if (i == points.Count - 2)
                {
                    GeodeticCurve subPath = geoCalc.CalculateGeodeticCurve(ellipsoid, points[i].coordinate, points.Last().coordinate);
                    d_i = subPath.EllipsoidalDistance / 1000.0;
                }
                else
                {
                    d_i = (double)i * distanceBetweenPoints / 1000.0;
                }

                double S_i = GetSlope(h_rs, h_i, C_e, d_i, d, (d - d_i));

                S_rim = (S_i > S_rim || S_rim == null) ? S_i : S_rim;
            }

            return S_rim ?? 0;
        }

        // equation 54
        private double GetDistanceFromTxToB(double TxAntennaeHeight, double RxAntennaeHeight, double S_tim, double S_rim)
        {            
            // first point is Tx
            double h_ts = Convert.ToDouble(points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = Convert.ToDouble(points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;

            double d_b = (h_rs - h_ts + (S_rim * d)) / (S_tim + S_rim);

            return d_b;
        }

        // equation 55
        private double GetBullingtonPointDiffractionParameter(double TxAntennaeHeight, double RxAntennaeHeight, double S_tim, double d_b, double frequency)
        {
            // first point is Tx
            double h_ts = Convert.ToDouble(points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = Convert.ToDouble(points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double f = 300.0 / frequency;

            double buldge = S_tim * d_b;

            double V_b = GetDiffractionParameter(h_ts, h_ts, h_rs, d, d_b, buldge, f);

            return V_b;
        }

        // equation 57
        private double GetBullingtonMethodDiffractionLoss(double L_uc)
        {
            double L_b;

            double d = path.EllipsoidalDistance / 1000.0;

            L_b = L_uc + (1.0 - Math.Exp(-L_uc / 6.0)) * (10.0 + (0.02 * d));

            return L_b;
        }

        #endregion

        #region Standard Maths Equations

        double GetSlope(double h_s, double h_i, double C_e, double d_i, double d, double d_s)
        {
            return (h_i + (500.0 * C_e * d_i * (d - d_i)) - h_s) / d_s;
        }

        double GetDiffractionParameter(double h_i, double h_ts, double h_rs, double d, double d_i, double buldge, double f)
        {
            double rayHeight = (h_ts * (d - d_i) + (h_rs * d_i)) / d;

            double oneOnF2 = Math.Sqrt(0.002 * d / (f * d_i * (d - d_i)));

            double v = (h_i + buldge - rayHeight) * oneOnF2;

            return v;
        }

        #endregion


        #region Member Functions


        private void UseDefaultValues()
        {
            distanceBetweenPoints = defaultDistanceBetweenPoints;
            ellipsoid = defaultEllipsoid;
            srtmData = new SRTMData(defaultSrtmCache);
            effectiveEarthRadius = defaultEffectiveEarthRadius;
        }

        #endregion
    }
}
