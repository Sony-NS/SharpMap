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
    public class MoveTool : MapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(MoveTool));

        protected ICoordinate LastMouseLocation { get; set; }
        protected ICoordinate MouseDownLocation { get; set; }
        private IFallOffPolicy fallOffPolicy;
        private bool isBusy;
        private IFeature dragSource;
        private IFeature dragTarget;
        private VectorLayer targetLayer;
        private ITrackerFeature snappingSource;
        private ISnapResult SnapResult { get; set; }
        private ITrackerFeature TrackerFeature { get; set; }

        //public Keys CancelKey { get; set; }

        public FallOffPolicyRule FallOffPolicy
        {
            get
            {
                return null != fallOffPolicy ? fallOffPolicy.FallOffPolicy : FallOffPolicyRule.None;
            }
            set
            {
                switch (value)
                {
                    case FallOffPolicyRule.Linear:
                        fallOffPolicy = new LinearFallOffPolicy();
                        break;
                    case FallOffPolicyRule.None:
                        fallOffPolicy = new NoFallOffPolicy();
                        break;
                    default:
                        break;
                }
            }
        }

        public MoveTool()
        {
            //CancelKey = Keys.Escape;
            dragLayers = new List<VectorLayer>();
            Name = "Move";
            fallOffPolicy = new NoFallOffPolicy();
        }

        private readonly List<VectorLayer> dragLayers;
        private List<VectorLayer> DragLayers
        {
            get { return dragLayers; }
        }
        private void ResetDragLayers()
        {
            foreach (VectorLayer layer in dragLayers)
            {
                foreach (IFeatureRenderer featureRenderer in layer.CustomRenderers)
                {
                    if (featureRenderer is IDisposable)
                    {
                        ((IDisposable)featureRenderer).Dispose();
                    }
                }
            }
            DragLayers.Clear();
        }
        private VectorLayer GetDragLayer(string name)
        {
            foreach (VectorLayer vectorLayer in DragLayers)
            {
                if (vectorLayer.Name == name)
                    return vectorLayer;
            }
            return null;
        }

        public override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render(e.Graphics, MapControl.Map);
        }

        public override void Render(Graphics graphics, Map mapBox)
        {
            MapControl.SelectTool.Render(graphics, mapBox);
            foreach (VectorLayer vectorLayer in DragLayers)
            {
                // Render the dragLayer; bypass ILayer.Render and call OnRender directly; this is much more efficient
                // It only usefull to use ILayer.Render when layer contents are not modified between draw
                // operations.
                vectorLayer.OnRender(graphics, mapBox);
            }
            MapControl.SnapTool.Render(graphics, mapBox);
        }

        private VectorStyle selectionStyle;
        private VectorStyle errorSelectionStyle;

        /// <summary>
        /// StartDragging prepares dragging of feature
        /// </summary>
        /// <param name="temp"></param>
        /// <param name="feature"></param>
        /// <returns></returns>
        private IFeature StartDragging(ICoordinate temp, IFeature feature)
        {
            // No feature selected; do not drag
            if (null == feature)
                return null;
            // Is it allowed to drag the selected feature?
            if (!MapControl.SelectTool.FeatureEditors[0].AllowMove())
                return null;

            StartDrawing();
            ResetDragLayers();

            // There is 1 feature selected in the selectionLayer of the selecttool. Update the style of this layer based
            // on the style of the 'source' layer
            VectorLayer sourceLayer = (VectorLayer)Map.GetLayerByFeature(feature);
			//NS: 2013-08-15 jika layer yang terpilih == null
            try
            {
                selectionStyle = (VectorStyle)sourceLayer.Style.Clone();
                errorSelectionStyle = (VectorStyle)sourceLayer.Style.Clone();
                MapControlHelper.PimpStyle(selectionStyle, true);
                MapControlHelper.PimpStyle(errorSelectionStyle, false);

                MapControl.SelectTool.FeatureEditors[0].FallOffPolicy = fallOffPolicy;
                MapControl.SelectTool.FeatureEditors[0].WorkerFeatureCreated += MoveTool_WorkerFeatureCreated;
                MapControl.SelectTool.FeatureEditors[0].Start();
                dragTarget = MapControl.SelectTool.FeatureEditors[0].TargetFeature;
                TrackerFeature = MapControl.SelectTool.FeatureEditors[0].GetTrackerAtCoordinate(temp);
                targetLayer = AddFeatureToDragLayers(feature, dragTarget);
            }
            catch
            {
                //DoNothing :)
            }

            return feature;
        }

        void MoveTool_WorkerFeatureCreated(IFeature sourceFeature, IFeature workFeature)
        {
            AddFeatureToDragLayers(sourceFeature, workFeature);
        }

        private void DoDragging()
        {
            DoDrawing(true);
        }


        private void EndDragging()
        {
            StopDrawing();
            ResetDragLayers();
        }

        private VectorLayer AddFeatureToDragLayers(IFeature sourceFeature, IFeature cloneFeature)
        {
            VectorLayer sourceLayer = (VectorLayer) Map.GetLayerByFeature(sourceFeature);
            // NOTE: sourceLayer should never return null 
            if (null == sourceLayer)
            {
                throw new ArgumentOutOfRangeException("sourceFeature", "Movetool unable to find sourcelayer; internal corruption caused by removed feature?");
            }
            VectorLayer dragLayer = GetDragLayer(sourceLayer.Name);
            if (null == dragLayer)
            {
                dragLayer = new VectorLayer(sourceLayer);

                foreach (var customRenderer in sourceLayer.CustomRenderers)
                {
                    var renderer = customRenderer as ICloneable;
                    if (null != renderer)
                    {
                        dragLayer.CustomRenderers.Add((IFeatureRenderer)renderer.Clone());
                    }
                }

                MapControlHelper.PimpStyle(dragLayer.Style, true);

                var dragFeatures = new FeatureCollection();
                dragLayer.DataSource = dragFeatures;
                dragLayer.Map = Map;
                if (sourceLayer.DataSource is FeatureCollection)
                {
                    ((FeatureCollection)dragLayer.DataSource).FeatureType = sourceLayer.DataSource.FeatureType;
                }
                DragLayers.Add(dragLayer);
            }

            if(sourceLayer.Visible && !dragLayer.DataSource.Contains(cloneFeature))
            {
                dragLayer.DataSource.Features.Add(cloneFeature);
                dragLayer.RenderRequired = true;
            }

            return dragLayer;
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            MapControl.SnapTool.Reset();
            SelectTool selectTool = MapControl.SelectTool;
            IFeature oldSelectedFeature = null;
            IList<ITrackerFeature> focusedTrackers = new List<ITrackerFeature>();
            ITrackerFeature trackerFeature = selectTool.GetTrackerAtCoordinate(worldPosition);
            
            if (null != trackerFeature)
            {
                oldSelectedFeature = trackerFeature.FeatureEditor.SourceFeature;
                focusedTrackers = trackerFeature.FeatureEditor.GetFocusedTrackers();
            }

            // Let the selecttool handle the mouse event unless multiple trackers have focus and
            // there is no key pressed. In this case the user expects to move the focused trackers
            // and SelectTool will reset them
            if (!((focusedTrackers.Count > 1) && (!selectTool.KeyToggleSelection) &&
                (!selectTool.KeyExtendSelection)/* && (trackerFeature.Selected)*/))
            {
                selectTool.OnMouseDown(worldPosition, e);
                // did we just deselect out only selected tracker?
                if (null != trackerFeature)
                {
                    int focusedTrackersCount = trackerFeature.FeatureEditor.GetFocusedTrackers().Count;
                    if ((focusedTrackers.Count != focusedTrackersCount) && (0 == focusedTrackersCount))
                        return;
                }
            }

            if (e.Button != MouseButtons.Left)
            {
                return;
            }
            if (1 != selectTool.FeatureEditors.Count)
            {
                return;
            }
            dragSource = null;
            if (selectTool.FeatureEditors.Count == 1)
            {
                isBusy = true;
                IFeature feature = selectTool.FeatureEditors[0].SourceFeature;
                if (oldSelectedFeature != feature)
                {
                    if (!selectTool.FeatureEditors[0].AllowSingleClickAndMove())
                    {
                        isBusy = false;
                        return;
                    }
                }

                if (!selectTool.FeatureEditors[0].AllowMove())
                {
                    isBusy = false;
                    return;
                }

                // TODO: this code looks too complicated
                //IFeatureProvider featureProvider = selectTool.MultiSelection[0].Layer.DataSource;
                IFeatureProvider featureProvider = selectTool.FeatureEditors[0].Layer.DataSource;
                // IndexOf doesn;'t work on shapefiles; featurerows are recreated during each read
                int dragIndex = featureProvider.Features.IndexOf(feature);
                if (-1 == dragIndex)
                {
                    isBusy = false;
                    return;
                }
                dragSource = StartDragging(worldPosition, featureProvider.GetFeature(dragIndex));
                if (null == dragSource)
                {
                    isBusy = false;
                    return;
                }
            }
            else
            {
                return;
            }
            LastMouseLocation = worldPosition;
            MouseDownLocation = worldPosition;
            snappingSource = null;
            IList<ITrackerFeature> list = selectTool.FeatureEditors[0].GetFocusedTrackers();
            if (null == list)
                return;
            if (list.Count <= 0)
                return;
            if (list.Count == 1 )
                snappingSource = list[0];
            return;
        }

        /// <summary>
        /// Synchronise trackers of the selection with the geometry. This is only necessay when a topology rule
        /// has applied some special adjustments to the geometry during an operation. 
        /// e.g adjusting the angle of a non geometry based cross section.
        /// </summary>
        /// <param name="geometry"></param>
        private void SynchroniseSelectionTrackers(IGeometry geometry)
        {
            MapControl.SelectTool.FeatureEditors[0].UpdateTracker(geometry);
        }

        /// <summary>
        /// Processes the mouse movement. Moving an object only works when the left mouse button is pressed (drawgging).
        /// Always call the selecttool to do the default processing such as setting the correct cursor.
        /// </summary>
        /// <param name="worldPosition"></param>
        /// <param name="e"></param>
        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            MapControl.SelectTool.OnMouseMove(worldPosition, e);
            if (null == LastMouseLocation)
                return;
            if ((worldPosition.X == LastMouseLocation.X) && (worldPosition.Y == LastMouseLocation.Y))
                return;
            if ((e.Button != MouseButtons.Left) && (MapControl.SelectTool.IsActive)) // HACK snapping logic should only in curve
                return; 
            //NS, 2013-09-13
            //if (MapControl.SelectTool.FeatureEditors[0].SourceFeature.Geometry is GeoAPI.Geometries.ILineString)
            //{
            //    GisSharpBlog.NetTopologySuite.Geometries.LineString line =
            //        MapControl.SelectTool.FeatureEditors[0].TargetFeature.Geometry as GisSharpBlog.NetTopologySuite.Geometries.LineString;

            //    if (((worldPosition.X == line.StartPoint.X) && (worldPosition.Y == line.StartPoint.Y)) &&
            //        ((worldPosition.X == line.EndPoint.X) && (worldPosition.Y == line.EndPoint.Y)))
            //    {
            //        //return;
            //        throw new ArgumentOutOfRangeException("NS Message...", "StartPoint or EndPoint cannot be move!");
            //    }
            //}

            //NS, 2013-10-14, hanya feture yang diedit yang bisa digeser....:)
            if (MapControl.SelectTool.FeatureEditors[0].SourceFeature.Attributes.Keys.Contains("Editing"))
            {
                if (!(bool)MapControl.SelectTool.FeatureEditors[0].SourceFeature.Attributes["Editing"]) return;
            }

            SnapResult = null;
            //if (null != dragSource)
            //NS: 2013-08-15, jika dragTarget == NULL error....
            if ((null != dragSource) && (null != dragTarget))
            {
                SnapResult = MapControl.SnapTool.ExecuteLayerSnapRules(MapControl.SelectTool.FeatureEditors[0].Layer,
                                                                dragSource, dragTarget.Geometry, worldPosition,
                                                                (null == snappingSource) ? -1 : snappingSource.Index);

                targetLayer.Style = (null == SnapResult) ? errorSelectionStyle : selectionStyle;
            }
            if (null != dragTarget)
            {
                bool adjustGeometry = MapControl.SelectTool.FeatureEditors[0].UpdateDefaultGeometry(
                    (null != SnapResult) ? SnapResult.SnappedFeature : null, 
                                            dragTarget, 
                                            (null != SnapResult) ? SnapResult.Location : worldPosition);

                if (adjustGeometry)
                {
                    SynchroniseSelectionTrackers(dragTarget.Geometry);
                }
                else 
                {
                    if ((null != SnapResult) && (null != snappingSource))
                    {
                        MoveSelection(SnapResult.Location.X - dragTarget.Geometry.Coordinates[snappingSource.Index].X,
                                        SnapResult.Location.Y - dragTarget.Geometry.Coordinates[snappingSource.Index].Y);

                        //NS, 2014-05-02
                        //Untuk point awal dan akhir, apabila diedit maka akan mengikuti salah satu titik yang di gesernya (awal/akhir) 
                        if ((snappingSource.Index == 0) && (dragTarget.Geometry.GeometryType == "Polygon"))
                        {
                            ITrackerFeature aTrackerFeature = MapControl.SelectTool.FeatureEditors[0].GetTrackerAtCoordinate(dragTarget.Geometry.Coordinates[dragTarget.Geometry.NumPoints - 1]);
                            if (null != aTrackerFeature)
                            {
                                //Tracker
                                //MapControl.SelectTool.FeatureEditors[0].MoveTracker(aTrackerFeature,
                                //    TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].X,
                                //    TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].Y, SnapResult);
                                //Point
                                aTrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[dragTarget.Geometry.NumPoints - 1].X =
                                    TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].X;
                                aTrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[dragTarget.Geometry.NumPoints - 1].Y =
                                    TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].Y;
                            }
                        }
                        else
                            if ((snappingSource.Index == (dragTarget.Geometry.NumPoints - 1)) && (dragTarget.Geometry.GeometryType == "Polygon"))
                            {
                                ITrackerFeature aTrackerFeature = MapControl.SelectTool.FeatureEditors[0].GetTrackerAtCoordinate(dragTarget.Geometry.Coordinates[0]);
                                if (null != aTrackerFeature)
                                {
                                    //Tracker
                                    //MapControl.SelectTool.FeatureEditors[0].MoveTracker(aTrackerFeature,
                                    //    TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].X,
                                    //    TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].Y, SnapResult);
                                    //Point
                                    aTrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[0].X =
                                        TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].X;
                                    aTrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[0].Y =
                                        TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[snappingSource.Index].Y;
                                }
                            }
                    }
                    else
                    {
                        MoveSelection(worldPosition.X - LastMouseLocation.X, worldPosition.Y - LastMouseLocation.Y);
                        //NS, 2014-05-06
                        //Jika polygon di geser, pastikan point terakhir mengikuti point awal
                        if (dragTarget.Geometry.GeometryType == "Polygon")
                        {
                            ITrackerFeature aTrackerFeature = MapControl.SelectTool.FeatureEditors[0].GetTrackerAtCoordinate(dragTarget.Geometry.Coordinates[dragTarget.Geometry.NumPoints - 1]);
                            if (null != aTrackerFeature)
                            {
                                aTrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[dragTarget.Geometry.NumPoints - 1].X =
                                        TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[0].X;
                                aTrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[dragTarget.Geometry.NumPoints - 1].Y =
                                        TrackerFeature.FeatureEditor.TargetFeature.Geometry.Coordinates[0].Y;
                            }
                        }
                    }
                }
                DoDragging();
            }
            LastMouseLocation = worldPosition;
        }
        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (isBusy)
            {
                EndDragging();

                if (null != SnapResult)
                //if (!MapControl.SnapTool.Failed)
                {
                    // only execute move when mouse has been moved.
                    if (!((worldPosition.X == MouseDownLocation.X) && (worldPosition.Y == MouseDownLocation.Y)))
                    {
                        if (MapControl.SelectTool.FeatureEditors.Count > 0)
                        {
                            // hack? sourceLayer doesn't have to be part of a network; thus we are
                            // required to force repaint. DataSource has no knowledge of layer.
                            VectorLayer sourceLayer = (VectorLayer)Map.GetLayerByFeature(dragSource);
                            var featureEditor = MapControl.SelectTool.FeatureEditors[0];
                            featureEditor.EditableObject.BeginEdit(string.Format("Move feature {0}",
                                         featureEditor.SourceFeature is INameable
                                             ? ((INameable)featureEditor.SourceFeature).Name
                                             : ""));
                            featureEditor.Stop(SnapResult);
                            featureEditor.EditableObject.EndEdit();
                            //// hack? sourceLayer doesn't have to be part of a network; thus we are
                            //// required to force repaint. DataSource has no knowledge of layer.
                            //VectorLayer sourceLayer = (VectorLayer)Map.GetLayerByFeature(dragSource);
                            //sourceLayer.RenderRequired = true;
                            sourceLayer.RenderRequired = true;
                        }
                    }
                    Cleanup();
                    //if (MapControl.SelectTool.FeatureEditors.Count > 0)
                    //{
                    //    MapControl.SelectTool.FeatureEditors[0].SourceFeature.Geometry = dragTarget.Geometry;
                    //    //MapControl.SelectTool.UpdateSelection(dragTarget.Geometry);
                    //}
                    MapControl.SnapTool.Reset();
                }
                else
                {
                    Cancel();
                }
                dragSource = null;
                dragTarget = null;
            }
            // forward handling to selecttool; enable selection cycling
            MapControl.SelectTool.OnMouseUp(worldPosition, e);
        }

        public override bool IsBusy
        {
            get { return isBusy; }
        }

        public void SetCoordinate(IFeature feature, int coordinateIndex, ICoordinate coordinate)
        {
            IGeometry geometry = feature.Geometry;
            Collection<IGeometry> trackers = new Collection<IGeometry>();
            List<int> handleIndices = new List<int>();
            for (int i = 0; i < geometry.Coordinates.Length; i++)
            {
                trackers.Add(GeometryFactory.CreatePoint((ICoordinate)geometry.Coordinates[i].Clone()));
            }
            handleIndices.Add(coordinateIndex);

            IFeatureProvider provider = GetFeatureProviderByFeature(feature);
            int geometryIndex = provider.IndexOf(feature);

            if (geometryIndex == -1)
            {
                log.DebugFormat("Can't find layer for geometry (via feature): {0}", geometry);
                return;
            }

            IGeometry updatedGeometry = (IGeometry)geometry.Clone();

            double deltaX = coordinate.X - geometry.Coordinates[coordinateIndex].X;
            double deltaY = coordinate.Y - geometry.Coordinates[coordinateIndex].Y;

            fallOffPolicy.Reset();
            fallOffPolicy.Move(updatedGeometry, trackers, handleIndices, coordinateIndex, deltaX, deltaY);
            fallOffPolicy.Reset();

            ((IFeature)provider.Features[geometryIndex]).Geometry = updatedGeometry;
        }
        public void MoveSelection(double deltaX, double deltaY)
        {
            if (null == dragTarget)
                return;
            if (null != TrackerFeature)
                MapControl.SelectTool.FeatureEditors[0].MoveTracker(TrackerFeature, deltaX, deltaY, SnapResult);
        }

        private void Cleanup()
        {
            fallOffPolicy.Reset();
            LastMouseLocation = null;
            dragSource = null;
            isBusy = false;
        }

        public override void Cancel()
        {
            EndDragging();
            Cleanup();
        }

    }
}
