/*
 * Created  : Sony NS 
 * Descript : Defines a DXF line, with starting and ending point
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Line
    {
        public List<GeoAPI.Geometries.ICoordinate> Location { get; set; }
        public string Layer { get; set; }
        public string Handle { get; set; }

        /// <summary>
        /// Initialize a new instance of the Line object
        /// </summary>
        /// <param name="Location">A Vector2d containg X and Y coordinates of the first point</param>
        /// <param name="P2">A Vector2d containg X and Y coordinates of the second point</param>
        /// <param name="Layer">Layer name</param>
        /// <returns>A DXF Line object</returns>
        public Line(List<GeoAPI.Geometries.ICoordinate> Location, string Layer, string Handle)
        {
            this.Location = Location.ToList();
            this.Layer = Layer;
            this.Handle = Handle;
        }
    }
}
