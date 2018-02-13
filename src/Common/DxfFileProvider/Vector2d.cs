/*
 * Created  : Sony NS
 * Descript : Defines a 2D point in space. Contains a static method for an origin point.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Vector2d
    {
        public double X { get; set; }
        public double Y { get; set; }

        /// <summary>
        /// Initialize a new instance of the Vector2d object. 
        /// </summary>
        /// <param name="X">X coordinate</param>
        /// <param name="Y">Y coordinate</param>
        /// <returns>A DXF Vector2d object</returns>
        public Vector2d(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        /// <summary>
        /// Gets a Vector2d at the origin (0,0)
        /// </summary>
        /// <returns>A DXF Vector2d object</returns>
        public static Vector2d Zero
        {
            get { return new Vector2d(0, 0); }
        }

        public static GeoAPI.Geometries.ICoordinate ZeroCoordinate
        {
            get { return new GisSharpBlog.NetTopologySuite.Geometries.Coordinate(); }
        }
        //new ICoordinate[]
        public static GeoAPI.Geometries.ICoordinate[] ZeroArrCoordinate
        {
            get { return new GeoAPI.Geometries.ICoordinate[] { }; }
        }
    }
}
