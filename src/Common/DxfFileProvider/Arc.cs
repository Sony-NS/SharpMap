/*
 * Created  : Sony NS
 * Descript : Defines a DXF arc, with it's layer, center point, radius, start and end angle
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Arc
    {
        public string Layer { get; set; }
        public string Handle { get; set; }
        public GeoAPI.Geometries.ICoordinate Center { get; set; }
        public double Radius { get; set; }
        public double StartAngle { get; set; }
        public double EndAngle { get; set; }

        /// <summary>
        /// Initialize a new instance of the Arc object
        /// </summary>
        /// <param name="Center">A Vector2d containg X and Y center coordinates</param>
        /// <param name="Radius">Arc radius</param>
        /// <param name="StartAng">Starting angle, in degrees</param>
        /// <param name="EndAng">Ending angle, in degrees</param>
        /// <param name="Layer">Layer name</param>
        /// <returns>A DXF Arc object</returns>
        public Arc(GeoAPI.Geometries.ICoordinate Center, double Radius, double StartAng, double EndAng, string Layer, string Handle)
        {
            this.Center = Center;
            this.Radius = Radius;
            this.StartAngle = StartAng;
            this.EndAngle = EndAng;
            this.Layer = Layer;
            this.Handle = Handle;
        }
    }
}
