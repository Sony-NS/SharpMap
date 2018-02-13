/* 
 * Created  : Sony NS @ SNC Bandung, 2013-11-11 
 * Descript : Polygon Tool.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using DelftTools.Utils;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using SharpMap.Converters.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Topology;
using SharpMap.UI.Editors;
using SharpMap.UI.FallOff;
using SharpMap.UI.Helpers;
using SharpMap.Styles;
using SharpMap.UI.Snapping;

namespace SharpMap.UI.Tools
{
    public class DrawPolygonTool : MapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DrawPolygonTool));

        private bool isBusy;
        private bool isAddPoint;
        private Point _dragStartPoint;
        private Point _dragEndPoint;
        //private Bitmap _dragImage;
        private Rectangle _rectangle = Rectangle.Empty;
        private bool _dragging;
        private ICoordinate _dragStartCoord;
        private double _orgScale;
        //public bool IsDrawPolygon;
        public List<ICoordinate> pointArray = new List<ICoordinate>();
        //private VectorLayer _newPolygonLayer;
        private readonly Collection<IGeometry> _newPolygonGeometry = new Collection<IGeometry>();

        public DrawPolygonTool()
        {
            Name = "DrawPolygon";
            pointArray = null;
            isBusy = false;
            _rectangle = Rectangle.Empty;
        }

        //private void AddDrawingLayer()
        //{
        //    _newPolygonLayer = new VectorLayer((VectorLayer)Layer)
        //    {
        //        RenderRequired = true,
        //        Name = "newLine",
        //        Map = Layer.Map
        //    };

        //    DataTableFeatureProvider trackingProvider = new DataTableFeatureProvider(_newPolygonGeometry);
        //    _newPolygonLayer.DataSource = trackingProvider;
        //    MapControlHelper.PimpStyle(_newPolygonLayer.Style, true);
        //}

        //private void RemoveDrawingLayer()
        //{
        //    _newPolygonLayer = null;
        //}

        //public override void StartDrawing()
        //{
        //    base.StartDrawing();
        //    AddDrawingLayer();
        //}
        //public override void StopDrawing()
        //{
        //    base.StopDrawing();
        //    RemoveDrawingLayer();
        //}

        //NS, 2013-11-07
        private Point ClipPoint(Point p)
        {
            var x = p.X < 0 ? 0 : (p.X > MapControl.ClientSize.Width ? MapControl.ClientSize.Width : p.X);
            var y = p.Y < 0 ? 0 : (p.Y > MapControl.ClientSize.Height ? MapControl.ClientSize.Height : p.Y);
            return new Point(x, y);
        }

        //NS, 2013-11-07
        private static Rectangle GenerateRectangle(Point p1, Point p2)
        {
            var x = Math.Min(p1.X, p2.X);
            var y = Math.Min(p1.Y, p2.Y);
            var width = Math.Abs(p2.X - p1.X);
            var height = Math.Abs(p2.Y - p1.Y);

            return new Rectangle(x, y, width, height);
        }

        //public override void Render(Graphics graphics, Map mapBox)
        //{
        //    if (null == _newPolygonLayer)
        //        return;
        //    _newPolygonLayer.Render();
        //    graphics.DrawImage(_newPolygonLayer.Image, 0, 0);
        //    MapControl.SnapTool.Render(graphics, mapBox);
        //}

        public override void OnPaint(PaintEventArgs e)
        {
            if (_dragging)
            {
                e.Graphics.DrawImageUnscaled(Map.Image, 0, 0);                
            }
            //Draws current line or polygon (Draw Line or Draw Polygon tool)
            if ((pointArray != null))
            {
                if (pointArray.Count == 1)
                {
                    var p1 = Map.WorldToImage(pointArray[0]);
                    var p2 = Map.WorldToImage(pointArray[1]);
                    e.Graphics.DrawLine(new Pen(Color.Red, 2F), p1, p2);
                }
                else
                {
                    PointF[] pts = new PointF[pointArray.Count];
                    for (int i = 0; i < pts.Length; i++)
                        pts[i] = Map.WorldToImage(pointArray[i]);

                    if (MapControl.DrawPolygonTool.IsActive)
                    {
                        //Warna polygon
                        Color c = Color.FromArgb(127, Color.GhostWhite);
                        e.Graphics.FillPolygon(new SolidBrush(c), pts);
                        //Warna garis...
                        e.Graphics.DrawPolygon(new Pen(Color.Red, 2F), pts);
                    }
                    else
                        e.Graphics.DrawLines(new Pen(Color.Red, 2F), pts);
                }
            }

            base.OnPaint(e);
        }

        public override void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            //IPolygon polygon = (IPolygon)
            //if (IsActive)
            //{
            //    if (MapControl.GeometryDefined)
            //    {
            //        var cl = new GisSharpBlog.NetTopologySuite.Geometries.CoordinateList(_pointArray, false);
            //        cl.CloseRing();
            //        MapControl.GeometryDefined(GeometryFactory.CreatePolygon(GeometryFactory.CreateLinearRing(GisSharpBlog.NetTopologySuite.Geometries.CoordinateArrays.AtLeastNCoordinatesOrNothing(4, cl.ToCoordinateArray())), null));
            //    }
                isBusy = false;
                isAddPoint = false;
            //}
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if ((e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle)) //dragging
            {
                isBusy = true;
                _dragStartPoint = e.Location;
                _dragEndPoint = e.Location;
                _dragStartCoord = Map.Center;
                _orgScale = Map.Zoom;
                isAddPoint = true;
            }
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            //base.OnMouseMove(e);
            if (Map != null)
            {
                bool isStartDrag = Map != null && e.Location != _dragStartPoint && !_dragging &&
                                   (e.Button == MouseButtons.Left || e.Button == MouseButtons.Middle) &&
                    //Left of middle button can start drag
                                   !(MapControl.DrawPolygonTool.IsActive); //It should not be any of these tools

                if (isStartDrag)
                {
                    _dragging = true;
                }

                if (_dragging)
                {
                    //bool isPanOperation = true;
                    //if (IsDrawPolygon)
                    //{
                    //    isPanOperation = true;
                    //}

                    //if (isPanOperation)
                    //{
                    _dragEndPoint = ClipPoint(e.Location);
                    if (_dragStartCoord != null)
                    {
                        GisSharpBlog.NetTopologySuite.Geometries.Coordinate newCoord = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate();
                        newCoord.X = _dragStartCoord.X - Map.PixelSize * (_dragEndPoint.X - _dragStartPoint.X);
                        newCoord.Y = _dragStartCoord.Y - Map.PixelSize * (_dragStartPoint.Y - _dragEndPoint.Y);
                        Map.Center = newCoord;

                        //if (MapCenterChanged != null)
                        //    MapCenterChanged(map.Center);

                        MapControl.Invalidate(MapControl.ClientRectangle);
                    }
                    //}
                }
                else
                {
                    _dragEndPoint = new Point(0, 0);
                    if (pointArray != null)
                    {
                        pointArray[pointArray.Count - 1] = Map.ImageToWorld(ClipPoint(e.Location));
                        _rectangle = GenerateRectangle(_dragStartPoint, ClipPoint(e.Location));
                        MapControl.Invalidate(new Region(MapControl.ClientRectangle));
                    }
                }
            }            
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (Map != null)
            {
                if (pointArray == null)
                {
                    pointArray = new List<ICoordinate>(2);
                    pointArray.Add(Map.ImageToWorld(e.Location));
                    pointArray.Add(Map.ImageToWorld(e.Location));
                }
                else
                {
                    //var temp = new Coordinate[_pointArray.Count + 2];
                    pointArray.Add(Map.ImageToWorld(e.Location));
                }
            }
        }

        public override void ActiveToolChanged(IMapTool newTool)
        {
            isAddPoint = false;
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

    }
}
