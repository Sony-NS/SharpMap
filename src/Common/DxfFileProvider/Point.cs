/*
 * Created  : Sony NS
 * Descript : Defines a DXF point, with it's layer and position
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Point
    {
        public GeoAPI.Geometries.ICoordinate Location { get; set; }
        public string Layer;
        public string Handle;

        /// <summary>
        /// Initialize a new instance of the Point object
        /// </summary>
        /// <param name="Position">A Vector2d containg X and Y coordinates</param>
        /// <param name="Layer">Layer name</param>
        /// <returns>A DXF Point object</returns>
        public Point(GeoAPI.Geometries.ICoordinate Location, string Layer, string Handle)
        {
            this.Location = Location;
            this.Layer = Layer;
            this.Handle = Handle;
        }
    }
}
