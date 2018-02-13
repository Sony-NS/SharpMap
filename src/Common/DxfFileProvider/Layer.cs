/*
 * Created  : Sony NS 
 * Descript : Defines a DXF layer, with it's name and AciColor code
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class Layer
    {
        public string Name { get; set; }
        public int ColorIndex { get; set; }

        /// <summary>
        /// Initialize a new instance of the Layer object
        /// </summary>
        /// <param name="Name">Layer name</param>
        /// <param name="ColorIndex">The AciColor index for the layer</param>
        /// <returns>A DXF Layer object</returns>
        public Layer(string Name, int ColorIndex)
        {
            this.Name = Name;
            this.ColorIndex = ColorIndex;
        }
    }
}
