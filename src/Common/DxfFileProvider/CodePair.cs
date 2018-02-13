/*
 * Created  : Sony NS
 * Descript : CodePair class for storing the code/value read from the DXF file
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider
{
    public class CodePair
    {
        public int Code { get; set; }
        public string Value { get; set; }

        /// <summary>
        /// Initialize a new instance of the CodePair object. 
        /// </summary>
        /// <param name="Code">Numeric DXF code</param>
        /// <param name="Value">The value of the corresponding code</param>
        /// <returns>A DXF Vector2d object</returns>
        public CodePair(int Code, string Value)
        {
            this.Code = Code;
            this.Value = Value;
        }
    }
}
