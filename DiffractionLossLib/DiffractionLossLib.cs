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

        protected Configuration config;

        protected LibraryMode mode = LibraryMode.Normal;

        protected GeodeticCalculator geoCalc;
        protected Ellipsoid ellipsoid;
        protected Ellipsoid defaultEllipsoid = Ellipsoid.WGS84;
        protected double distanceBetweenPoints;
        protected const double defaultDistanceBetweenPoints = 100;

        protected double distanceBetweenLastPoints;

        protected SRTMData srtmData;
        protected const string defaultSrtmCache = @"C:\temp\srtm-cache";

        protected double effectiveEarthRadius;
        protected const double defaultEffectiveEarthRadius = 8495;

        protected const double bullingtonCutoff = -0.78;

        protected GeodeticCurve path;

        #endregion



        #region Primary Interface

        public DiffractionLossCalculator()
        {
            geoCalc = new GeodeticCalculator();

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
            List<Point> points = GenerateIntermediateProfilePoints(Tx, Rx);

            double L_ba = CalculateActualTerrain(points, TxAntennaeHeight, RxAntennaeHeight, frequency);

            double h_mts;
            double h_mrs;
            double L_bs = CalculateSmoothProfile(points, TxAntennaeHeight, RxAntennaeHeight, frequency, out h_mts, out h_mrs);

            double L_sph = CalculateSphericalEarth(h_mts, h_mrs, frequency);

            // Equation 66
            double L = CalculateLoss(L_ba, L_bs, L_sph);

            return L;
        }


        #endregion



        #region Equation Step Functions

        protected double CalculateActualTerrain(List<Point> points, double TxAntennaeHeight, double RxAntennaeHeight, double frequency)
        {
            // equation 49
            double S_tim = GetLargestIntermediateSlopeFromTx(points, TxAntennaeHeight);

            // equation 50
            double S_tr = GetTotalPathSlope(points, TxAntennaeHeight, RxAntennaeHeight);

            double L_uc;

            double L_b;

            if (S_tim < S_tr)
            {
                // case 1: the path is line of sight

                // equation 51
                double V_max = GetHighestDiffractionParameter(points, TxAntennaeHeight, RxAntennaeHeight, frequency);

                // equation 31/52
                L_uc = (V_max > bullingtonCutoff) ? GetBullingtonMethodLoss(V_max) : 0.0;
            }
            else
            {
                // case 2: the path is trans-horizon

                //equation 53
                double S_rim = GetLargestIntermediateSlopeFromRx(points, RxAntennaeHeight);

                // equation 54
                // d_b is distance from Tx to B in kilometers
                double d_b = GetDistanceFromTxToB(points, TxAntennaeHeight, RxAntennaeHeight, S_tim, S_rim);

                // equation 55
                double V_b = GetBullingtonPointDiffractionParameter(points, TxAntennaeHeight, RxAntennaeHeight, S_tim, d_b, frequency);

                // equation 31/56
                L_uc = (V_b > bullingtonCutoff) ? GetBullingtonMethodLoss(V_b) : 0.0;
            }

            // equation 57
            L_b = GetBullingtonMethodDiffractionLoss(L_uc);

            return L_b;
        }

        protected double CalculateSmoothProfile(List<Point> points, double TxAntennaeHeight, double RxAntennaeHeight, double frequency, out double h_mts, out double h_mrs)
        {
            // equation 58
            double v_1 = GetV_1(points);

            // equation 59
            double v_2 = GetV_2(points);

            // equation 60a
            double h_stip = GetTransmitterInitialProvisionalHeight(v_1, v_2);

            // equation 60b
            double h_srip = GetReceiverInitialProvisionalHeight(v_1, v_2);

            // equation 61a,b,c,d
            (double h_obs, double a_obt, double a_obr) = GetObstructionAndElevations(points, TxAntennaeHeight, RxAntennaeHeight);

            double h_stp;
            double h_srp;

            if (h_obs <= 0.0)
            {
                // equation 62a
                h_stp = h_stip;

                // equation 62b
                h_srp = h_srip;
            }
            else
            {
                //equation 62e
                double g_t = a_obt / (a_obt + a_obr);

                //equation 62f
                double g_r = a_obr / (a_obt + a_obr);

                //equation 62c
                h_stp = h_stip - (h_obs * g_t);

                //equation 62d
                h_srp = h_srip - (h_obs * g_r);
            }

            // equation 63a,b
            double h_1 = points.First().height ?? 0.0;
            double h_st = (h_stp > h_1) ? h_1 : h_stp;

            // equation 63c,d
            double h_n = points.Last().height ?? 0.0;
            double h_sr = (h_srp > h_n) ? h_n : h_srp;

            // equation 64a
            double h_ts = (points.First().height ?? 0.0) + TxAntennaeHeight;
            h_mts = h_ts - h_st;      

            // equation 64b
            double h_rs = (points.Last().height ?? 0.0) + RxAntennaeHeight;
            h_mrs = h_rs - h_sr;

            // create the smooth profile points
            List<Point> smoothProfilePoints = new List<Point>();
            for (int i = 0; i < points.Count; i++)
            {
                smoothProfilePoints.Add(new Point(points[i].coordinate, 0));
            }
            smoothProfilePoints.First().height = h_mts;
            smoothProfilePoints.Last().height = h_mrs;

            double L_bs = CalculateActualTerrain(smoothProfilePoints, 0.0, 0.0, frequency);

            return L_bs;
        }

        protected double CalculateSphericalEarth(double h_mts, double h_mrs, double frequency)
        {
            double L_sph = 0.0;

            double h_1 = h_mts;
            double h_2 = h_mrs;

            double a = effectiveEarthRadius;

            double d = path.EllipsoidalDistance / 1000.0;

            // equation 21
            double d_loss = Math.Sqrt(2.0 * a) * (Math.Sqrt(h_1 / 1000.0) + Math.Sqrt(h_2 / 1000.0));

            if ( d >= d_loss)
            {
                // Section 3.1.1
                /* 
                 * For our use case can skip a section here and use Beta = 1. 
                 * Calculating the electrical influence will be a feature for the next release.
                 */
                //(double K_H, double K_V) = CalculateEletricalInfluence();

                L_sph = CalculateDiffractionLossForSphericalEarth(d, a, h_1, h_2, frequency, 1.0);

                return L_sph;
            }
            else
            {
                // Section 3.2
                (double h, double h_req) = CalculateClearanceHeights(a, d, h_1, h_2, frequency);

                if (h > h_req)
                {
                    L_sph = 0.0;

                    return L_sph;
                }

                // equation 24
                double a_em = 0.5 * Math.Pow(d / (Math.Sqrt(h_1 / 1000.0) + Math.Sqrt(h_2 / 1000.0)), 2.0);

                double A_h = CalculateDiffractionLossForSphericalEarth(d, a_em, h_1, h_2, frequency, 1.0);

                if (A_h < 0)
                {
                    L_sph = 0.0;

                    return L_sph;
                }
                else
                {
                    L_sph = (1.0 - (h / h_req)) * A_h;
                }
            }

            return L_sph;
        }

        // equation 66
        protected double CalculateLoss(double L_ba, double L_bs, double L_sph)
        {
            /*
             * Developer's Note: Compared to the validation data the value of L_sph appears to be the negative value required. 
             * So I'm using -L_sph here despite Equation 66 being written as L = L_ba + max(L_sph - L_bs, 0)
             * This requires some further investigation.
             */ 
            double L = L_ba + ((-L_sph - L_bs > 0) ? -L_sph - L_bs : 0.0);

            return L;
        }

        public List<Point> GenerateIntermediateProfilePoints(GlobalCoordinates start, GlobalCoordinates end)
        {
            List<Point> points = new List<Point>(); 

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
        protected double GetLargestIntermediateSlopeFromTx(List<Point> points, double TxAntennaeHeight)
        {
            // slope = m/km
            double? S_tim = null; ;

            // first point is Tx
            double h_ts = (points.First().height ?? 0) + TxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double C_e = 1.0 / effectiveEarthRadius;

            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count - 1; i++)
            {
                double h_i = points[i].height ?? 0.0;
                double d_i = i * distanceBetweenPoints / 1000.0;

                double S_i = GetSlope(h_ts, h_i, C_e, d_i, d, d_i);

                S_tim = (S_i > S_tim || S_tim == null) ? S_i : S_tim;
            }

            return S_tim ?? 0;
        }

        // equation 50
        protected double GetTotalPathSlope(List<Point> points, double TxAntennaeHeight, double RxAntennaeHeight)
        {
            // first point is Tx
            double h_ts = (points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = (points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;

            double S_tr = (h_rs - h_ts) / d;

            return S_tr;
        }

        // equation 51
        protected double GetHighestDiffractionParameter(List<Point> points, double TxAntennaeHeight, double RxAntennaeHeight, double frequency)
        {
            double? V_max = null;

            // first point is Tx
            double h_ts = (points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = (points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double C_e = 1.0 / effectiveEarthRadius;
            double f = 300.0 / frequency;
            
            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count - 1; i++)
            {
                double h_i = points[i].height ?? 0.0;
                double d_i = i * distanceBetweenPoints / 1000.0;

                double buldge = (500.0 * C_e * d_i * (d - d_i));

                double V_i = GetDiffractionParameter(h_i, h_ts, h_rs, d, d_i, buldge, f);

                V_max = (V_i > V_max || V_max == null) ? V_i : V_max;
            }

            return V_max ?? 0;
        }

        // equation 31/52
        protected double GetBullingtonMethodLoss(double v)
        {
            double J = 6.9 + (20.0 * Math.Log10( Math.Sqrt( Math.Pow((v - 0.1), 2.0) + 1.0 ) + v - 0.1 ));

            return J;
        }

        // equation 53
        protected double GetLargestIntermediateSlopeFromRx(List<Point> points, double RxAntennaeHeight)
        {
            // slope = m/km
            double? S_rim = null;

            // first point is Tx
            double h_rs = (points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double C_e = 1.0 / effectiveEarthRadius;

            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count - 1; i++)
            {
                double h_i = (points[i].height ?? 0.0);

                // d_(n-2) will likely be less than distanceBetweenPoints from Rx
                double d_i;
                if (i == points.Count - 2)
                {
                    if (mode == LibraryMode.Normal)
                    {
                        GeodeticCurve subPath = geoCalc.CalculateGeodeticCurve(ellipsoid, points[i].coordinate, points.Last().coordinate);
                        d_i = subPath.EllipsoidalDistance / 1000.0;
                    }
                    else
                    {
                        d_i = distanceBetweenLastPoints / 1000.0;
                    }
                }
                else
                {
                    d_i = i * distanceBetweenPoints / 1000.0;
                }

                double S_i = GetSlope(h_rs, h_i, C_e, d_i, d, (d - d_i));

                S_rim = (S_i > S_rim || S_rim == null) ? S_i : S_rim;
            }

            return S_rim ?? 0;
        }
        
        // equation 54
        protected double GetDistanceFromTxToB(List<Point> points, double TxAntennaeHeight, double RxAntennaeHeight, double S_tim, double S_rim)
        {            
            // first point is Tx
            double h_ts = (points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = (points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;

            double d_b = (h_rs - h_ts + (S_rim * d)) / (S_tim + S_rim);

            return d_b;
        }

        // equation 55
        protected double GetBullingtonPointDiffractionParameter(List<Point> points, double TxAntennaeHeight, double RxAntennaeHeight, double S_tim, double d_b, double frequency)
        {
            // first point is Tx
            double h_ts = (points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = (points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            double f = 300.0 / frequency;

            double buldge = S_tim * d_b;

            double V_b = GetDiffractionParameter(h_ts, h_ts, h_rs, d, d_b, buldge, f);

            return V_b;
        }

        // equation 57
        protected double GetBullingtonMethodDiffractionLoss(double L_uc)
        {
            double L_b;

            double d = path.EllipsoidalDistance / 1000.0;

            L_b = L_uc + ((1.0 - Math.Exp(-L_uc / 6.0)) * (10.0 + (0.02 * d)));

            return L_b;
        }

        // equation 58
        protected double GetV_1(List<Point> points)
        {
            double v_1 = 0.0;

            double d = path.EllipsoidalDistance / 1000.0;

            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count; i++)
            {
                double d_i = (i == points.Count - 1) ? d : i * distanceBetweenPoints / 1000.0;
                double d_previous = (i - 1) * distanceBetweenPoints / 1000.0;

                double h_i = (points[i].height ?? 0.0);
                double h_previous = (points[i - 1].height ?? 0.0);

                v_1 += (d_i - d_previous) * (h_i + h_previous);
            }

            return v_1;
        }

        // equation 59
        protected double GetV_2(List<Point> points)
        {
            double v_2 = 0.0;

            double d = path.EllipsoidalDistance / 1000.0;

            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count; i++)
            {
                double d_i = (i == points.Count - 1) ? d : i * distanceBetweenPoints / 1000.0;
                double d_previous = (i - 1) * distanceBetweenPoints / 1000.0;

                double h_i = points[i].height ?? 0.0;
                double h_previous = Convert.ToDouble(points[i - 1].height ?? 0.0);

                v_2 += (d_i - d_previous) * ((h_i * (2 * d_i + d_previous)) + (h_previous * (d_i + (2 * d_previous))));
            }

            return v_2;
        }

        // equation 60a
        protected double GetTransmitterInitialProvisionalHeight(double v_1, double v_2)
        {
            double d = path.EllipsoidalDistance / 1000.0;

            double h_stip = ((2.0 * v_1 * d) - v_2) / Math.Pow(d, 2.0);

            return h_stip;
        }

        // equation 60b
        protected double GetReceiverInitialProvisionalHeight(double v_1, double v_2)
        {
            double d = path.EllipsoidalDistance / 1000.0;

            double h_srip = (v_2 - (v_1 * d)) / Math.Pow(d, 2.0);

            return h_srip;
        }

        // equation 61a,b,c,d
        protected (double, double, double) GetObstructionAndElevations(List<Point> points, double TxAntennaeHeight, double RxAntennaeHeight)
        {
            // first point is Tx
            double h_ts = (points.First().height ?? 0.0) + TxAntennaeHeight;

            // last point is Rx
            double h_rs = (points.Last().height ?? 0.0) + RxAntennaeHeight;

            double d = path.EllipsoidalDistance / 1000.0;
            
            double? h_obs = null;
            double? a_obt = null;
            double? a_obr = null;

            // intermediate points are 1 .. n-2, 0 is Tx and n-1 is Rx
            for (int i = 1; i < points.Count - 1; i++)
            {
                double d_i = i * distanceBetweenPoints / 1000.0;
                double h_i = (points[i].height ?? 0.0);

                // equation 61a/61d
                double h_obi = h_i - ((h_ts * (d - d_i)) + (h_rs * d_i) )/ d;
                // equation 61a
                h_obs = (h_obi > h_obs || h_obs == null) ? h_obi : h_obs;

                // equation 61b
                double a_obti = h_obi / d_i;
                a_obt = (a_obti > a_obt || a_obt == null) ? a_obti : a_obt;

                // equation 61c
                double a_obri = h_obi / (d - d_i);
                a_obr = (a_obri > a_obr || a_obr == null) ? a_obri : a_obr;
            }

            return (h_obs ?? 0.0, a_obt ?? 0.0, a_obr ?? 0.0);
        }

        protected (double, double) CalculateEletricalInfluence()
        {
            //TODO:
            double K_H = 0.0;

            double K_V = K_H + 0.0;

            return (K_H, K_V);
        }

        protected (double, double) CalculateClearanceHeights(double a, double d, double h_1, double h_2, double frequency)
        {
            double f = 300.0 / frequency;

            // equation 22e
            double m = Math.Pow(d, 2.0) / ( 4 * a * (h_1 + h_2) );

            // equation 22d
            double c = (h_1 - h_2) / (h_1 + h_2);

            // equation 22c
            double b = 2.0 * Math.Sqrt((m + 1) / (3 * m))
                * Math.Cos(
                    (Math.PI / 3.0)
                    + (1.0 / 3.0 * Math.Acos(
                        ((3.0 * c) / 2.0)
                        * Math.Sqrt((3.0 * m) / Math.Pow((m + 1), 3.0))
                        )
                    )
                );

            // equation 22a
            double d_1 = (d * (1 + b)) / 2.0;

            // equation 22b
            double d_2 = d - d_1;

            double tmp1 = d_2 * (h_1 - Math.Pow(d_1, 2.0)) / (2 * a);
            double tmp2 = d_1 * (h_2 - Math.Pow(d_2, 2.0)) / (2 * a);

            double h = (tmp1 + tmp2)/d;

            // equaftion 23
            double h_req = 0.552 * Math.Sqrt((d_1 * d_2 * f) / d);

            return (h, h_req);
        }

        protected double CalculateDiffractionLossForSphericalEarth(double d, double a_e, double h_1, double h_2, double f, double Beta)
        {
            // equation 14a
            double X = 2.188 * Beta * Math.Pow(f, 1.0 / 3.0) * Math.Pow(a_e, -2.0 / 3.0) * d;

            // equation 15a
            double Y = 9.575 * 0.001 * Beta * Math.Pow(f, 2.0 / 3.0) * Math.Pow(a_e, -1.0 / 3.0);
            double Y_1 = Y * h_1;
            double Y_2 = Y * h_2;

            Func<double,double> F = (X >= 1.6) ? (Func<double, double>)Equation17a : (Func<double,double>)Equation17b;

            double B_1 = Beta * Y_1;
            Func<double, double> G_1 = (B_1 > 2.0) ? (Func<double, double>)Equation18 : (Func<double, double>)Equation18a;

            double B_2 = Beta * Y_2;
            Func<double, double> G_2 = (B_2 > 2.0) ? (Func<double, double>)Equation18 : (Func<double, double>)Equation18a;

            double L_sph = F(X) + G_1(B_1) + G_2(B_2);

            return L_sph;
        }

        protected static double Equation17a(double X)
        {
            double F = 11.0 + (10 * Math.Log10(X)) - (17.6 * X);
            return F;
        }

        protected static double Equation17b(double X)
        {
            double F = (-20.0 * Math.Log10(X)) - (5.6499 * Math.Pow(X, 1.425));
            return F;
        }

        protected static double Equation18(double B)
        {
            double G = (17.6 * Math.Sqrt(B - 1.1)) - (5 * Math.Log10(B - 1.1)) - 8.0;
            return G;
        }

        protected static double Equation18a(double B)
        {
            double G = 20.0 * Math.Log10(B + (0.1 * Math.Pow(B, 3.0)));
            return G;
        }

        #endregion



        #region Standard Maths Equations

        protected double GetSlope(double h_s, double h_i, double C_e, double d_i, double d, double d_s)
        {
            return (h_i + (500.0 * C_e * d_i * (d - d_i)) - h_s) / d_s;
        }

        protected double GetDiffractionParameter(double h_i, double h_ts, double h_rs, double d, double d_i, double buldge, double f)
        {
            double rayHeight = ((h_ts * (d - d_i)) + (h_rs * d_i)) / d;

            double oneOnF2 = Math.Sqrt((0.002 * d) / (f * d_i * (d - d_i)));

            double v = (h_i + buldge - rayHeight) * oneOnF2;

            return v;
        }

        #endregion



        #region Member Functions

        protected void UseDefaultValues()
        {
            distanceBetweenPoints = defaultDistanceBetweenPoints;
            ellipsoid = defaultEllipsoid;
            srtmData = new SRTMData(defaultSrtmCache);
            effectiveEarthRadius = defaultEffectiveEarthRadius;
        }

        #endregion


        #region Member Types

        protected enum LibraryMode { Normal, Test};

        #endregion
    }
}
