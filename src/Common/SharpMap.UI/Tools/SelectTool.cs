using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using GeoAPI.Geometries;
using log4net;
using SharpMap.Converters.Geometries;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Rendering.Thematics;
using SharpMap.Styles;
using SharpMap.UI.Editors;
using SharpMap.UI.Forms;
using SharpMap.UI.Helpers;
using GeoAPI.Extensions.Feature;
using System.ComponentModel;
using DelftTools.Utils;
using DelftTools.Utils.Collections;

namespace SharpMap.UI.Tools
{
    public enum MultiSelectionMode
    {
        Rectangle = 0,
        Lasso
    }

    /// <summary>
    /// SelectTool enables users to select features in the map
    /// The current implementation supports:
    /// - single selection feature by click on feature
    /// - multiple selection of feature by dragging a rectangle
    /// - adding features to the selection (KeyExtendSelection; normally the SHIFT key)
    /// - toggling selection of features (KeyToggleSelection; normally the CONTROL key)
    ///    if featues is not in selection it is added to selection
    ///    if feature is in selection it is removed from selection
    /// - Selection is visible to the user via trackers. Features with an IPoint geometry have 1 
    ///   tracker, based on ILineString and IPolygon have a tracker for each coordinate
    /// - Trackers can have focus. 
    ///   If a trackers has focus is visible to the user via another symbol (or same symbol in other color)
    ///   A tracker that has the focus is the tracker leading during special operation such as moving. 
    ///   For single selection a feature with an IPoint geometry automatically get the focus to the 
    ///   only tracker
    /// - Multiple trackers with focus
    /// - adding focus trackers (KeyExtendSelection; normally the SHIFT key)
    /// - toggling focus trackers (KeyToggleSelection; normally the CONTROL key)
    /// - Selection cycling, When multiple features overlap clicking on a selected feature will
    ///   result in the selection of the next feature. Compare behavior in Sobek Netter.
    /// 
    /// TODO
    /// - functionality reasonably ok, but TOO complex : refactor using tests
    /// - Selection cycling can be improved:
    ///     - for a ILineString the focus tracker is not set initially which can be set in the second
    ///       click. Thus a ILineString (and IPolygon) can eat a click
    ///     - if feature must be taken into account by selection cycling should be an option
    ///       (topology rule?)
    /// </summary>
    public class SelectTool : MapTool
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(SelectTool));

        public MultiSelectionMode MultiSelectionMode { get; set; }
        
        // TODO: these feature editor-related fields are not at home in SelectTool.
        public FeatureEditorCreationEventHandler FeatureEditorCreation;
        public IList<IFeatureEditor> FeatureEditors { get; private set; }
        private readonly Collection<ITrackerFeature> trackers = new Collection<ITrackerFeature>();
        private ICoordinateConverter coordinateConverter;
        
        /// <summary>
        /// Current layer where features are being selected (branch, nodes, etc.)
        /// </summary>
        private VectorLayer TrackingLayer // will be TrackingLayer containing tracking geometries
        {
            get { return trackingLayer; }
        }

        public IList<int> SelectedTrackerIndices
        {
            get
            {
                List<int> indices = new List<int>();
                return 1 == FeatureEditors.Count ? FeatureEditors[0].GetFocusedTrackerIndices() : indices;
            }
        }

        public bool KeyToggleSelection
        {
            get { return ((Control.ModifierKeys & Keys.Control) == Keys.Control); }
        }
        public bool KeyExtendSelection
        {
            get { return ((Control.ModifierKeys & Keys.Shift) == Keys.Shift); }
        }

        public SelectTool()
        {
            orgClickTime = DateTime.Now;
            FeatureEditors = new List<IFeatureEditor>();
            Name = "Select";

            trackingLayer.Name = "trackers";
            FeatureCollection trackerProvider = new FeatureCollection { Features = trackers };

            trackingLayer.DataSource = trackerProvider;

            CustomTheme iTheme = new CustomTheme(GetTrackerStyle);
            trackingLayer.Theme = iTheme;
        }

        private bool IsMultiSelect { get; set; }

        public override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Render(e.Graphics, MapControl.Map);
        }

        public override void Render(Graphics graphics, Map map)
        {
            // Render the selectionLayer and trackingLayer
            // Bypass ILayer.Render and call OnRender directly; this is more efficient
            foreach (var tracker in trackers)
            {
                if (null != tracker.FeatureEditor.SourceFeature)
                {
                    // todo optimize this; only necessary when map extent has changed.
                    tracker.FeatureEditor.UpdateTracker(tracker.FeatureEditor.SourceFeature.Geometry);
                }
            }
            trackingLayer.OnRender(graphics, map);
        }


        public ITrackerFeature GetTrackerAtCoordinate(ICoordinate worldPos)
        {
            ITrackerFeature trackerFeature = null;
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                trackerFeature = FeatureEditors[i].GetTrackerAtCoordinate(worldPos);
                if (null != trackerFeature)
                    break;
            }
            return trackerFeature;
        }

        private ICoordinate orgMouseDownLocation;
        private DateTime orgClickTime;
        private bool clickOnExistingSelection;
        private void SetClickOnExistingSelection(bool set, ICoordinate worldPosition)
        {
            clickOnExistingSelection = set;
            if (clickOnExistingSelection)
            {
                orgMouseDownLocation = (ICoordinate)worldPosition.Clone();
            }
            else
            {
                orgMouseDownLocation = null;
            }
        }

        private IFeatureEditor GetActiveMutator(IFeature feature)
        {
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                if (FeatureEditors[i].SourceFeature == feature)
                    return FeatureEditors[i];
            }
            return null;
        }

        public override void OnMouseDown(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left)
            {
                return;
            }

            var oldSelectedTrackerIndicesCount = SelectedTrackerIndices.Count;
            var oldTrackerFeatureCount = trackers.Count;

            IsBusy = true;
            ILayer selectedLayer;
            mouseDownLocation = worldPosition;

            // Check first if an object is already selected and if the mousedown has occured at this object.
            ITrackerFeature trackerFeature = GetTrackerAtCoordinate(worldPosition);
            if (FeatureEditors.Count > 1)
            {
                // hack: if multiple selection toggle/select complete feature
                trackerFeature = null;
            }

            SetClickOnExistingSelection(false, null);

            if (null != trackerFeature)
            {
                if (1 == FeatureEditors.Count)
                {
                    SetClickOnExistingSelection(true, worldPosition);
                    FocusTracker(trackerFeature);
                    MapControl.Refresh();

                }
                return;
            }
            // single selection. Find the nearest geometry and give 
            float limit = (float)MapHelper.ImageToWorld(Map, 4);
            IFeature nearest = FindNearestFeature(worldPosition, limit, out selectedLayer, ol => ol.Visible);
            if (null != nearest)
            {
                // Create or add a new FeatureEditor
                if (FeatureEditors.Count > 0)
                {
                    IFeatureEditor currentMutator = GetActiveMutator(nearest);
                    if (KeyExtendSelection)
                    {
                        if (null == currentMutator)
                        {
                            // not in selection; add
                            AddSelection(selectedLayer, nearest, -1, true);
                        } // else possibly set default focus tracker
                    }
                    else if (KeyToggleSelection)
                    {
                        if (null == currentMutator)
                        {
                            // not in selection; add
                            AddSelection(selectedLayer, nearest, -1, true);
                        }
                        else
                        {
                            // in selection; remove
                            RemoveSelection(nearest);
                        }
                    }
                    else
                    {
                        // no special key processing; handle as a single select.
                        Clear(false);
                        if (!StartSelection(selectedLayer, nearest, -1))
                        {
                            StartMultiSelect();
                        }
                        //AddSelection(selectedLayer, nearest, -1);
                    }
                }
                else
                {
                    if (!StartSelection(selectedLayer, nearest, -1))
                    {
                        StartMultiSelect();
                    }
                    //AddSelection(selectedLayer, nearest, -1);
                }
            }
            else
            {
                // We didn't find an object at the position of the mouse button -> start a multiple select
                if (!KeyExtendSelection)
                {
                    // we are not extending the current selection
                    Clear(false);
                }
                if (e.Button == MouseButtons.Left)
                //if (IsActive)
                {
                    StartMultiSelect();
                }
            }

            if ((oldSelectedTrackerIndicesCount != SelectedTrackerIndices.Count
                || oldTrackerFeatureCount != trackers.Count) && trackingLayer.DataSource.Features.Count != 0)
            {
                MapControl.Refresh();
            }

            //NS, 2013-09-04
            //Convert LineString to Curve: if numPoint = 2 then convert it...
            //if ((null != nearest)&&(nearest.Geometry.NumPoints == 2))
            //{
            //    // todo ?move to FeatureEditor and add support for polygon
            //    IFeatureEditor featureEditor = GetActiveMutator(nearest);

            //    if (featureEditor.SourceFeature.Geometry is ILineString)
            //    {
            //        featureEditor.EditableObject.BeginEdit(string.Format("Insert curvepoint into feature {0}",
            //                     featureEditor.SourceFeature is DelftTools.Utils.INameable
            //                     ? ((DelftTools.Utils.INameable)featureEditor.SourceFeature).Name
            //                     : ""));
            //        //featureEditor.Stop(SnapResult);
            //        ConvertToCurve(featureEditor.SourceFeature, featureEditor.Layer);
            //        featureEditor.EditableObject.EndEdit();
            //    }
            //    featureEditor.Layer.RenderRequired = true;
            //    MapControl.Refresh();
            //    //return;
            //}
        }

        /// <summary>
        /// NS, 2013-09-04
        /// add control point 
        /// </summary>
        /// <param name="aFeature"></param>
        //private void ConvertToCurve(IFeature aFeature, ILayer aLayer)
        //{
        //    ICoordinate startPoint, endPoint, controlPoint1, controlPoint2;
        //    if ((aFeature != null) && (aFeature.Geometry is ILineString) && (aFeature.Geometry.NumPoints == 2))
        //    {
        //        GisSharpBlog.NetTopologySuite.Geometries.LineString line =
        //            aFeature.Geometry as GisSharpBlog.NetTopologySuite.Geometries.LineString;
        //        startPoint = GeometryFactory.CreateCoordinate(line.StartPoint.X, line.StartPoint.Y);
        //        endPoint = GeometryFactory.CreateCoordinate(line.EndPoint.X, line.EndPoint.Y);

        //        /*
        //         * control point didapat dengan cara (ref: FlexGraphics VCL):
        //         * controlPointA.x := startPoint.x + (endPoint.x - startPoint.x) div 3;
        //         * controlPointA.y := startPoint.y + (endPoint.y - startPoint.y) div 3;
        //         * controlPointB.x := endPoint.x - (endPoint.x - startPoint.x) div 3;
        //         * controlPointB.y := endPoint.y - (endPoint.y - startPoint.y) div 3;
        //         */
        //        double aX, aY, bX, bY;
        //        aX = startPoint.X + (endPoint.X - startPoint.X) / 3;
        //        aY = startPoint.Y + (endPoint.Y - startPoint.Y) / 3;
        //        bX = endPoint.X - (endPoint.X - startPoint.X) / 3;
        //        bY = endPoint.Y - (endPoint.Y - startPoint.Y) / 3;

        //        controlPoint1 = GeometryFactory.CreateCoordinate(aX, aY);
        //        controlPoint2 = GeometryFactory.CreateCoordinate(bX, bY);

        //        //Create new LineString as Curve
        //        List<ICoordinate> vertices = new List<ICoordinate>();
        //        vertices.Add(startPoint);
        //        vertices.Add(controlPoint1);
        //        vertices.Add(controlPoint2);
        //        vertices.Add(endPoint);

        //        ILineString newLineString = GeometryFactory.CreateLineString(vertices.ToArray());
        //        //SharpMap.UI.Tools.SelectTool selectTool = MapControl.SelectTool;
        //        //selectTool.FeatureEditors[0].SourceFeature.Geometry = newLineString;
        //        SharpMap.Layers.ILayer targetLayer = aLayer;
        //        //update geometry to curve...
        //        aFeature.Geometry = newLineString;
        //        Select(targetLayer, aFeature, 2);
        //    }
        //}

        private void StartMultiSelect()
        {
            IsMultiSelect = true;
            selectPoints.Clear();
            UpdateMultiSelection(mouseDownLocation);
            StartDrawing();
        }

        private void StopMultiSelect()
        {
            IsMultiSelect = false;
            StopDrawing();
        }

        /// <summary>
        /// Returns styles used by tracker features.
        /// </summary>
        /// <param name="feature"></param>
        /// <returns></returns>
        private static VectorStyle GetTrackerStyle(IFeature feature)
        {
            var trackerFeature = (TrackerFeature)feature;

            VectorStyle style;

            // styles are stored in the cache for performance reasons
            lock (stylesCache)
            {
                if (!stylesCache.ContainsKey(trackerFeature.Bitmap))
                {
                    style = new VectorStyle { Symbol = trackerFeature.Bitmap };
                    stylesCache[trackerFeature.Bitmap] = style;
                }
                else
                {
                    style = stylesCache[trackerFeature.Bitmap];
                }
            }

            return style;
        }

        static IDictionary<Bitmap, VectorStyle> stylesCache = new Dictionary<Bitmap, VectorStyle>();

        public void Clear()
        {
            Clear(true);
        }

        private void Clear(bool fireSelectionChangedEvent)
        {
            FeatureEditors.Clear();
            if (trackingLayer.DataSource.GetFeatureCount() <= 0)
                return;
            trackers.Clear();
            trackingLayer.RenderRequired = true;
            UpdateMapControlSelection(fireSelectionChangedEvent);
        }


        private void SynchronizeTrackers()
        {
            trackers.Clear();
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                foreach (ITrackerFeature trackerFeature in FeatureEditors[i].GetTrackers())
                {
                    //NS, 2013-10-14, tambah kondisi jika feture tsb diedit baru render tracker pointnya
                    if (trackerFeature.FeatureEditor.SourceFeature.Attributes.Keys.Contains("Editing"))
                    {
                        if ((bool)trackerFeature.FeatureEditor.SourceFeature.Attributes["Editing"])
                            trackers.Add(trackerFeature);
                    }
                }
            }
            trackingLayer.RenderRequired = true;

        }

        // TODO, HACK: what SelectTool has to do with FeatureEditor? Refactor it.
        public IFeatureEditor GetFeatureEditor(ILayer layer, IFeature feature)
        {
            try
            {
                IFeatureEditor featureEditor = null;

                if (FeatureEditorCreation != null)
                {
                    // allow custom feature editor creation
                    featureEditor = FeatureEditorCreation(layer, feature,
                                                          (layer is VectorLayer) ? ((VectorLayer)layer).Style : null);
                }
                if (null == featureEditor)
                {
                    // no custom feature editor; fall back to default editors.
                    featureEditor = FeatureEditorFactory.Create(coordinateConverter, layer, feature,
                                                                (layer is VectorLayer)
                                                                    ? ((VectorLayer)layer).Style
                                                                    : null);
                }
                return featureEditor;
            }
            catch (Exception exception)
            {
                log.Error("Error creating feature editor: " + exception.Message);
                return null;
            }
        }

        private bool StartSelection(ILayer layer, IFeature feature, int trackerIndex)
        {
            IFeatureEditor featureEditor = GetFeatureEditor(layer, feature);
            if (null == featureEditor)
                return false;
            if (featureEditor.AllowSingleClickAndMove())
            {
                // do not yet select, but allow MltiSelect
                FeatureEditors.Add(featureEditor);
                SynchronizeTrackers();
                UpdateMapControlSelection();
                return true;
            }
            return false;
        }

        public void AddSelection(IEnumerable<IFeature> features)
        {
            foreach (IFeature feature in features)
            {
                var layer = Map.GetLayerByFeature(feature);
                AddSelection(layer, feature, 0, false);
            }
            UpdateMapControlSelection();
        }


        public void AddSelection(ILayer layer, IFeature feature)
        {
            AddSelection(layer, feature, 0, true);
        }

        public void AddSelection(ILayer layer, IFeature feature, int trackerIndex, bool synchronizeUI)
        {
            if (!layer.Visible)
            {
                return;
            }
            IFeatureEditor featureEditor = GetFeatureEditor(layer, feature);
            if (null == featureEditor)
                return;
            FeatureEditors.Add(featureEditor);
            
            if (synchronizeUI)
            {
                UpdateMapControlSelection();
            }
        }

        public IEnumerable<IFeature> Selection
        {
            get
            {
                foreach (IFeatureEditor featureEditor in FeatureEditors)
                {
                    yield return featureEditor.SourceFeature;
                }
            }
        }

        //public void UpdateSelection(IGeometry geometry) // HACK: select tool must select features, not edit them
        //{
        //    FeatureEditors[0].SourceFeature.Geometry = geometry;
        //}

        private void RemoveSelection(IFeature feature)
        {
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                if (FeatureEditors[i].SourceFeature == feature)
                {
                    FeatureEditors.RemoveAt(i);
                    break;
                }
            }
            UpdateMapControlSelection();
        }


        /// <summary>
        /// Sets the selected object in the selectTool. SetSelection supports also the toggling/extending the 
        /// selected trackers.
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="featureLayer"></param>
        /// <param name="trackerIndex"></param>
        /// <returns>A clone of the original object.</returns>
        /// special cases 
        /// feature is ILineString or IPolygon and trackerIndex != 1 : user clicked an already selected 
        /// features -> only selected tracker changes.
        private void SetSelection(IFeature feature, ILayer featureLayer, int trackerIndex)
        {
            if (null != feature)
            {
                // store selected trackers
                IList<int> featureTrackers = new List<int>();
                for (int i = 0; i < TrackingLayer.DataSource.Features.Count; i++)
                {
                    TrackerFeature trackerFeature = (TrackerFeature)TrackingLayer.DataSource.Features[i];
                    if (trackerFeature == feature)
                    {
                        featureTrackers.Add(i);
                    }
                }
                // store selected objects 
                AddSelection(featureLayer, feature, trackerIndex, true);
            }
        }
        
        private void FocusTracker(ITrackerFeature trackFeature)
        {
            if (null == trackFeature)
                return;

            if (!((KeyToggleSelection) || (KeyExtendSelection)))
            {
                for (int i = 0; i < FeatureEditors.Count; i++)
                {
                    foreach (ITrackerFeature tf in FeatureEditors[i].GetTrackers())
                    {
                        FeatureEditors[i].Select(tf, false);
                    }
                }
            }
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                foreach (TrackerFeature tf in FeatureEditors[i].GetTrackers())
                {
                    if (tf == trackFeature)
                    {
                        if (KeyToggleSelection)
                        {
                            FeatureEditors[i].Select(trackFeature, !trackFeature.Selected);
                        }
                        else
                        {
                            FeatureEditors[i].Select(trackFeature, true);
                        }
                    }
                }
            }
        }

        private List<PointF> selectPoints = new List<PointF>();
        //private bool lassoSelect = false;

        private void UpdateMultiSelection(ICoordinate worldPosition)
        {
            if (MultiSelectionMode == MultiSelectionMode.Lasso)
            {
                selectPoints.Add(Map.WorldToImage(worldPosition));
            }
            else
            {
                WORLDPOSITION = worldPosition;
            }
        }

        private IPolygon CreatePolygon(double left, double top, double right, double bottom)
        {
            var vertices = new List<ICoordinate>
                                   {
                                       GeometryFactory.CreateCoordinate(left, bottom),
                                       GeometryFactory.CreateCoordinate(right, bottom),
                                       GeometryFactory.CreateCoordinate(right, top),
                                       GeometryFactory.CreateCoordinate(left, top)
                                   };
            vertices.Add((ICoordinate)vertices[0].Clone());
            ILinearRing newLinearRing = GeometryFactory.CreateLinearRing(vertices.ToArray());
            return GeometryFactory.CreatePolygon(newLinearRing, null);

        }

        private IPolygon CreateSelectionPolygon(ICoordinate worldPosition)
        {
            if (MultiSelectionMode == MultiSelectionMode.Rectangle)
            {
                if (0 == Math.Abs(mouseDownLocation.X - worldPosition.X))
                {
                    return null;
                }
                if (0 == Math.Abs(mouseDownLocation.Y - worldPosition.Y))
                {
                    return null;
                }
                return CreatePolygon(Math.Min(mouseDownLocation.X, worldPosition.X),
                                             Math.Max(mouseDownLocation.Y, worldPosition.Y),
                                             Math.Max(mouseDownLocation.X, worldPosition.X),
                                             Math.Min(mouseDownLocation.Y, worldPosition.Y));
            }
            var vertices = new List<ICoordinate>();

            foreach (var point in selectPoints)
            {
                vertices.Add(Map.ImageToWorld(point));
            }
            if (vertices.Count == 1)
            {
                // too few points to create a polygon
                return null;
            }
            vertices.Add((ICoordinate)worldPosition.Clone());
            vertices.Add((ICoordinate)vertices[0].Clone());
            ILinearRing newLinearRing = GeometryFactory.CreateLinearRing(vertices.ToArray());
            return GeometryFactory.CreatePolygon(newLinearRing, null);
        }

        private ICoordinate mouseDownLocation; // TODO: remove me
        private ICoordinate WORLDPOSITION;
        public override void OnDraw(Graphics graphics)
        {
            if (MultiSelectionMode == MultiSelectionMode.Lasso)
            {
                GraphicsHelper.DrawSelectionLasso(graphics, KeyExtendSelection ? Color.Magenta : Color.DeepSkyBlue, selectPoints.ToArray());
            }
            else
            {
                ICoordinate coordinate1 = GeometryFactory.CreateCoordinate(mouseDownLocation.X, mouseDownLocation.Y);
                ICoordinate coordinate2 = GeometryFactory.CreateCoordinate(WORLDPOSITION.X, WORLDPOSITION.Y);
                PointF point1 = Map.WorldToImage(coordinate1);
                PointF point2 = Map.WorldToImage(coordinate2);
                GraphicsHelper.DrawSelectionRectangle(graphics, KeyExtendSelection ? Color.Magenta : Color.DeepSkyBlue, point1, point2);
            }
        }

        public override void OnMouseMove(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (IsMultiSelect)
            {
                //WORLDPOSITION = worldPosition;
                UpdateMultiSelection(worldPosition);
                DoDrawing(false);
                return;
            }

            Cursor cursor = null;
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                ITrackerFeature trackerFeature = FeatureEditors[i].GetTrackerAtCoordinate(worldPosition);
                if (null != trackerFeature)
                {
                    cursor = FeatureEditors[i].GetCursor(trackerFeature);
                }
            }
            if (null == cursor)
            {
                cursor = Cursors.Default;
            }

            MapControl.Cursor = cursor;
        }
        private void UpdateMapControlSelection()
        {
            UpdateMapControlSelection(true);
        }
        private void UpdateMapControlSelection(bool fireSelectionChangedEvent)
        {
            SynchronizeTrackers();

            IList<IFeature> selectedFeatures = new List<IFeature>();
            for (int i = 0; i < FeatureEditors.Count; i++)
            {
                selectedFeatures.Add(FeatureEditors[i].SourceFeature);
            }    

            MapControl.SelectedFeatures = selectedFeatures;

            if (fireSelectionChangedEvent && SelectionChanged != null)
            {
                SelectionChanged(this, null);
            }

        }
        public override void OnMouseDoubleClick(object sender, MouseEventArgs e)
        {
            orgMouseDownLocation = null;
        }

        public override void OnMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            if (IsMultiSelect)
            {
                HandleMultiSelectMouseUp(worldPosition);
            }
            else
            {
                HandleMouseUp(worldPosition, e);
            }
            IsBusy = false;
            orgClickTime = DateTime.Now;
        }

        private void HandleMouseUp(ICoordinate worldPosition, MouseEventArgs e)
        {
            if ((null != orgMouseDownLocation) && (orgMouseDownLocation.X == worldPosition.X) &&
                (orgMouseDownLocation.Y == worldPosition.Y) && (e.Button == MouseButtons.Left))
            {
                // check if mouse was pressed at a selected object without moving the mouse. The default behaviour 
                // should be to select 'the next' object
                TimeSpan timeSpan = DateTime.Now - orgClickTime;
                int dc = SystemInformation.DoubleClickTime;
                if (dc < timeSpan.TotalMilliseconds)
                {
                    if (1 == FeatureEditors.Count)
                    {
                        // check if selection exists; could be toggled
                        Layer outLayer;
                        IFeature nextFeature = GetNextFeatureAtPosition(worldPosition,
                            // set limit from 4 to 10: TOOLS-1499
                                                                        (float)MapHelper.ImageToWorld(Map, 10),
                                                                        out outLayer,
                                                                        FeatureEditors[0].SourceFeature,
                                                                        ol => ol.Visible);
                        if (null != nextFeature)
                        {
                            Clear(false);
                            SetSelection(nextFeature, outLayer, 0); //-1 for ILineString
                            //MapControl.Refresh();
                        }
                    }
                }
            }
            UpdateMapControlSelection(true);
        }

        /// TODO: note if no features are selected the selection rectangle maintains visible after mouse up
        /// ISSUE 2373
        private void HandleMultiSelectMouseUp(ICoordinate worldPosition)
        {
            StopMultiSelect();
            List<IFeature> selectedFeatures = null;
            if (!KeyExtendSelection)
            {
                selectedFeatures = new List<IFeature>(FeatureEditors.Select(fe => fe.SourceFeature).ToArray());
                Clear(false);
            }
            IPolygon selectionPolygon = CreateSelectionPolygon(worldPosition);
            if (null != selectionPolygon)
            {
                foreach (ILayer layer in Map.GetAllLayers(false))
                {
                    //make sure parent layer is selectable or null
                    var parentLayer = Map.GetGroupLayerContainingLayer(layer);
                    if ( (parentLayer == null || parentLayer.IsSelectable) && (layer.IsSelectable) && (layer is VectorLayer))
                    {
                        // do not use the maptool provider but the datasource of each layer.
                        var vectorLayer = (VectorLayer)layer;
                        IList multiFeatures = vectorLayer.DataSource.GetFeatures(selectionPolygon);
                        for (int i = 0; i < multiFeatures.Count; i++)
                        {
                            var feature = (IFeature)multiFeatures[i];
                            if ((null != selectedFeatures) && (selectedFeatures.Contains(feature)))
                            {
                                continue;
                            }
                            AddSelection(vectorLayer, feature, -1, false);
                        }
                    }
                }
            }
            else
            {
                // if mouse hasn't moved handle as single select. A normal multi select uses the envelope
                // of the geometry and this has as result that unwanted features will be selected.
                ILayer selectedLayer;
                float limit = (float)MapHelper.ImageToWorld(Map, 4);
                IFeature nearest = FindNearestFeature(worldPosition, limit, out selectedLayer, ol => ol.Visible);
                if (null != nearest) //&& (selectedLayer.IsVisible))
                    AddSelection(selectedLayer, nearest, -1, false);
            }

            selectPoints.Clear();
            // synchronize with map selection, possible check if selection is already set; do not remove
            UpdateMapControlSelection(true);
        }

        readonly VectorLayer trackingLayer = new VectorLayer(String.Empty);

        public override IMapControl MapControl
        {
            get { return base.MapControl; }
            set
            {
                base.MapControl = value;
                trackingLayer.Map = MapControl.Map;
                coordinateConverter = new CoordinateConverter(MapControl);
            }
        }

        /// <summary>
        /// Selects the given features on the map. Will search all layers for the features.
        /// </summary>
        /// <param name="featuresToSelect">The feature to select on the map.</param>
        public bool Select(IEnumerable<IFeature> featuresToSelect)
        {
            if (featuresToSelect == null)
            {
                Clear(true);
                return false;
            }

            Clear(false);
            foreach (var feature in featuresToSelect)
            {
                var foundLayer = Map.GetLayerByFeature(feature);
                if (foundLayer != null && foundLayer is VectorLayer)
                {
                    AddSelection(foundLayer, feature, -1, feature == featuresToSelect.Last());
                }
            }
            return true;
        }

        /// <summary>
        /// Selects the given feature on the map. Will search all layers for the feature.
        /// </summary>
        /// <param name="featureToSelect">The feature to select on the map.</param>
        public bool Select(IFeature featureToSelect)
        {
            if (null == featureToSelect)
            {
                Clear(true);
                return false;
            }
            // Find the layer that this feature is on
            ILayer foundLayer = MapControl.Map.GetLayerByFeature(featureToSelect);
            if (foundLayer != null && foundLayer is VectorLayer)
            {
                // Select the feature
                Select(foundLayer, featureToSelect, -1);
                return true;
            }

            return false;
        }

        public void Select(ILayer vectorLayer, IFeature feature, int trackerIndex)
        {
            if (IsBusy)
            {
                return;
            }

            Clear(false);
            SetSelection(feature, vectorLayer, trackerIndex);
            UpdateMapControlSelection(true);
        }

        /// <summary>
        /// Handles changes to the map (or bubbled up from ITheme, ILayer) properties. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public override void OnMapPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is ILayer)
            {
                if (e.PropertyName == "Enabled")
                {
                    // If a layer is enabled of disables and features of the layer are selected 
                    // the selection is cleared. Another solution is to remove only features of layer 
                    // from the selection, but this simple and effective.
                    ILayer layer = (ILayer)sender;
                    if (layer is GroupLayer)
                    {
                        GroupLayer layerGroup = (GroupLayer)layer;
                        foreach (ILayer layerGroupLayer in layerGroup.Layers)
                        {
                            HandleLayerStatusChanged(layerGroupLayer);
                        }
                    }
                    else
                    {
                        HandleLayerStatusChanged(layer);
                    }
                }
            }
        }
        private void HandleLayerStatusChanged(ILayer layer)
        {
            foreach (ITrackerFeature trackerFeature in trackers)
            {
                if (layer != trackerFeature.FeatureEditor.Layer)
                    continue;
                Clear();
                return;
            }
        }
        public override void OnMapCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangeAction.Remove:
                    {
                        if (sender is Map)
                        {
                            ILayer layer = (ILayer)e.Item;
                            if (layer is GroupLayer)
                            {
                                GroupLayer layerGroup = (GroupLayer)layer;
                                foreach (ILayer layerGroupLayer in layerGroup.Layers)
                                {
                                    HandleLayerStatusChanged(layerGroupLayer);
                                }
                            }
                            else
                            {
                                HandleLayerStatusChanged(layer);
                            }
                        }
                        break;
                    }
                case NotifyCollectionChangeAction.Replace:
                    throw new NotImplementedException();
            }
        }
        /// <summary>
        /// todo add cancel method to IMapTool 
        /// todo mousedown clears selection -> complex selection -> start multi select -> cancel -> original selection lost
        /// </summary>
        public override void Cancel()
        {
            if (IsBusy)
            {
                if (IsMultiSelect)
                {
                    StopMultiSelect();
                }
                IsBusy = false;
            }
            Clear(true);
        }

        public event EventHandler SelectionChanged;

        public override bool IsActive
        {
            get
            {
                return base.IsActive;
            }
            set
            {
                base.IsActive = value;
                if (false == IsActive)
                {
                    MultiSelectionMode = MultiSelectionMode.Rectangle;
                }
            }
        }
    }
}