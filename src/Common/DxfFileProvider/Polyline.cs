/*
 * Created  : Sony NS 
 * Descript : Defines a DXF polyline, with it's layer, vertex list and closed flag
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Polyline
    {
        public string Layer { get; set; }
        public string Handle { get; set; }
        public List<GeoAPI.Geometries.ICoordinate> Location { get; set; }
        public bool Closed { get; set; }
        public double Elevation { get; set; }

        /// <summary>
        /// Initialize a new instance of the Polyline object
        /// </summary>
        /// <param name="Vertexes">A Vertex list containg X and Y coordinates of each vertex</param>
        /// <param name="Layer">Layer name</param>
        /// <param name="Closed">Determine if the polyline is opened or closed</param>
        /// <returns>A DXF Polyline object</returns>
        public Polyline(List<GeoAPI.Geometries.ICoordinate> Location, string Layer, string Handle, bool Closed)
        {
            this.Location = Location;
            this.Layer = Layer;
            this.Handle = Handle;
            this.Closed = Closed;
        }
    }
}
