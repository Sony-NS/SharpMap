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
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Drawing.Drawing2D;
using DelftTools.Utils.Drawing;
using log4net;
using SharpMap.Styles;
using SharpMap.Utilities;
using GeoAPI.Geometries;

namespace SharpMap.Rendering
{
	/// <summary>
	/// This class renders individual geometry features to a graphics object using the settings of a map object.
	/// </summary>
	public class VectorRenderingHelper
	{
        private static readonly ILog log = LogManager.GetLogger(typeof(VectorRenderingHelper));

        private const float nearZero = 1E-30f; // 1/Infinity
        private enum ClipState { Within, Outside, Intersecting };

	    static VectorRenderingHelper()
        {
        }

        /// <summary>
        /// Purpose of this method is to prevent the 'overflow error' exception in the FillPath method.
        /// This Exception is thrown when the coordinate values become too big (values over -2E+9f always
        /// throw an exception, values under 1E+8f seem to be okay). This method limits the coordinates to
        /// the values given by the second parameter (plus an minus). Theoretically the lines to and from
        /// these limited points are not correct but GDI+ paints incorrect even before that limit is reached.
        /// </summary>
        /// <param name="vertices">The vertices that need to be limited</param>
        /// <param name="limit">The limit at which coordinate values will be cutoff</param>
        /// <returns>The limited vertices</returns>
        private static System.Drawing.PointF[] LimitValues(System.Drawing.PointF[] vertices, float limit)
        {
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i].X = Math.Max(-limit, Math.Min(limit, vertices[i].X));
                vertices[i].Y = Math.Max(-limit, Math.Min(limit, vertices[i].Y));
            }
            return vertices;
        }

		/// <summary>
		/// Renders a MultiLineString to the map.
		/// </summary>
		/// <param name="g">Graphics reference</param>
		/// <param name="lines">MultiLineString to be rendered</param>
		/// <param name="pen">Pen style used for rendering</param>
		/// <param name="map">Map reference</param>
		public static void DrawMultiLineString(System.Drawing.Graphics g, IMultiLineString lines, System.Drawing.Pen pen, SharpMap.Map map)
		{
			for (int i = 0; i < lines.Geometries.Length; i++)
				DrawLineString(g, (ILineString)lines.Geometries[i], pen, map);
		}

		/// <summary>
		/// Renders a LineString to the map.
		/// </summary>
		/// <param name="g">Graphics reference</param>
		/// <param name="line">LineString to render</param>
		/// <param name="pen">Pen style used for rendering</param>
		/// <param name="map">Map reference</param>
		public static void DrawLineString(System.Drawing.Graphics g, ILineString line, System.Drawing.Pen pen, SharpMap.Map map)
		{
			if (line.Coordinates.Length > 1)
			{
				    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
				    //gp.AddLines(LimitValues(Transform.TransformToImage(line, map), extremeValueLimit));
                    gp.AddLines(Transform.TransformToImage(line, map));
				    g.DrawPath(pen, gp);
			}
		}

        /// <summary>
        /// Sony NS 2013-06-26 
        /// AddCurve for LineString type
        /// </summary>
        /// <param name="g"></param>
        /// <param name="line"></param>
        /// <param name="pen"></param>
        /// <param name="map"></param>
        public static void DrawCurve(System.Drawing.Graphics g, ILineString curveLine, System.Drawing.Pen pen, SharpMap.Map map)
        {
            if (curveLine.Coordinates.Length > 1)
            {
                System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();
                //gp.AddLines(LimitValues(Transform.TransformToImage(line, map), extremeValueLimit));
                gp.AddCurve(Transform.TransformToImage(curveLine, map));
                g.DrawPath(pen, gp);
            }
        }

		/// <summary>
		/// Renders a multipolygon byt rendering each polygon in the collection by calling DrawPolygon.
		/// </summary>
		/// <param name="g">Graphics reference</param>
		/// <param name="pols">MultiPolygon to render</param>
		/// <param name="brush">Brush used for filling (null or transparent for no filling)</param>
		/// <param name="pen">Outline pen style (null if no outline)</param>
		/// <param name="clip">Specifies whether polygon clipping should be applied</param>
		/// <param name="map">Map reference</param>
		public static void DrawMultiPolygon(System.Drawing.Graphics g, IMultiPolygon pols, System.Drawing.Brush brush, System.Drawing.Pen pen, bool clip, SharpMap.Map map)
		{
			for (int i = 0; i < pols.Geometries.Length; i++)
				DrawPolygon(g, (IPolygon)pols.Geometries[i], brush, pen, clip, map);
		}

		/// <summary>
		/// Renders a polygon to the map.
		/// </summary>
		/// <param name="g">Graphics reference</param>
		/// <param name="pol">Polygon to render</param>
		/// <param name="brush">Brush used for filling (null or transparent for no filling)</param>
		/// <param name="pen">Outline pen style (null if no outline)</param>
		/// <param name="clip">Specifies whether polygon clipping should be applied</param>
		/// <param name="map">Map reference</param>
		public static void DrawPolygon(System.Drawing.Graphics g, IPolygon pol, System.Drawing.Brush brush, System.Drawing.Pen pen, bool clip, SharpMap.Map map)
		{
            try
            {
                
			if (pol.Shell == null)
				return;
			if (pol.Shell.Coordinates.Length > 2)
			{
				//Use a graphics path instead of DrawPolygon. DrawPolygon has a problem with several interior holes
				System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();

				//Add the exterior polygon
				if (!clip)
                    gp.AddPolygon(Transform.TransformToImage(pol.Shell, map));
                    //gp.AddPolygon(LimitValues(Transform.TransformToImage(pol.Shell, map), extremeValueLimit));
				else
					gp.AddPolygon(clipPolygon(Transform.TransformToImage(pol.Shell, map), map.Size.Width, map.Size.Height));

				//Add the interior polygons (holes)
				for (int i = 0; i < pol.Holes.Length; i++)
					if (!clip)
                        gp.AddPolygon(Transform.TransformToImage(pol.Holes[i], map));
						//gp.AddPolygon(LimitValues(Transform.TransformToImage(pol.Holes[i], map), extremeValueLimit));
					else
						gp.AddPolygon(clipPolygon(Transform.TransformToImage(pol.Holes[i], map), map.Size.Width, map.Size.Height));

				// Only render inside of polygon if the brush isn't null or isn't transparent
				if (brush != null && brush != System.Drawing.Brushes.Transparent)
					g.FillPath(brush, gp);
				// Create an outline if a pen style is available
				if (pen != null)
					g.DrawPath(pen, gp);
			}
            }
            catch(InvalidOperationException e)
            {
                log.WarnFormat("Error during rendering", e);
            }
            catch (OverflowException e)
            {
                log.WarnFormat("Error during rendering", e);
            }
        }

        /// <summary>
        /// NS, 2013-12-02, draw circle inside polygon
        /// </summary>
        /// <param name="g"></param>
        /// <param name="pol"></param>
        /// <param name="brush"></param>
        /// <param name="pen"></param>
        /// <param name="clip"></param>
        /// <param name="map"></param>
        /// <param name="circleline"></param>
        /// <param name="drawcircle"></param>
        /// <param name="radius"></param>
        public static void DrawPolygonWithCircle(System.Drawing.Graphics g, IPolygon pol, System.Drawing.Brush brush,
            System.Drawing.Pen pen, bool clip, SharpMap.Map map, System.Drawing.Pen circleline, bool drawcircle, int radius)
        {
            try
            {
                if (pol.Shell == null)
                    return;
                if (pol.Shell.Coordinates.Length > 2)
                {
                    if (drawcircle)
                    {
                        System.Drawing.PointF pp = SharpMap.Utilities.Transform.WorldtoMap(pol.Centroid.Coordinate, map);                        
                        g.SmoothingMode = SmoothingMode.AntiAlias;
                        g.DrawEllipse(circleline, (pp.X - radius), (pp.Y - radius), radius * 2f, radius * 2f);
                    }

                    //Use a graphics path instead of DrawPolygon. DrawPolygon has a problem with several interior holes
                    System.Drawing.Drawing2D.GraphicsPath gp = new System.Drawing.Drawing2D.GraphicsPath();

                    //Add the exterior polygon
                    if (!clip)
                        gp.AddPolygon(Transform.TransformToImage(pol.Shell, map));
                    //gp.AddPolygon(LimitValues(Transform.TransformToImage(pol.Shell, map), extremeValueLimit));
                    else
                        gp.AddPolygon(clipPolygon(Transform.TransformToImage(pol.Shell, map), map.Size.Width, map.Size.Height));

                    //Add the interior polygons (holes)
                    for (int i = 0; i < pol.Holes.Length; i++)
                        if (!clip)
                            gp.AddPolygon(Transform.TransformToImage(pol.Holes[i], map));
                        //gp.AddPolygon(LimitValues(Transform.TransformToImage(pol.Holes[i], map), extremeValueLimit));
                        else
                            gp.AddPolygon(clipPolygon(Transform.TransformToImage(pol.Holes[i], map), map.Size.Width, map.Size.Height));

                    // Only render inside of polygon if the brush isn't null or isn't transparent
                    if (brush != null && brush != System.Drawing.Brushes.Transparent)
                        g.FillPath(brush, gp);
                    // Create an outline if a pen style is available
                    if (pen != null)
                        g.DrawPath(pen, gp);
                }
            }
            catch (InvalidOperationException e)
            {
                log.WarnFormat("Error during rendering", e);
            }
            catch (OverflowException e)
            {
                log.WarnFormat("Error during rendering", e);
            }
        }

		/// <summary>
		/// Renders a label to the map.
		/// </summary>
		/// <param name="g">Graphics reference</param>
		/// <param name="LabelPoint">Label placement</param>
		/// <param name="Offset">Offset of label in screen coordinates</param>
		/// <param name="font">Font used for rendering</param>
		/// <param name="forecolor">Font forecolor</param>
		/// <param name="backcolor">Background color</param>
		/// <param name="halo">Color of halo</param>
		/// <param name="rotation">Text rotation in degrees</param>
		/// <param name="text">Text to render</param>
		/// <param name="map">Map reference</param>
		public static void DrawLabel(System.Drawing.Graphics g, System.Drawing.PointF LabelPoint, System.Drawing.PointF Offset, System.Drawing.Font font, System.Drawing.Color forecolor, System.Drawing.Brush backcolor, System.Drawing.Pen halo, float rotation, string text, SharpMap.Map map)
		{
			System.Drawing.SizeF fontSize = g.MeasureString(text, font); //Calculate the size of the text
			LabelPoint.X += Offset.X; LabelPoint.Y += Offset.Y; //add label offset
			if (rotation != 0 && rotation != float.NaN)
			{
				g.TranslateTransform(LabelPoint.X, LabelPoint.Y);
				g.RotateTransform(rotation);
				g.TranslateTransform(-fontSize.Width / 2, -fontSize.Height / 2);
				if (backcolor != null && backcolor != System.Drawing.Brushes.Transparent)
					g.FillRectangle(backcolor, 0, 0, fontSize.Width * 0.74f + 1f, fontSize.Height * 0.74f);
				System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();
				path.AddString(text, font.FontFamily, (int)font.Style, font.Size, new System.Drawing.Point(0, 0), null);
				if (halo != null)
					g.DrawPath(halo, path);
				g.FillPath(new System.Drawing.SolidBrush(forecolor), path);
				//g.DrawString(text, font, new System.Drawing.SolidBrush(forecolor), 0, 0);				
				g.Transform = map.MapTransform;
			}
			else
			{
				if (backcolor != null && backcolor != System.Drawing.Brushes.Transparent)
					g.FillRectangle(backcolor, LabelPoint.X, LabelPoint.Y, fontSize.Width * 0.74f + 1, fontSize.Height * 0.74f);

				System.Drawing.Drawing2D.GraphicsPath path = new System.Drawing.Drawing2D.GraphicsPath();

				path.AddString(text, font.FontFamily, (int)font.Style, font.Size, LabelPoint, null);
				if (halo != null)
					g.DrawPath(halo, path);
				g.FillPath(new System.Drawing.SolidBrush(forecolor), path);
				//g.DrawString(text, font, new System.Drawing.SolidBrush(forecolor), LabelPoint.X, LabelPoint.Y);
			}
		}
		/*private System.Drawing.RectangleF GetPathEnvelope(System.Drawing.Drawing2D.GraphicsPath gp)
		{
			float minX = float.MaxValue; float minY = float.MaxValue;
			float maxX = float.MinValue; float maxY = float.MinValue;
			for(int i=0;i<gp.PointCount;i++)
				if(minX>gp.PathPoints[i].X)
		}*/

		/// <summary>
		/// Clips a polygon to the view.
		/// Based on UMN Mapserver renderer [This method is currently not used. It seems faster just to draw the outside points as well)
		/// </summary>
		/// <param name="vertices">vertices in image coordinates</param>
		/// <param name="width">Width of map in image coordinates</param>
		/// <param name="height">Height of map in image coordinates</param>
		/// <returns>Clipped polygon</returns>
		internal static System.Drawing.PointF[] clipPolygon(System.Drawing.PointF[] vertices, int width, int height)
		{
			float deltax, deltay, xin, xout, yin, yout;
			float tinx, tiny, toutx, touty, tin1, tin2, tout;
			float x1, y1, x2, y2;

			List<System.Drawing.PointF> line = new List<System.Drawing.PointF>();
			if (vertices.Length <= 1) /* nothing to clip */
				return vertices;
			/*
			** Don't do any clip processing of shapes completely within the
			** clip rectangle based on a comparison of bounds.   We could do 
			** something similar for completely outside, but that rarely occurs
			** since the spatial query at the layer read level has generally already
			** discarded all shapes completely outside the rect.
			*/

			// TODO
			//if (vertices.bounds.maxx <= width
			//		&& vertices.bounds.minx >= 0
			//		&& vertices.bounds.maxy <= height
			//		&& vertices.bounds.miny >= 0)
			//	{
			//		return vertices;
			//	}


			//line.point = (pointObj*)malloc(sizeof(pointObj) * 2 * shape->line[j].numpoints + 1); /* worst case scenario, +1 allows us to duplicate the 1st and last point */
			//line.numpoints = 0;

			for (int i = 0; i < vertices.Length - 1; i++)
			{
				x1 = vertices[i].X;
				y1 = vertices[i].Y;
				x2 = vertices[i + 1].X;
				y2 = vertices[i + 1].Y;

				deltax = x2 - x1;
				if (deltax == 0)
				{	// bump off of the vertical
					deltax = (x1 > 0) ? -float.MinValue : float.MinValue;
				}
				deltay = y2 - y1;
				if (deltay == 0)
				{	// bump off of the horizontal
					deltay = (y1 > 0) ? -float.MinValue : float.MinValue;
				}

				if (deltax > 0)
				{   //  points to right
					xin = 0;
					xout = width;
				}
				else
				{
					xin = width;
					xout = 0;
				}

				if (deltay > 0)
				{   //  points up
					yin = 0;
					yout = height;
				}
				else
				{
					yin = height;
					yout = 0;
				}

				tinx = (xin - x1) / deltax;
				tiny = (yin - y1) / deltay;

				if (tinx < tiny)
				{   // hits x first
					tin1 = tinx;
					tin2 = tiny;
				}
				else
				{   // hits y first
					tin1 = tiny;
					tin2 = tinx;
				}

				if (1 >= tin1)
				{
					if (0 < tin1)
						line.Add(new System.Drawing.PointF(xin, yin));

					if (1 >= tin2)
					{
						toutx = (xout - x1) / deltax;
						touty = (yout - y1) / deltay;

						tout = (toutx < touty) ? toutx : touty;

						if (0 < tin2 || 0 < tout)
						{
							if (tin2 <= tout)
							{
								if (0 < tin2)
								{
									if (tinx > tiny)
										line.Add(new System.Drawing.PointF(xin, y1 + tinx * deltay));
									else
										line.Add(new System.Drawing.PointF(x1 + tiny * deltax, yin));
								}

								if (1 > tout)
								{
									if (toutx < touty)
										line.Add(new System.Drawing.PointF(xout, y1 + toutx * deltay));
									else
										line.Add(new System.Drawing.PointF(x1 + touty * deltax, yout));
								}
								else
									line.Add(new System.Drawing.PointF(x2, y2));
							}
							else
							{
								if (tinx > tiny)
									line.Add(new System.Drawing.PointF(xin, yout));
								else
									line.Add(new System.Drawing.PointF(xout, yin));
							}
						}
					}
				}
			}
			if (line.Count > 0)
				line.Add(new System.Drawing.PointF(line[0].Y, line[0].Y));

			return line.ToArray();
		}

        public static void DrawCircle(Graphics g, IPoint point, int radius, Brush brush, Map map)
        {
            if (point == null)
                return;
            
            var pp = SharpMap.Utilities.Transform.WorldtoMap(point.Coordinate, map);
            
            g.CompositingMode = CompositingMode.SourceOver;
            
            g.FillEllipse(brush, (pp.X - radius), (pp.Y - radius), radius * 2f, radius * 2f);
        }

		/// <summary>
		/// Renders a point to the map.
		/// </summary>
		/// <param name="g">Graphics reference</param>
		/// <param name="point">Point to render</param>
		/// <param name="symbol">Symbol to place over point</param>
		/// <param name="symbolscale">The amount that the symbol should be scaled. A scale of '1' equals to no scaling</param>
		/// <param name="offset">Symbol offset af scale=1</param>
		/// <param name="rotation">Symbol rotation in degrees</param>
		/// <param name="map">Map reference</param>
		public static void DrawPoint(Graphics g, IPoint point, Bitmap symbol, float symbolscale, PointF offset, float rotation, Map map)
		{
			if (point == null)
				return;
			
			System.Drawing.PointF pp = SharpMap.Utilities.Transform.WorldtoMap(point.Coordinate, map);
			
			Matrix startingTransform = g.Transform;

            g.CompositingMode = CompositingMode.SourceOver;

            if (rotation != 0 && !Single.IsNaN(rotation))
			{
			    SizeF size = new SizeF(symbol.Width / 2, symbol.Height / 2);
                PointF rotationCenter = PointF.Add(new PointF(pp.X - size.Width, pp.Y - size.Height), size);

				Matrix transform = new Matrix();
				transform.RotateAt(rotation, rotationCenter);

				g.Transform = transform;

				if (symbolscale == 1f)
					g.DrawImageUnscaled(symbol, (int)(pp.X - symbol.Width / 2 + offset.X), (int)(pp.Y - symbol.Height / 2 + offset.Y));
				else
				{
					float width = symbol.Width * symbolscale;
					float height = symbol.Height * symbolscale;
					g.DrawImage(symbol, (int)pp.X - width / 2 + offset.X * symbolscale, (int)pp.Y - height / 2 + offset.Y * symbolscale, width, height);
				}

				g.Transform = startingTransform;
			}
			else
			{
				if (symbolscale == 1f)
					g.DrawImageUnscaled(symbol, (int)(pp.X - symbol.Width / 2 + offset.X), (int)(pp.Y - symbol.Height / 2 + offset.Y));
				else
				{
					float width = symbol.Width * symbolscale;
					float height = symbol.Height * symbolscale;
					g.DrawImage(symbol, (int)pp.X - width / 2 + offset.X * symbolscale, (int)pp.Y - height / 2 + offset.Y * symbolscale, width, height);
				}
			}
		}

        /// <summary>
        /// NS, 2013-10-01 Add circle outline in current symbol :)
        /// </summary>
        /// <param name="g"></param>
        /// <param name="point"></param>
        /// <param name="symbol"></param>
        /// <param name="symbolscale"></param>
        /// <param name="offset"></param>
        /// <param name="rotation"></param>
        /// <param name="map"></param>
        /// <param name="circleradius"></param>
        /// <param name="radius"></param>
        /// <param name="circleline"></param>
        public static void DrawPointWithCircle(Graphics g, IPoint point, Bitmap symbol, float symbolscale, PointF offset,
            float rotation, Map map, bool circleradius, int radius, Pen circleline)
        {
            if (point == null)
                return;

            System.Drawing.PointF pp = SharpMap.Utilities.Transform.WorldtoMap(point.Coordinate, map);

            Matrix startingTransform = g.Transform;

            g.CompositingMode = CompositingMode.SourceOver;

            if (circleradius)
            {
			    //NS, 2013-10-14, Add SmootingMode
			    g.SmoothingMode = SmoothingMode.AntiAlias;
                g.DrawEllipse(circleline, (pp.X - radius), (pp.Y - radius), radius * 2f, radius * 2f);
            }

            //NS, 2013-10-02, resize symbol
            //Size aSize = new Size((int)(symbol.Width * map.Zoom), (int)(symbol.Height * map.Zoom));
            //if (!aSize.IsEmpty)
            //{
            //    Bitmap bmpSymbol = new Bitmap(symbol, aSize);
            //    symbol = bmpSymbol;
            //}

            if (rotation != 0 && !Single.IsNaN(rotation))
            {
                SizeF size = new SizeF(symbol.Width / 2, symbol.Height / 2);
                PointF rotationCenter = PointF.Add(new PointF(pp.X - size.Width, pp.Y - size.Height), size);

                Matrix transform = new Matrix();
                transform.RotateAt(rotation, rotationCenter);

                g.Transform = transform;

                if (symbolscale == 1f)
                    g.DrawImageUnscaled(symbol, (int)(pp.X - symbol.Width / 2 + offset.X), (int)(pp.Y - symbol.Height / 2 + offset.Y));
                else
                {
                    float width = symbol.Width * symbolscale;
                    float height = symbol.Height * symbolscale;
                    g.DrawImage(symbol, (int)pp.X - width / 2 + offset.X * symbolscale, (int)pp.Y - height / 2 + offset.Y * symbolscale, width, height);
                }

                g.Transform = startingTransform;
            }
            else
            {
                if (symbolscale == 1f)
                    g.DrawImageUnscaled(symbol, (int)(pp.X - symbol.Width / 2 + offset.X), (int)(pp.Y - symbol.Height / 2 + offset.Y));
                else
                {
                    float width = symbol.Width * symbolscale;
                    float height = symbol.Height * symbolscale;
                    g.DrawImage(symbol, (int)pp.X - width / 2 + offset.X * symbolscale, (int)pp.Y - height / 2 + offset.Y * symbolscale, width, height);
                }
            }
        }

		/// <summary>
		/// Renders a <see cref="SharpMap.Geometries.MultiPoint"/> to the map.
		/// </summary>
		/// <param name="g">Graphics reference</param>
		/// <param name="points">MultiPoint to render</param>
		/// <param name="symbol">Symbol to place over point</param>
		/// <param name="symbolscale">The amount that the symbol should be scaled. A scale of '1' equals to no scaling</param>
		/// <param name="offset">Symbol offset af scale=1</param>
		/// <param name="rotation">Symbol rotation in degrees</param>
		/// <param name="map">Map reference</param>
        public static void DrawMultiPoint(Graphics g, IMultiPoint points, Bitmap symbol, float symbolscale, PointF offset, float rotation, Map map)
		{
			for (int i = 0; i < points.Geometries.Length; i++)
				DrawPoint(g, (IPoint)points.Geometries[i], symbol, symbolscale, offset, rotation, map);
		}

        public static void DrawMultiPointWithCircle(Graphics g, IMultiPoint points, Bitmap symbol, float symbolscale, PointF offset,
            float rotation, Map map, bool circleradius, int radius, Pen circleline)
        {
            for (int i = 0; i < points.Geometries.Length; i++)
                DrawPointWithCircle(g, (IPoint)points.Geometries[i], symbol, symbolscale, offset, rotation, map, circleradius, radius, circleline);
        }

        /// <summary>
        /// Renders a geometry to the screen depending on the geometry type.
        /// </summary>
        /// <param name="g">The graphics object used to draw geometries.</param>
        /// <param name="map">The map the geometry belongs to and is rendered onto.</param>
        /// <param name="feature">The feature of which his geometry will be rendered.</param>
        /// <param name="style">The style to use when rendering the geometry.</param>
        /// <param name="defaultSymbol">The default symbology to use when none is specified by the style.</param>
        /// <param name="clippingEnabled">If rendering clipping is enabled.</param>
        public static void RenderGeometry(Graphics g, Map map, IGeometry feature, VectorStyle style, Bitmap defaultSymbol, bool clippingEnabled)
        {
            Bitmap symbol = style.Symbol;

            switch (feature.GeometryType)
            {
                case "Polygon":
                    //NS, 2013-12-02, Add centroid poin inside polygon as guide coordinate for gpshistory
                    if ((style.EnableCircleRadius) && (style.EnableOutline))
                    {
                        DrawPolygonWithCircle(g, (IPolygon)feature, style.Fill, style.Outline, clippingEnabled, map,
                            style.CircleLine, style.EnableCircleRadius, style.CircleRadius);
                    }
                    else
                        //NS, 2013-12-02, Add centroid poin inside polygon as guide coordinate for gpshistory
                        if ((style.EnableCircleRadius))
                        {
                            DrawPolygonWithCircle(g, (IPolygon)feature, style.Fill, null, clippingEnabled, map,
                                style.CircleLine, style.EnableCircleRadius, style.CircleRadius);
                        }
                        else
                            if (style.EnableOutline)
                                DrawPolygon(g, (IPolygon)feature, style.Fill, style.Outline, clippingEnabled, map);
                            else
                                DrawPolygon(g, (IPolygon)feature, style.Fill, null, clippingEnabled, map);
                    break;
                case "MultiPolygon":
                    if (style.EnableOutline)
                        DrawMultiPolygon(g, (IMultiPolygon)feature, style.Fill, style.Outline, clippingEnabled, map);
                    else
                        DrawMultiPolygon(g, (IMultiPolygon)feature, style.Fill, null, clippingEnabled, map);
                    break;
                case "Curve":
                    DrawCurve(g, (ILineString)feature, style.Line, map);
                    if (style.EnableDashStyle == true)
                        VectorRenderingHelper.DrawDashLineString(g, (ILineString)feature, style.DashLine, map, style.DashReverse);
                    break;
                case "LineString":
                    //NS, 2014-07-17
                    if (!style.IsCurve)
                        DrawLineString(g, (ILineString)feature, style.Line, map);
                    else //NS, 2013-06-26
                        DrawCurve(g, (ILineString)feature, style.Line, map);
                    //NS, 2013-09-16
                    if (style.EnableDashStyle == true)
                        VectorRenderingHelper.DrawDashLineString(g, (ILineString)feature, style.DashLine, map, style.DashReverse);
                    break;
                case "MultiLineString":
                    DrawMultiLineString(g, (IMultiLineString)feature, style.Line, map);
                    break;
                case "Point":
                    if (symbol == null)
                    {
                        symbol = defaultSymbol;
                    }
                    //NS, 2013-10-02, berguna pada saat highlight point....
                    if (style.EnableCircleRadius)
                    {
                        DrawPointWithCircle(g, (IPoint)feature, symbol, style.SymbolScale, style.SymbolOffset, style.SymbolRotation, 
                            map, true, style.CircleRadius, style.CircleLine);
                    }
                    DrawPoint(g, (IPoint)feature, symbol, style.SymbolScale,
                                             style.SymbolOffset, style.SymbolRotation, map);
                    break;
                case "MultiPoint":
                    if (symbol == null)
                    {
                        symbol = defaultSymbol;
                    }
                    //NS, 2013-10-02, berguna pada saat highlight point....
                    if (style.EnableCircleRadius)
                    {
                        VectorRenderingHelper.DrawMultiPointWithCircle(g, (IMultiPoint)feature, symbol, style.SymbolScale, 
                            style.SymbolOffset, style.SymbolRotation, map, true, style.CircleRadius, style.CircleLine);
                    }
                    VectorRenderingHelper.DrawMultiPoint(g, (IMultiPoint)feature, symbol, style.SymbolScale,
                                                  style.SymbolOffset, style.SymbolRotation, map);
                    break;
                case "GeometryCollection":
                    IGeometryCollection geometryCollection = (IGeometryCollection)feature;
                    for (int i = 0; i < geometryCollection.Count; i++)
                    {
                        RenderGeometry(g, map, geometryCollection[i], style, defaultSymbol, clippingEnabled);
                    }

                    break;
                default:
                    break;
            }
        }

        public static ShapeType GetIndexedShapeType(int index)
        {
            Array symbols = Enum.GetValues(typeof(ShapeType));
            return (ShapeType)symbols.GetValue(index % symbols.Length);
        }

        /// <summary>
        /// NS, 2013-09-16
        /// Draw dash style for arrow, in curve lineString
        /// </summary>
        /// <param name="g"></param>
        /// <param name="p"></param>
        /// <param name="dash"></param>
        /// <param name="gap"></param>
        /// <param name="s"></param>
        /// <param name="e"></param>
        /// <param name="ignoreLength"></param>
        private static void DrawDashedLine(System.Drawing.Graphics g, System.Drawing.Pen p, float dash, float gap,
            System.Drawing.PointF s, System.Drawing.PointF e, bool ignoreLength)
        {
            float dx = e.X - s.X;
            float dy = e.Y - s.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            float remainder = len;
            float vx = dx / len;
            float vy = dy / len;

            if (len <= dash + gap)
            {
                if (len >= dash && ignoreLength == true)
                {
                    e = new System.Drawing.PointF(s.X + vx * dash, s.Y + vy * dash);
                    g.DrawLine(p, s, e);
                }
                return;
            }

            System.Drawing.PointF last = s;

            while (remainder > dash + gap)
            {
                System.Drawing.PointF p1 = new System.Drawing.PointF(last.X, last.Y);
                System.Drawing.PointF p2 = new System.Drawing.PointF(p1.X + vx * dash, p1.Y + vy * dash);
                //System.Drawing.PointF p3 = new System.Drawing.PointF(p2.X + 3, p2.Y + 3);
                
                g.DrawLine(p, p1, p2);
                
                last = new System.Drawing.PointF(p2.X + vx * gap, p2.Y + vy * gap);

                remainder = remainder - dash - gap;
            }

            //if (remainder > 0)
            //{
            //    g.DrawLine(p, last, e);
            //}
        }

        /// <summary>
        /// NS, 2013-09-16
        /// Dash style for arrow (direction street)
        /// </summary>
        /// <param name="g"></param>
        /// <param name="line"></param>
        /// <param name="pen"></param>
        /// <param name="map"></param>
        /// <param name="reverse"></param>
        public static void DrawDashLineString(System.Drawing.Graphics g, ILineString line, 
            System.Drawing.Pen pen, SharpMap.Map map, bool reverse)
        {
            if (line.Coordinates.Length > 1)
            {
                System.Drawing.PointF[] arrPoint = Transform.TransformToImage(line, map);
                if (arrPoint.Length >= 2)
                {
                    bool ignoreLength = false;
                    if (reverse == false)
                    {
                        for (int k = 0; k < arrPoint.Length - 1; k++)
                        {
                            ignoreLength = (k % 5 == 0) ? true : false;
                            DrawDashedLine(g, pen, 10, 50, arrPoint[k], arrPoint[k + 1], ignoreLength);
                        }
                    }
                    else
                    {
                        for (int k = arrPoint.Length - 1; k >= 1; k--)
                        {
                            ignoreLength = (k % 5 == 0) ? true : false;
                            DrawDashedLine(g, pen, 10, 50, arrPoint[k], arrPoint[k - 1], ignoreLength);
                        }
                    }
                }
            }
        }
	}
}
