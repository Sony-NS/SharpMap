using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Mapping;
using GeoPoint = GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace SharpMap.UI.Tools
{
    public class MeasureTool : MapTool
    {
        private IList<IGeometry> geometries;
        private IEnumerable<GeoPoint> pointGeometries;
        private VectorLayer pointLayer;
        private double distanceInMeters;

        public MeasureTool(MapControl mapControl)
            : base(mapControl)
        {
            geometries = new List<IGeometry>();
            pointGeometries = geometries.OfType<GeoPoint>();

            pointLayer = new VectorLayer();
            pointLayer.Name = "measure";
            pointLayer.DataSource = new DataTableFeatureProvider(geometries);
            pointLayer.Style.Symbol = TrackerSymbolHelper.GenerateSimple(Pens.DarkMagenta, Brushes.Indigo, 6, 6);
            pointLayer.Visible = false;
            pointLayer.ShowInLegend = false;
        }

        /// <summary>
        /// Use this property to enable or disable tool. When the measure tool is deactivated, it cleans up old measurements.
        /// </summary>
        public override bool IsActive
        {
            get
            {
                return base.IsActive;
            }
            set
            {
                base.IsActive = value;
                if (!IsActive)
                    Clear();
            }
        }

        public override void ActiveToolChanged(IMapTool newTool)
        {
            // TODO: It seems this is never called, so it is also cleared when the IsActive property is (re)set
            Clear();
            base.ActiveToolChanged(newTool);
        }

        /// <summary>
        /// Clean up set coordinates and distances for a fresh future measurement
        /// </summary>
        private void Clear()
        {
            geometries.Clear();
            pointLayer.DataSource.Features.Clear();
            distanceInMeters = double.MinValue;
        }

        public override void OnMouseDown(GeoAPI.Geometries.ICoordinate worldPosition, System.Windows.Forms.MouseEventArgs e)
        {
            // Starting a new measurement?
            if (pointGeometries.Count() >= 2 && Control.ModifierKeys != Keys.Alt)
            {
                Clear();
            }

            // Add the newly selected point
            var point = new GeoPoint(worldPosition);
            pointLayer.DataSource.Add(point);

            CalculateDistance();

            // Refresh the screen
            pointLayer.RenderRequired = true;
            MapControl.Refresh(); // HACK: Why is this needed? (Only RenderRequired = true isn't enough...)
            
            base.OnMouseDown(worldPosition, e);
        }

        /// <summary>
        /// Calculate distance in meters between the two selected points
        /// </summary>
        private void CalculateDistance()
        {
            var points = pointGeometries.ToList();

            if (points.Count >= 2)
            {
                // Convert the world coordinates into actual meters using a geodetic calculation helper 
                // class. The height is not taken into account (since this information is missing).
                // TODO: Use the elipsoid appropriate to the current map layers loaded
                /*GeodeticCalculator calc = new GeodeticCalculator();
                GlobalCoordinates gp0 = new GlobalCoordinates(pointGometries[0].Coordinate.X, pointGometries[0].Coordinate.Y);
                GlobalCoordinates gp1 = new GlobalCoordinates(pointGometries[1].Coordinate.X, pointGometries[1].Coordinate.Y);
                GeodeticCurve gc = calc.CalculateGeodeticCurve(Ellipsoid.WGS84, gp0, gp1);
                distanceInMeters = gc.EllipsoidalDistance;*/
                // HACK: For now use a plain coordinates to meters calculation like Sobek normally uses (RD or Amersfoort)
                
                distanceInMeters = 0.0;
                for (int i = 1; i < points.Count; i++)
                {
                    distanceInMeters += Math.Sqrt(
                        Math.Pow(points[i].Coordinate.X - points[i - 1].Coordinate.X, 2) +
                        Math.Pow(points[i].Coordinate.Y - points[i - 1].Coordinate.Y, 2));
                }

                // Show a line indicator
                //pointLayer.DataSource.Features
                var existingLine = pointLayer.DataSource.Features.OfType<LineString>().FirstOrDefault();
                if (existingLine != null)
                {
                    pointLayer.DataSource.Features.Remove(existingLine);
                }

                var lineGeometry = new LineString(points.Select(g => g.Coordinate).ToArray());
                pointLayer.DataSource.Add(lineGeometry);
            }
        
        }

        /// <summary>
        /// Painting of the measure tool (the selected points, a connecting line and the distance in text)
        /// </summary>
        /// <param name="e"></param>
        public override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render(e.Graphics, MapControl.Map);
        }

        /// <summary>
        /// Visual rendering of the measurement (two line-connected points and the text)
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="map"></param>
        public override void Render(Graphics graphics, Map map)
        {
            pointLayer.Map = map;
            pointLayer.Visible = true;
            pointLayer.RenderRequired = true;
            pointLayer.Render();
            graphics.DrawImageUnscaled(pointLayer.Image, 0, 0);

            // Show the distance in text
            if (geometries.Count >= 2)
            {
                Font distanceFont = new Font("Arial", 10);
                Map.WorldToImage(geometries[1].Coordinate);
                PointF textPoint = Map.WorldToImage(geometries[1].Coordinate);
                if (distanceInMeters > double.MinValue)
                    graphics.DrawString(distanceInMeters.ToString("N") + "m", distanceFont, Brushes.Black, textPoint);
            }
        }
        
    }
}
