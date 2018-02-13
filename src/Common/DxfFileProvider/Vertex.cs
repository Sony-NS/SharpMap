/*
 * Created  : Sony NS 
 * Descript : Defines a DXF vertex, with position, bulge and layer
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Vertex
    {
        public Vector2d Position { get; set; }
        public double Bulge { get; set; }
        public double Elevation { get; set; }
        public string Layer { get; set; }

        /// <summary>
        /// Initialize a new instance of the Vertex object. Bulge and Layer are optional (defaults to 0).
        /// </summary>
        /// <param name="Location">A Vector2d containg X and Y coordinates</param>
        /// <param name="Bulge">The tangent of 1/4 the included angle for an arc segment. Negative if the arc goes clockwise from the start point to the endpoint.</param>
        /// <param name="Layer">Layer name</param>
        /// <returns>A DXF Vertex object</returns>
        public Vertex(Vector2d Location, double Bulge = 0, string Layer = "0")
        {
            this.Position = Location;
            this.Bulge = Bulge;
            this.Layer = Layer;
        }

        /// <summary>
        /// Initialize a new instance of the Vertex object. Bulge and Layer are optional (defaults to 0).
        /// </summary>
        /// <param name="X">X coordinate</param>
        /// <param name="Y">Y coordinate</param>
        /// <param name="Bulge">The tangent of 1/4 the included angle for an arc segment. Negative if the arc goes clockwise from the start point to the endpoint.</param>
        /// <param name="Layer">Layer name</param>
        /// <returns>A DXF Vertex object</returns>
        public Vertex(double X, double Y, double Elevation, double Bulge = 0, string Layer = "0")
        {
            this.Position = new Vector2d(0, 0);
            this.Position.X = X;
            this.Position.Y = Y;
            this.Elevation = Elevation;
            this.Bulge = Bulge;
            this.Layer = Layer;
        }
    }
}
