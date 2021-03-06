// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using DelftTools.Utils.Aop.NotifyPropertyChange;
using DelftTools.Utils.Drawing;
using GeoAPI.Geometries;
using SharpMap.Styles.Shapes;

namespace SharpMap.Styles
{
	/// <summary>
	/// Defines a style used for rendering vector data
	/// </summary>
	[NotifyPropertyChange(EnableLogging = false)]
	public class VectorStyle : Style, IDisposable
	{
		#region Privates
		private Pen line;
		private Pen outline;
		private bool enableOutline;
		private Brush fill;
		private Bitmap symbol;
        private Bitmap legendSymbol;
	    private Type geometryType;
        private int shapeSize = 18;
	    private bool customSymbol;
        //NS, 2013-09-16
        private bool enableDashStyle;
        private Pen dashLine;
        private bool dashReverse;
        //NS, 2013-09-23, Digunakan pada saat editing point
        //private Brush _pointBrush;
        //NS, 2013-10-01
        private bool enableCircleRadius;
        private int circleRadius = 20;
        private Pen circleLine;
        //NS, 2014-07-17, curve pada line string harus bisa diseting curve atau bukan
        private bool isCurve = false;
		#endregion

		/// <summary>
		/// Initializes a new VectorStyle and sets the default values
		/// </summary>
		/// <remarks>
		/// Default style values when initialized:<br/>
		/// *LineStyle: 1px solid black<br/>
		/// *FillStyle: Solid black<br/>
		/// *Outline: No Outline
		/// *Symbol: null-reference
		/// </remarks>
		public VectorStyle() : 
            this(
                new SolidBrush(Color.AntiqueWhite), 
                            (Pen) Pens.Black.Clone(), 
                            true, 
                            (Pen) Pens.BlueViolet.Clone(), 
                            1f, 
                            typeof (ILineString),
                            ShapeType.Ellipse, //NS, 2013-09-28 Diamond to Ellipse....
                            18)
            {
		}

        /// <summary>
        /// Non default constructor to enable fast creation of correct vectorStyle without first generating invalid
        /// default style and symbol.
        /// </summary>
        /// <param name="fill"></param>
        /// <param name="outline"></param>
        /// <param name="enableOutline"></param>
        /// <param name="line"></param>
        /// <param name="symbolScale"></param>
        /// <param name="geometryType"></param>
        /// <param name="shapeType"></param>
        /// <param name="shapeSize"></param>
        public VectorStyle(Brush fill, Pen outline, bool enableOutline, Pen line, float symbolScale,
            Type geometryType, ShapeType shapeType, int shapeSize)
        {
            this.fill = fill;
            this.outline = outline;
            this.enableOutline = enableOutline;
            this.line = line;
            //NS, 2013-09-16
            this.enableDashStyle = false;
            this.dashLine = new System.Drawing.Pen(System.Drawing.Color.Red, 1);
            this.dashReverse = false;
            //NS, 2013-09-23
            //PointColor = new SolidBrush(Color.Red);
            //NS, 2013-10-01
            this.enableCircleRadius = false;
            this.circleRadius = 20;
            this.circleLine = new System.Drawing.Pen(System.Drawing.Color.Yellow, 1);
            //EndOfNS
		    this.symbolScale = symbolScale;
    	    this.geometryType = geometryType;
            this.shapeType = shapeType;
            this.shapeSize = shapeSize;

            this.isCurve = false;
            UpdateSymbols();
        }

	    #region Properties

		/// <summary>
		/// Linestyle for line geometries
		/// </summary>
		public virtual Pen Line
		{
			get { return line; }
            set 
            { 
                line = value;
                
                if (! customSymbol)
                    UpdateSymbols(); 
            }
		}

