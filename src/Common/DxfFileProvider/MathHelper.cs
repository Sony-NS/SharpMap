/*
 * Created  : Sony NS
 * Descript : utility
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class MathHelper
    {
        #region CoordinateSystem enum

        /// <summary>
        /// Defines the coordinate system reference.
        /// </summary>
        public enum CoordinateSystem
        {
            /// <summary>
            /// World coordinates.
            /// </summary>
            World,
            /// <summary>
            /// Object coordinates.
            /// </summary>
            Object
        }

        #endregion

        public GeoAPI.Geometries.ICoordinate CrossProduct(GeoAPI.Geometries.ICoordinate u, GeoAPI.Geometries.ICoordinate v)
        {
            double a = u.Y * v.Z - u.Z * v.Y;
            double b = u.Z * v.X - u.X * v.Z;
            double c = u.X * v.Y - u.Y * v.X;
            GeoAPI.Geometries.ICoordinate coord = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate(a, b, c);
            return coord;
        }

        public static double DotProduct(GeoAPI.Geometries.ICoordinate u, GeoAPI.Geometries.ICoordinate v)
        {
            return (u.X * v.X) + (u.Y * v.Y) + (u.Z * v.Z);
        }

        //public GeoAPI.Geometries.ICoordinate Transform(GeoAPI.Geometries.ICoordinate point, GeoAPI.Geometries.ICoordinate zAxis, 
        //    CoordinateSystem from, CoordinateSystem to)
        //{
        //    Matrix3d trans = ArbitraryAxis(zAxis);
        //    if (from == CoordinateSystem.World && to == CoordinateSystem.Object)
        //    {
        //        trans = trans.Traspose();
        //        return trans * point;
        //    }
        //    if (from == CoordinateSystem.Object && to == CoordinateSystem.World)
        //    {
        //        return trans * point;
        //    }
        //    return point;
        //}
    }
}
