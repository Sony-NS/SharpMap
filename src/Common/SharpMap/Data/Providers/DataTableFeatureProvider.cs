// Copyright 2006 - Morten Nielsen (www.iter.dk)
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
//
// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Text;
using DelftTools.Utils.Collections.Generic;
using DelftTools.Utils.Data;
using GeoAPI.Extensions.Feature;
using GisSharpBlog.NetTopologySuite.Geometries;
using log4net;
using SharpMap.Data;
using GeoAPI.Geometries;

namespace SharpMap.Data.Providers
{
	/// <summary>
	/// Datasource for storing a limited set of geometries.
	/// </summary>
	/// <remarks>
	/// <para>The DataTableFeatureProvider doesn�t utilize performance optimizations of spatial indexing,
	/// and thus is primarily meant for rendering a limited set of Geometries.</para>
	/// <para>A common use of the DataTableFeatureProvider is for highlighting a set of selected features.</para>
	/// <example>
	/// The following example gets data within a BoundingBox of another datasource and adds it to the map.
	/// <code lang="C#">
	/// List&#60;Geometry&#62; geometries = myMap.Layers[0].DataSource.GetGeometriesInView(myBox);
	/// VectorLayer laySelected = new VectorLayer("Selected Features");
	/// laySelected.DataSource = new DataTableFeatureProvider(geometries);
	/// laySelected.Style.Outline = new Pen(Color.Magenta, 3f);
	/// laySelected.Style.EnableOutline = true;
	/// myMap.Layers.Add(laySelected);
	/// </code>
	/// </example>
	/// <example>
	/// Adding points of interest to the map. This is useful for vehicle tracking etc.
	/// <code lang="C#">
	/// List&#60;SharpMap.Geometries.Geometry&#62; geometries = new List&#60;SharpMap.Geometries.Geometry&#62;();
	/// //Add two points
	/// geometries.Add(new SharpMap.Geometries.Point(23.345,64.325));
	/// geometries.Add(new SharpMap.Geometries.Point(23.879,64.194));
	/// SharpMap.Layers.VectorLayer layerVehicles = new SharpMap.Layers.VectorLayer("Vechicles");
	/// layerVehicles.DataSource = new SharpMap.Data.Providers.DataTableFeatureProvider(geometries);
	/// layerVehicles.Style.Symbol = Bitmap.FromFile(@"C:\data\car.gif");
	/// myMap.Layers.Add(layerVehicles);
	/// </code>
	/// </example>
	/// </remarks>
    public class DataTableFeatureProvider : Unique<long>, IFeatureProvider
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(DataTableFeatureProvider));

	    public virtual Type FeatureType
	    {
            get { return typeof(FeatureDataRow); }
	    }

	    public virtual IList Features
	    {
	        get { return attributesTable; }
	        set { throw new System.NotImplementedException(); }
	    }

	    private IList<IGeometry> geometries;

        /// <summary>
        /// Gets or sets the geometries this datasource contains
        /// </summary>
        public virtual IList<IGeometry> Geometries
        {
            get { return geometries; }
            set { geometries = value; }
        }

        private FeatureDataTable attributesTable;

        public virtual FeatureDataTable AttributesTable
        {
            get { return attributesTable; }
            set
            {
                UnSubscribeAttributeTableEvents();
                attributesTable = value;
                SubscribeAttributeTableEvents();
            }
        }


        #region constructors

        public DataTableFeatureProvider()
        {
            geometries = new List<IGeometry>();
            attributesTable = new FeatureDataTable();
            SubscribeAttributeTableEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableFeatureProvider"/>
        /// </summary>
        /// <param name="geometries">Set of geometries that this datasource should contain</param>
        public DataTableFeatureProvider(IList<IGeometry> geometries)
        {
            this.geometries = geometries;
            attributesTable = new FeatureDataTable();
            InitAttributeRows(geometries);
            SubscribeAttributeTableEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableFeatureProvider"/>
        /// </summary>
        /// <param name="feature">Feature to be in this datasource</param>
        public DataTableFeatureProvider(FeatureDataRow feature)
        {
            geometries = new Collection<IGeometry>();
            attributesTable = new FeatureDataTable();
            geometries.Add(feature.Geometry);
            attributesTable.AddRow(feature);
            SubscribeAttributeTableEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableFeatureProvider"/>
        /// </summary>
        /// <param name="features">Features to be included in this datasource</param>
        public DataTableFeatureProvider(FeatureDataTable features)
        {
            geometries = new List<IGeometry>();

            for (int i = 0; i < features.Count; i++)
                geometries.Add(((IFeature)features[i]).Geometry);

            attributesTable = features;

            SubscribeAttributeTableEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableFeatureProvider"/>
        /// </summary>
        /// <param name="geometry">Geometry to be in this datasource</param>
        public DataTableFeatureProvider(IGeometry geometry)
        {
            geometries = new List<IGeometry>();
            geometries.Add(geometry);
            attributesTable = new FeatureDataTable();
            InitAttributeRows(geometries);
            SubscribeAttributeTableEvents();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableFeatureProvider"/>
        /// </summary>
        /// <param name="wellKnownBinaryGeometry"><see cref="SharpMap.Geometries.Geometry"/> as Well-known Binary to be included in this datasource</param>
        public DataTableFeatureProvider(byte[] wellKnownBinaryGeometry)
            : this(SharpMap.Converters.WellKnownBinary.GeometryFromWKB.Parse(wellKnownBinaryGeometry))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataTableFeatureProvider"/>
        /// </summary>
        /// <param name="wellKnownTextGeometry"><see cref="SharpMap.Geometries.Geometry"/> as Well-known Text to be included in this datasource</param>
        public DataTableFeatureProvider(string wellKnownTextGeometry)
            : this(SharpMap.Converters.WellKnownText.GeometryFromWKT.Parse(wellKnownTextGeometry))
        {
        }

        #endregion

        private void SubscribeAttributeTableEvents()
        {
            attributesTable.RowDeleted += attributesTable_RowDeleted;
            attributesTable.RowChanged += attributesTable_RowChanged;
        }

        private void UnSubscribeAttributeTableEvents()
        {
            if (attributesTable != null)
            {
                attributesTable.RowDeleted -= attributesTable_RowDeleted;
                attributesTable.RowChanged -= attributesTable_RowChanged;
            }
        }

        void attributesTable_RowChanged(object sender, System.Data.DataRowChangeEventArgs e)
        {
            if (e.Action == DataRowAction.Add)
            {
                Geometries.Add(((FeatureDataRow)e.Row).Geometry);
            }
        }

        void attributesTable_RowDeleted(object sender, System.Data.DataRowChangeEventArgs e)
        {
            Geometries.Remove(((FeatureDataRow)e.Row).Geometry);
        }

        private void InitAttributeRows(IList<IGeometry> geometries)
        {
            AttributesTable.Clear();
            foreach (IGeometry g in geometries)
            {
                FeatureDataRow r = AttributesTable.NewRow();
                r.Geometry = g;
                AttributesTable.AddRow(r);
            }
        }

        #region IFeatureProvider Members

	    public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox)
        {
            return GetGeometriesInView(bbox, -1);
        }

        /// <summary>
        /// Returns features within the specified bounding box
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double minGeometrySize)
        {
            Collection<IGeometry> list = new Collection<IGeometry>();
            for (int i = 0; i < Geometries.Count; i++)
                if (!Geometries[i].IsEmpty)
                    if (Geometries[i].EnvelopeInternal.Intersects(bbox))
                        list.Add(Geometries[i]);
            return list;
        }

        /// <summary>
        /// Returns features within the specified bounding box
        /// </summary>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public virtual ICollection<IFeature> GetFeaturesInView(IEnvelope bbox)
        {
            ICollection<IGeometry> visibleGeometries = GetGeometriesInView(bbox, -1);
            if (visibleGeometries.Count > 0)
            {
                Collection<IFeature> features = new Collection<IFeature>();
                foreach (IGeometry geometry in visibleGeometries)
                {
                    WrapperFeature wrapperFeature = new WrapperFeature(geometry);
                    features.Add(wrapperFeature);
                    //FeatureDataRow row = attributesTable.NewRow();
                    //row.Geometry = geometry;
                }
                return features;
            }
            return null;
        }

        /// <summary>
        /// Returns all objects whose boundingbox intersects 'envelope'.
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public virtual ICollection<int> GetObjectIDsInView(IEnvelope envelope)
        {
            Collection<int> list = new Collection<int>();
            for (int i = 0; i < Geometries.Count; i++)
                if (Geometries[i].EnvelopeInternal.Intersects(envelope))
                    list.Add((int)i);

            return list;
        }

        /// <summary>
        /// Returns the geometry corresponding to the Object ID
        /// </summary>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        public virtual IGeometry GetGeometryByID(int oid)
        {
            return Geometries[(int)oid];                
        }

        public virtual IList GetFeatures(IGeometry geom)
	    {
            IList result = new ArrayList();
            foreach (IFeature feature in Features)
            {
                if(feature.Geometry.Intersects(geom))
                {
                    result.Add(feature);
                }
            }
            return result;
	    }

        public virtual IList GetFeatures(IEnvelope box)
	    {
            IList result = new ArrayList();
            foreach (IFeature feature in Features)
            {
                if (feature.Geometry.EnvelopeInternal.Intersects(box))
                {
                    result.Add(feature);
                }
            }
            return result;
        }

	    /// <summary>
        /// Throws an NotSupportedException. Attribute data is not supported by this datasource
        /// </summary>
        /// <param name="geom"></param>
        /// <param name="ds">FeatureDataSet to fill data into</param>
        public virtual void ExecuteIntersectionQuery(IGeometry geom, FeatureDataSet ds)
        {
            FeatureDataTable dt = AttributesTable.Clone();
            foreach (FeatureDataRow row in AttributesTable)
            {
                dt.Rows.Add(row.ItemArray);
                ((FeatureDataRow)dt.Rows[dt.Count - 1]).Geometry = row.Geometry;
            }
            ds.Tables.Add(dt);
        }

        /// <summary>
        /// Throws an NotSupportedException. Attribute data is not supported by this datasource
        /// </summary>
        /// <param name="box"></param>
        /// <param name="ds">FeatureDataSet to fill data into</param>
        public virtual void ExecuteIntersectionQuery(IEnvelope box, FeatureDataSet ds)
        {
            FeatureDataTable dt = AttributesTable.Clone();
            foreach (FeatureDataRow row in AttributesTable)
            {
                if (!row.Geometry.IsEmpty)
                {
                    if (row.Geometry.EnvelopeInternal.Intersects(box))
                    {
                        dt.Rows.Add(row.ItemArray);
                        ((FeatureDataRow)dt.Rows[dt.Count - 1]).Geometry = row.Geometry;
                    }
                }
            }
            ds.Tables.Add(dt);
        }

        /// <summary>
        /// Returns the number of features in the dataset
        /// </summary>
        /// <returns>number of features</returns>
        public virtual int GetFeatureCount()
        {
            return Geometries.Count;
        }

        /// <summary>
        /// Throws an NotSupportedException. Attribute data is not supported by this datasource
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual IFeature GetFeature(int index)
        {
            if (index == -1)
            {
                return null;
            }
            return (IFeature) AttributesTable[(int)index];
        }

	    public virtual bool Contains(IFeature feature)
	    {
            //dont use this..contains check the key of rows
            //return AttributesTable.Rows.Contains(feature);
	        return IndexOf(feature) != -1;
	    }

	    public virtual int IndexOf(IFeature feature)
	    {
            if (feature is FeatureDataRow)
            {
                return AttributesTable.Rows.IndexOf((FeatureDataRow)feature);
            }
	        return -1;
	    }

	    /// <summary>
        /// Boundingbox of dataset
        /// </summary>
        /// <returns>boundingbox</returns>
        public virtual IEnvelope GetExtents()
        {
            IEnvelope envelope = new Envelope();

            for (int i = 0; i < Geometries.Count; i++)
            {
                if (!Geometries[i].IsEmpty)
                {
                    envelope.ExpandToInclude(Geometries[i].EnvelopeInternal);
                }
            }
	        
            return envelope;
        }

        #endregion

        private int _SRID = -1;
        /// <summary>
        /// The spatial reference ID (CRS)
        /// </summary>
        public virtual int SRID
        {
            get { return _SRID; }
            set { _SRID = value; }
        }

        // additions

        public virtual FeatureDataTable Attributes
        {
            get { return attributesTable; }
        }

	    public virtual void AddFeature(IFeature feature)
	    {
	        Add(feature.Geometry);
	    }

	    public virtual void Clear()
        {
            geometries.Clear();
            attributesTable.Clear();
        }

        public virtual IFeature Add(IGeometry geometry)
        {
            FeatureDataRow featureDataRow = attributesTable.NewRow();
            featureDataRow.Geometry = geometry;
            attributesTable.AddRow(featureDataRow);
            return featureDataRow;
        }

        public virtual Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

	    public virtual FeatureDataRow UpdateGeometry(int index, IGeometry newGeometry)
        {
            geometries[index] = newGeometry;
            FeatureDataRow featureDataRow = (FeatureDataRow) attributesTable[index];
            featureDataRow.Geometry = newGeometry;
            return featureDataRow;
        }

        public virtual void Remove(int index)
        {
            //geometries.RemoveAt(index);
            FeatureDataRow featureDataRow = (FeatureDataRow) attributesTable[index];
            attributesTable.RemoveRow(featureDataRow);
        }

        public virtual FeatureDataRow FeatureByIndex(int index)
        {
            return (FeatureDataRow) attributesTable[index];
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes the object
        /// </summary>
        public virtual void Dispose()
        {
            UnSubscribeAttributeTableEvents();
            geometries = null;
            attributesTable = null;

        }

        #endregion
    }
    public class WrapperFeature : Unique<long>, IFeature
    {
        private IGeometry geometry;

        public WrapperFeature(IGeometry geometry)
        {
            Geometry = geometry;
        }

        #region IFeature Members

        public IGeometry Geometry
        {
            get { return geometry; }
            set { geometry = value; }
        }
        
        public IFeatureAttributeCollection Attributes
        {
            get
            {
                throw new Exception("The method or operation is not implemented.");
            }
            set
            {
                throw new Exception("The method or operation is not implemented.");
            }
        }

        #endregion

        public object Clone()
        {
            throw new NotImplementedException();
        }
    }

}