		/// <summary>
		/// Outline style for line and polygon geometries
		/// </summary>
		public virtual Pen Outline
		{
			get { return outline; }
			set 
            { 
                outline = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
		}

        /// <summary>
        /// NS, 2013-09-16, Dashline style for line geometries
        /// </summary>
        public virtual Pen DashLine
        {
            get { return dashLine; }
            set
            {
                dashLine = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

		/// <summary>
		/// Specified whether the objects are rendered with or without outlining
		/// </summary>
		public virtual bool EnableOutline
		{
			get { return enableOutline; }
			set
			{
			    enableOutline = value;

                if (!customSymbol) 
                    UpdateSymbols();
			}
		}

        /// <summary>
        /// NS, 2013-09-16, Specified whether the objects are rendered with or without dashline
        /// </summary>
        public virtual bool EnableDashStyle
        {
            get { return enableDashStyle; }
            set
            {
                enableDashStyle = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

        /// <summary>
        /// NS, 2013-09-16, Specified whether the objects are rendered dash reverse or not
        /// </summary>
        public virtual bool DashReverse
        {
            get { return dashReverse; }
            set
            {
                dashReverse = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

        /// <summary>
        /// NS, 2013-09-23, Fillstyle for Point geometries (will be used if no Symbol is set)
        /// </summary>
        //public Brush PointColor
        //{
        //    get { return _pointBrush; }
        //    set { _pointBrush = value; }
        //}

        /// <summary>
        /// NS, 2013-10-01, Specified whether the objects are rendered with circle radius arround symbol or not
        /// </summary>
        public virtual bool EnableCircleRadius
        {
            get { return enableCircleRadius; }
            set
            {
                enableCircleRadius = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

        /// <summary>
        /// NS, 2013-10-01, CircleLine style for draw circle arround symbol
        /// </summary>
        public virtual Pen CircleLine
        {
            get { return circleLine; }
            set
            {
                circleLine = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

        ///<summary>
        /// NS, 2013-10-01, Defines the circle radius arround symbol
        ///</summary>
        public virtual int CircleRadius
        {
            get { return circleRadius; }
            set
            {
                circleRadius = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

        /// <summary>
        /// NS, 2014-07-17, Curve, untuk menentukan DrawCurve atw DrawLine
        /// </summary>
        public virtual bool IsCurve
        {
            get { return isCurve; }
            set
            {
                isCurve = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

		/// <summary>
		/// Fillstyle for Polygon geometries
		/// </summary>
        public virtual Brush Fill
        {
            get { return fill; }
            set
            {
                fill = value;

                if (!customSymbol) 
                    UpdateSymbols();
            }
        }


        public virtual Bitmap LegendSymbol
        {
            get     
            {
                return legendSymbol;
            }
        }

        public virtual Type GeometryType
        {
            get
            {
                return geometryType;
            }
            set
            {
                geometryType = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }


        /// <summary>
		/// Symbol used for rendering points
		/// </summary>
		public virtual Bitmap Symbol
		{
			get { return symbol; }
			set
			{
			    symbol = value;
			    customSymbol = true;

                if (value != null)
                {
                    //set the legendSymbol with the custom image
                    Bitmap legendSymbolBitmap = new Bitmap(16, 16);
                    Graphics g = Graphics.FromImage(legendSymbolBitmap);
                    g.Clear(Color.Transparent);

                    g.CompositingMode = CompositingMode.SourceOver;
                    g.DrawImage(Symbol, 0, 0, legendSymbolBitmap.Width, legendSymbolBitmap.Height);
                    legendSymbol = legendSymbolBitmap;
                }
			}
		}
		private float symbolScale;

		/// <summary>
		/// Scale of the symbol (defaults to 1)
		/// </summary>
		/// <remarks>
		/// Setting the symbolscale to '2.0' doubles the size of the symbol, where a scale of 0.5 makes the scale half the size of the original image
		/// </remarks>
		public virtual float SymbolScale
		{
			get { return symbolScale; }
			set { symbolScale = value; }
		}

		private PointF symbolOffset;

		/// <summary>
		/// Gets or sets the offset in pixels of the symbol.
		/// </summary>
		/// <remarks>
		/// The symbol offset is scaled with the <see cref="SymbolScale"/> property and refers to the offset af <see cref="SymbolScale"/>=1.0.
		/// </remarks>
		public virtual PointF SymbolOffset
		{
			get { return symbolOffset; }
			set { symbolOffset = value; }
		}

		private float symbolRotation;

        ///<summary>
        /// Defines the shapesize for symbol
        ///</summary>
        public virtual int ShapeSize
        {
            get { return shapeSize; }
            set
            {
                shapeSize = value;

                if (!customSymbol)
                    UpdateSymbols();
            }
        }

        private ShapeType shapeType = ShapeType.Diamond; // default

	    ///<summary>
	    /// Defines shape for symbol
	    ///</summary>
	    public ShapeType Shape
	    {
	        get { return shapeType; }
	        set
	        {
	            shapeType = value;
                UpdateSymbols();
	        }
	    }

	    public static IShapeFactory ShapeFactory = new ShapeFactory();

	    /// <summary>
		/// Gets or sets the rotation of the symbol in degrees (clockwise is positive)
		/// </summary>
		public virtual float SymbolRotation
		{
			get { return symbolRotation; }
			set { symbolRotation = value; }
		}

		#endregion
        
        /// <summary>
        /// This function updates the Symbol property with a bitmap generated by using the shape type, size, fillcolor, bordercolor etc.
        /// </summary>
        private void UpdateSymbols()
        {
            var shape = ShapeFactory.CreateShape();
            shape.Width = shapeSize;
            shape.Height = shapeSize;
            shape.ColorFillSolid = (fill is SolidBrush) ? ((SolidBrush)fill).Color : Color.Transparent;
            shape.BorderWidth = outline.Width;
            shape.BorderColor = outline.Color;
            shape.ShapeType = Shape;
            
            Bitmap bitmap = new Bitmap(shapeSize, shapeSize);
            Graphics g = Graphics.FromImage(bitmap);
            
            g.Clear(Color.Transparent);
            shape.Paint(g);
            symbol = bitmap;

            //update LegendSymbol
            Bitmap legendSymbolBitmap = new Bitmap(16, 16);
            g = Graphics.FromImage(legendSymbolBitmap);
            g.Clear(Color.Transparent);
            if (GeometryType == typeof(IPoint))
            {
                g.CompositingMode = CompositingMode.SourceOver;
                g.DrawImage(Symbol, 0, 0, legendSymbolBitmap.Width, legendSymbolBitmap.Height);
            }
            else if ((GeometryType == typeof(IPolygon)) || (GeometryType == typeof(IMultiPolygon)))
            {
                g.FillRectangle(Fill, 2, 3, 12, 10);
                g.DrawRectangle(Outline, 2, 3, 12, 10);
            }
            else if ((GeometryType == typeof(ILineString)) || (GeometryType == typeof(IMultiLineString)))
            {
                g.DrawLine(Outline, 2, 8, 14, 8);
                g.DrawLine(Line, 2, 8, 14, 8);
            }
            else
            {
                g.FillRectangle(Fill, 2, 3, 12, 10);
                g.DrawRectangle(Outline, 2, 3, 12, 10);
            }

            legendSymbol = legendSymbolBitmap;
            customSymbol = false;
        }

	    public override object Clone()
	    {
	        VectorStyle vectorStyle = new VectorStyle();

            vectorStyle.Shape = Shape;
            vectorStyle.ShapeSize = ShapeSize;
            if ((customSymbol) && (null != Symbol))
                vectorStyle.Symbol = (Bitmap)Symbol.Clone();

            vectorStyle.Line = Line == null ? null : (Pen) Line.Clone();
            vectorStyle.Outline = Outline == null ? null : (Pen)Outline.Clone();
            vectorStyle.EnableOutline = EnableOutline;
            vectorStyle.Fill = Fill == null ? null : (Brush)Fill.Clone();
            vectorStyle.GeometryType = GeometryType;
            //NS, 2013-09-17
            //vectorStyle.EnableDashStyle = EnableDashStyle;
            //vectorStyle.DashLine = DashLine == null ? null : (Pen)DashLine.Clone();
            //vectorStyle.DashReverse = DashReverse;
            //NS, 2013-09-23
            //if (_pointBrush != null)
            //    vectorStyle._pointBrush = _pointBrush.Clone() as Brush;
            //NS, 2013-10-02
            //vectorStyle.CircleLine = CircleLine == null ? null : (Pen)CircleLine.Clone();
            //EnOfNS
	        return vectorStyle;
	    }

	    public void Dispose()
	    {
            if (Line != null) Line.Dispose();
            if (Outline != null) Outline.Dispose();
            if (Fill != null) Fill.Dispose();
            if (Symbol != null) Symbol.Dispose();
            //NS, 2013-09-17
            if (DashLine != null) DashLine.Dispose();
            //NS, 2013-09-23
            //if (_pointBrush != null)
            //{
            //    _pointBrush.Dispose();
            //    _pointBrush = null;
            //}
            //NS, 2013-10-02
            if (CircleLine != null) CircleLine.Dispose();
        }

        /// <summary>
        /// In order to support proper serialization the outside world needs to know if the Symbol was set
        /// by an external source.
        /// </summary>
        /// <returns></returns>
        public bool HasCustomSymbol
	    {
            get { return customSymbol;}
	    }
	}
}
