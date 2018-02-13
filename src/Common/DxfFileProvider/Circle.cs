/*
 * Created  : Sony NS
 * Descript : Defines a DXF circle, with it's layer, center point and radius
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Circle
    {
        public double Radius { get; set; }
        public GeoAPI.Geometries.ICoordinate Center { get; set; }
        public string Layer { get; set; }
        public string Handle { get; set; }

        /// <summary>
        /// Initialize a new instance of the Circle object
        /// </summary>
        /// <param name="Center">A Vector2d containg X and Y center coordinates</param>
        /// <param name="Radius">Circle radius</param>
        /// <param name="Layer">Layer name</param>
        /// <returns>A DXF Circle object</returns>
        public Circle(GeoAPI.Geometries.ICoordinate Center, double Radius, string Layer, string Handle)
        {
            this.Center = Center;
            this.Radius = Radius;
            this.Layer = Layer;
            this.Handle = Handle;
        }
    }
}
