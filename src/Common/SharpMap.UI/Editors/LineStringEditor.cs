﻿using System;
using System.Collections.Generic;
using System.Drawing;
using DelftTools.Utils;
using SharpMap.Layers;
using SharpMap.Styles;
using GeoAPI.Extensions.Feature;
using SharpMap.Topology;
using SharpMap.UI.FallOff;
using SharpMap.UI.Mapping;
using GeoAPI.Geometries;
using SharpMap.Converters.Geometries;
using System.Windows.Forms;
using System.Reflection;
using SharpMap.UI.Snapping;

namespace SharpMap.UI.Editors
{
    public class LineStringEditor : FeatureEditor
    {
        public ITrackerFeature AllTracker { get; private set; }
        static private Bitmap trackerSmallStart;
        static private Bitmap trackerSmallEnd;
        static private Bitmap trackerSmall;
        static private Bitmap selectedTrackerSmall;

        public LineStringEditor(ICoordinateConverter coordinateConverter, ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
            : base(coordinateConverter, layer, feature, vectorStyle, editableObject)
        {
            Init();
        }

        private void Init()
        {
            CreateTrackers();
        }
        /// <summary>
        /// NS 2013-03-05 19:51, override fungsi ini agar point dapat bergerak,
        /// untuk lebih jelasnya lihat  fungsi FetureEditor.AllowMoveCore()
        /// </summary>
        /// <returns>true</returns>
        protected override bool AllowMoveCore()
        {
            return true;
        }

        /// <summary>
        /// NS 2013-03-05 19:51, seperti halnya fungsi diatas,
        /// untuk lebih jelasnya lihat di FetureEditor.AllowSingleClickAndMove()
        /// </summary>
        /// <returns>true</returns>
        public override bool  AllowSingleClickAndMove()
        {
            return true;
        }
        
        protected override void CreateTrackers()
        {
            if (trackerSmallStart == null)
            {
                trackerSmallStart = TrackerSymbolHelper.GenerateSimple(new Pen(Color.Blue), new SolidBrush(Color.DarkBlue), 6, 6);
                trackerSmallEnd = TrackerSymbolHelper.GenerateSimple(new Pen(Color.Tomato), new SolidBrush(Color.Maroon), 6, 6);
                //NS, 2013-10-11, Warna track symbol dari Green ke Red
                trackerSmall = TrackerSymbolHelper.GenerateSimple(new Pen(Color.Red), new SolidBrush(Color.OrangeRed), 6, 6);//TrackerSymbolHelper.GenerateSimple(new Pen(Color.Green), new SolidBrush(Color.Lime), 6, 6);
                selectedTrackerSmall = TrackerSymbolHelper.GenerateSimple(new Pen(Color.DarkMagenta), new SolidBrush(Color.Magenta), 6, 6);
            }
            trackers.Clear();
            // optimization: SourceFeature.Geometry.Coordinates is an expensive operation
            if(SourceFeature.Geometry == null)
            {
                return;
            }

            ICoordinate[] coordinates = SourceFeature.Geometry.Coordinates;
            //NS, 2014-05-06
            //Untuk point akhir dari polygon tidak dibuatkan tracker
            int coordCount = 0;
            if (SourceFeature.Geometry.GeometryType == "Polygon")
                coordCount = coordinates.Length - 1;
            else
                coordCount = coordinates.Length;

            //for (int i = 0; i < coordinates.Length; i++)
            for (int i = 0; i < coordCount; i++) 
            {
                ICoordinate coordinate = coordinates[i];
                IPoint selectPoint = GeometryFactory.CreatePoint(coordinate.X, coordinate.Y);
                if (0 == i)
                    trackers.Add(new TrackerFeature(this, selectPoint, i, trackerSmallStart));
                else if ((coordinates.Length - 1) == i)
                    trackers.Add(new TrackerFeature(this, selectPoint, i, trackerSmallEnd));
                else
                    trackers.Add(new TrackerFeature(this, selectPoint, i, trackerSmall));
            }
            AllTracker = new TrackerFeature(this, null, -1, null);
        }
        public override ITrackerFeature GetTrackerAtCoordinate(ICoordinate worldPos)
        {
            ITrackerFeature trackerFeature = base.GetTrackerAtCoordinate(worldPos);
            if (null == trackerFeature)
            {
                ICoordinate org = CoordinateConverter.ImageToWorld(new PointF(0, 0));
                ICoordinate range = CoordinateConverter.ImageToWorld(new PointF(6, 6)); // todo make attribute
                if (SourceFeature.Geometry.Distance(GeometryFactory.CreatePoint(worldPos)) < Math.Abs(range.X - org.X))
                    return AllTracker;
            }
            return trackerFeature;
        }
        public override bool MoveTracker(ITrackerFeature trackerFeature, double deltaX, double deltaY, ISnapResult snapResult)
        {
            if (trackerFeature == AllTracker)
            {
                int index = -1;
                IList<int> handles = new List<int>();
                IList<IGeometry> geometries = new List<IGeometry>();

                for (int i = 0; i < trackers.Count; i++)
                {
                    geometries.Add(trackers[i].Geometry);
                    //if (trackers[i].Selected)
                    {
                        handles.Add(i);
                    }
                    if (trackers[i] == trackerFeature)
                    {
                        index = i;
                    }
                }
                if (0 == handles.Count)
                    return false;
                if (null == FallOffPolicy)
                {
                    FallOffPolicy = new NoFallOffPolicy();
                }
                FallOffPolicy.Move(TargetFeature.Geometry, geometries, handles, index, deltaX, deltaY);
                foreach (IFeatureRelationEditor topologyRule in TopologyRules)
                {
                    topologyRule.UpdateRelatedFeatures(SourceFeature, TargetFeature.Geometry, new List<int> { 0 });
                }

                return true;
            }
            return base.MoveTracker(trackerFeature, deltaX, deltaY, snapResult);
        }

        public override Cursor GetCursor(ITrackerFeature trackerFeature)
        {
            // ReSharper disable AssignNullToNotNullAttribute
            if (trackerFeature == AllTracker)
                return Cursors.SizeAll;
            return new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("SharpMap.UI.Cursors.moveTracker.cur"));
            // ReSharper restore AssignNullToNotNullAttribute
        }

        public override void Select(ITrackerFeature trackerFeature, bool select)
        {
            trackerFeature.Selected = select;
            trackerFeature.Bitmap = select ? selectedTrackerSmall : trackerSmall;
        }
    }
}