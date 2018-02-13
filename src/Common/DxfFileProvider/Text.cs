/*
 * Created  : Sony NS 
 * Descript : Defines a DXF text, with its layer, node point and text value
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Text
    {
        public string Value { get; set; }
        public string Layer { get; set; }
        public string Handle { get; set; }
        public GeoAPI.Geometries.ICoordinate Location { get; set; }

        /// <summary>
        /// Initialize a new instance of the Text object
        /// </summary>
        /// <param name="Position">A Vector2d containg  X and Y coordinates</param>
        /// <param name="Value">The text string itself</param>
        /// <param name="Layer">Layer name</param>
        /// <returns>A DXF Text object</returns>
        public Text(GeoAPI.Geometries.ICoordinate Location, string Value, string Layer, string Handle)
        {
            this.Location = Location;
            this.Value = Value;
            this.Layer = Layer;
            this.Handle = Handle;
        }
    }
}
