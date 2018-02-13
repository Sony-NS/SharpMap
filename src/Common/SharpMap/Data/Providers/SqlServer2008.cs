// Copyright 2008 - William Dollins   
// SQL Server 2008 by William Dollins (dollins.bill@gmail.com)   
// Based on Oracle provider by Humberto Ferreira (humbertojdf@gmail.com)   
//   
// Date 2007-11-28   
//
// Adapted to DeltaShell-Spatial from Diego Guidi, 2011-04-19
//
// This file is part of    
// is free software; you can redistribute it and/or modify   
// it under the terms of the GNU Lesser General Public License as published by   
// the Free Software Foundation; either version 2 of the License, or   
// (at your option) any later version.   
//   
// is distributed in the hope that it will be useful,   
// but WITHOUT ANY WARRANTY; without even the implied warranty of   
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the   
// GNU Lesser General Public License for more details.   

// You should have received a copy of the GNU Lesser General Public License   
// along with  if not, write to the Free Software   
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA    

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using SharpMap.Converters.WellKnownBinary;
using SharpMap.Converters.WellKnownText;
using DelftTools.Utils.IO;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;

//NS 2013-05-15, SNC Bandung
using DelftTools.Utils.Data;
using DelftTools.Utils.IO;
using System.IO;

namespace SharpMap.Data.Providers
{
    /// <summary>
    /// Possible spatial object types on SqlServer
    /// </summary>
    public enum SqlServerSpatialObjectType
    {
        /// <summary>
        /// Geometry
        /// </summary>
        Geometry,
        /// <summary>
        /// Geography
        /// </summary>
        Geography,
    }

    /// <summary>   
    /// SQL Server 2008 data provider   
    /// </summary>   
    /// <remarks>   
    /// <para>This provider was developed against the SQL Server 2008 November CTP. The platform may change significantly before release.</para>   
    /// <example>   
    /// Adding a datasource to a layer:   
    /// <code lang="C#">   
    /// Layers.VectorLayer myLayer = new Layers.VectorLayer("My layer");   
    /// string ConnStr = "Provider=SQLOLEDB.1;Integrated Security=SSPI;Persist Security Info=False;Initial Catalog=myDB;Data Source=myServer\myInstance";   
    /// myLayer.DataSource = new Data.Providers.Katmai(ConnStr, "myTable", "GeomColumn", "OidColumn");   
    /// </code>   
    /// </example>   
    /// <para>SQL Server 2008 provider by Bill Dollins (dollins.bill@gmail.com). Based on the Oracle provider written by Humberto Ferreira.</para>   
    /// </remarks>   
    [Serializable]
    //public class SqlServer2008 : IFeatureProvider, IFileBased //NS 2013-05-15
    public class SqlServer2008 : Unique<long>, IFeatureProvider, IFileBased
    {
        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="geometryColumnName">Name of geometry column</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        public SqlServer2008(string connectionStr, string tablename, string geometryColumnName, string oidColumnName, string geometryLabelName, int srid) :
            this(connectionStr, tablename, geometryColumnName, oidColumnName, geometryLabelName, SqlServerSpatialObjectType.Geometry, srid) { }

        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="geometryColumnName">Name of geometry column</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        /// <param name="spatialObjectType">The type of the spatial object to use for spatial queries</param>
        public SqlServer2008(string connectionStr, string tablename, string geometryColumnName, string oidColumnName, 
            string geometryLabelName, SqlServerSpatialObjectType spatialObjectType, int srid)
        {
            this.ConnectionString = connectionStr;
            this.Table = tablename;
            this.GeometryColumn = geometryColumnName;
            this.ObjectIdColumn = oidColumnName;
            this.GeometryLabel = geometryColumnName;
            this._spatialObjectType = spatialObjectType;
            switch (spatialObjectType)
            {
                case SqlServerSpatialObjectType.Geometry:
                    this._spatialObject = "geometry";
                    break;

                default:
                    this._spatialObject = "geography";
                    break;
            }

            this.SRID = srid;
        }

        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        public SqlServer2008(string connectionStr, string tablename, string oidColumnName, string geometryLabelName, int srid) :
            this(connectionStr, tablename, "shape", oidColumnName, geometryLabelName, SqlServerSpatialObjectType.Geometry, srid) { }

        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>
        /// <param name="spatialObjectType">The type of the spatial object to use for spatial queries</param>
        public SqlServer2008(string connectionStr, string tablename, string oidColumnName, string geometryLabelName, 
            SqlServerSpatialObjectType spatialObjectType, int srid) :
            this(connectionStr, tablename, "shape", oidColumnName, geometryLabelName, spatialObjectType, srid) { }

        /// <summary>   
        /// Returns true if the datasource is currently open   
        /// </summary>   
        public bool IsOpen { get; private set; }

        public void RelocateTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public void ReConnect()
        {
            throw new NotImplementedException();
        }

        public void Open(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>   
        /// Opens the datasource   
        /// </summary>   
        public void Open()
        {
            //Don't really do anything.   
            this.IsOpen = true;
        }

        public string Path
        {
            get { throw new NotImplementedException(); } 
            set { throw new NotImplementedException(); }
        }

        public void CreateNew(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>   
        /// Closes the datasource   
        /// </summary>   
        public void Close()
        {
            //Don't really do anything.   
            this.IsOpen = false;
        }

        //NS 2013-05-15
        public virtual void Delete()
        {
            System.IO.File.Delete(Path);
        }
        //NS 2013-05-15
        public void SwitchTo(string newPath)
        {
            throw new NotImplementedException();
        }

        private bool _disposed;

        /// <summary>   
        /// Disposes the object   
        /// </summary>   
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (this._disposed)
                return;

            if (disposing)
            {
                //Close();   
            }
            this._disposed = true;
        }

        /// <summary>   
        /// Finalizer   
        /// </summary>   
        ~SqlServer2008()
        {
            this.Dispose();
        }

        private string _connectionString;

        /// <summary>   
        /// Connectionstring   
        /// </summary>   
        public string ConnectionString
        {
            get { return this._connectionString; }
            set { this._connectionString = value; }
        }

        private string _table;

        /// <summary>   
        /// Data table name   
        /// </summary>   
        public string Table
        {
            get { return this._table; }
            set { this._table = value; }
        }

        private string _geometryColumn;

        /// <summary>   
        /// Name of geometry column   
        /// </summary>   
        public string GeometryColumn
        {
            get { return this._geometryColumn; }
            set { this._geometryColumn = value; }
        }

        private string _objectIdColumn;

        /// <summary>   
        /// Name of column that contains the Object ID   
        /// </summary>   
        public string ObjectIdColumn
        {
            get { return this._objectIdColumn; }
            set { this._objectIdColumn = value; }
        }

        //NS 2013-05-16
        private string _geometryLabel;

        /// <summary>   
        /// Label for geometry column   
        /// </summary>   
        public string GeometryLabel
        {
            get { return this._geometryLabel; }
            set { this._geometryLabel = value; }
        }

        private bool _makeValid;

        /// <summary>
        /// Gets/Sets whether all <see cref="GisSharpBlog.NetTopologySuite.Geometries"/> passed to SqlServer2008 should me made valid using this function.
        /// </summary>
        public Boolean ValidateGeometries 
        { 
            get { return this._makeValid; } 
            set { this._makeValid = value; } 
        }

        private String MakeValidString
        {
            get { return this._makeValid ? ".MakeValid()" : String.Empty; }
        }

        private readonly string _spatialObject;
        private readonly SqlServerSpatialObjectType _spatialObjectType;        

        /// <summary>
        /// Spatial object type for  
        /// </summary>
        public SqlServerSpatialObjectType SpatialObjectType
        {
            get { return this._spatialObjectType; }
        }

        private long _id;

        public long Id
        {
            get { return this._id; }
            set { this._id = value; }
        }

        public Type FeatureType
        {
            get { return typeof(FeatureDataRow); }
        }

        public IList Features
        {
            get
            {
                //problem with reuse 
                return GetFeatures(GetExtents());
            }
            set { throw new NotImplementedException(); }
        }

        public IFeature Add(IGeometry geometry)
        {
            throw new NotImplementedException();
            //FeatureDataRow featureDataRow = attributesTable.NewRow();
            //featureDataRow.Geometry = geometry;
            //attributesTable.AddRow(featureDataRow);
            //return featureDataRow;
        }

        public Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate
        {
            get { throw new NotImplementedException(); } 
            set { throw new NotImplementedException(); }
        }

        public ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double minGeometrySize)
        {
            if (bbox == null)
                throw new ArgumentNullException("bbox");

            Collection<IGeometry> features = new Collection<IGeometry>();
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                string strBbox = this.GetBoxFilterStr(bbox);
                string strSQL = String.Format(
                    "SELECT g.{0}.STAsBinary() FROM {1} g WHERE ",
                    //"SELECT g.{0}.STAsText() FROM {1} g WHERE ", 
                    this.GeometryColumn, this.Table);
                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += this.DefinitionQuery + " AND ";
                strSQL += strBbox;
                Debug.WriteLine(strSQL);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                using (SqlCommand command = new SqlCommand(strSQL, conn))
                {
                    conn.Open();
                    using (SqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (dr[0] != DBNull.Value)
                            {
                                byte[] bytes = (byte[])dr[0];
                                IGeometry geom = GeometryFromWKB.Parse(bytes);
                                //IGeometry geom = SharpMap.Converters.WellKnownText.GeometryFromWKT.Parse(dr[0].ToString());
                                if (geom != null)
                                    features.Add(geom);
                            }
                        }
                    }
                    conn.Close();
                }
                watch.Stop();
                Debug.WriteLine("Elapsed miliseconds: " + watch.ElapsedMilliseconds);
            }
            return features;
        }

        /// <summary>   
        /// Returns geometry Object IDs whose bounding box intersects 'bbox'   
        /// </summary>   
        /// <param name="bbox"></param>   
        /// <returns></returns>   
        public ICollection<int> GetObjectIDsInView(IEnvelope bbox)
        {
            if (bbox == null)
                throw new ArgumentNullException("bbox");

            ICollection<int> objectlist = new Collection<int>();
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                string strBbox = this.GetBoxFilterStr(bbox);
                string strSQL = String.Format("SELECT g.{0} FROM {1} g WHERE ", this.ObjectIdColumn, this.Table);                
                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += String.Format(" {0} AND ", this.DefinitionQuery);
                strSQL += strBbox;
                Debug.WriteLine(strSQL);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                using (SqlCommand command = new SqlCommand(strSQL, conn))
                {
                    conn.Open();
                    using (SqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (dr[0] != DBNull.Value)
                            {
                                //int oid = (int)(decimal)dr[0];
                                int oid = (int)dr[0];
                                objectlist.Add(oid);
                            }
                        }
                    }
                    conn.Close();
                }
                watch.Stop();
                Debug.WriteLine("Elapsed miliseconds: " + watch.ElapsedMilliseconds);
            }
            return objectlist;
        }

        /// <summary>   
        /// Returns id of geometry object whose bounding box intersects 'bbox'   
        /// </summary>   
        /// <param name="bbox"></param>   
        /// <returns></returns>   
        public int GetGeometryID(string wktString)
        {
            if (wktString == null)
                throw new ArgumentNullException("wktString");

            int oid = -1;
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                string strBbox = this.GetEnvelopeCenterFilterStr(wktString);
                string strSQL = String.Format("SELECT TOP(1) g.{0} FROM {1} g WHERE ", 
                    this.ObjectIdColumn, this.Table);

                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += String.Format(" {0} AND ", this.DefinitionQuery);
                strSQL += strBbox;
                
                using (SqlCommand command = new SqlCommand(strSQL, conn))
                {
                    conn.Open();
                    using (SqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (dr[0] != DBNull.Value)
                            {
                                oid = (int)dr[0];
                            }
                        }
                    }
                    conn.Close();
                }
            }
            return oid;
        }

        public IGeometry GetGeometryByID(int oid)
        {
            IGeometry geom = null;
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                string strSQL = String.Format(
                    "SELECT g.{0}.STAsBinary() FROM {1} g WHERE {2}='{3}'",
                    //"SELECT g.{0}.STAsText() FROM {1} g WHERE {2}='{3}'", 
                    this.GeometryColumn, this.Table, this.ObjectIdColumn, oid);
                Debug.WriteLine(strSQL);

                conn.Open();
                using (SqlCommand command = new SqlCommand(strSQL, conn))
                {
                    using (SqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (dr[0] != DBNull.Value)
                                geom = GeometryFromWKB.Parse((byte[])dr[0]);
                                //geom = SharpMap.Converters.WellKnownText.GeometryFromWKT.Parse(dr[0].ToString());
                        }
                    }
                }
                conn.Close();
            }
            return geom;
        }

        /// <summary>
        /// NS 2013-05-16
        /// Get Geometry Label
        /// </summary>
        /// <param name="oid"></param>
        /// <returns></returns>
        public string GetGeometryLabelByID(int oid)
        {
            string geomLabel = string.Empty;
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                string strSQL = String.Format(
                    "SELECT g.{0} FROM {1} g WHERE {2}='{3}'",
                    this.GeometryLabel, this.Table, this.ObjectIdColumn, oid);
                Debug.WriteLine(strSQL);

                conn.Open();
                using (SqlCommand command = new SqlCommand(strSQL, conn))
                {
                    using (SqlDataReader dr = command.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            if (dr[0] != DBNull.Value)
                                geomLabel = dr[0].ToString();
                        }
                    }
                }
                conn.Close();
            }
            return geomLabel;
        }

        public IList GetFeatures(IGeometry boundingGeometry)
        {
            if (boundingGeometry == null)
                throw new ArgumentNullException("boundingGeometry");

            return GetFeatures(boundingGeometry.EnvelopeInternal);
        }

        public IList GetFeatures(IEnvelope box)
        {
            if (box == null)
                throw new ArgumentNullException("box");

            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                string strBbox = this.GetBoxFilterStr(box);
                string strSQL = String.Format(
                    "SELECT g.*, g.{0}{1}.STAsBinary() AS sharpmap_tempgeometry FROM {2} g WHERE ",
                    //"SELECT g.*, g.{0}{1}.STAsText() AS sharpmap_tempgeometry FROM {2} g WHERE ",
                    this.GeometryColumn, this.MakeValidString, this.Table);
                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += this.DefinitionQuery + " AND ";
                strSQL += strBbox;
                Debug.WriteLine(strSQL);

                Stopwatch watch = new Stopwatch();
                watch.Start();
                try
                {
                    using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
                    {
                        conn.Open();
                        System.Data.DataSet dataset = new System.Data.DataSet();
                        adapter.Fill(dataset);
                        conn.Close();
                        if (dataset.Tables.Count > 0)
                        {
                            FeatureDataTable fdt = new FeatureDataTable(dataset.Tables[0]);
                            foreach (DataColumn col in dataset.Tables[0].Columns)
                                if (col.ColumnName != this.GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                    fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                            foreach (DataRow dr in dataset.Tables[0].Rows)
                            {
                                FeatureDataRow fdr = fdt.NewRow();
                                foreach (DataColumn col in dataset.Tables[0].Columns)
                                    if (col.ColumnName != this.GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                        fdr[col.ColumnName] = dr[col];
                                fdr.Geometry = GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
                                //fdr.Geometry = SharpMap.Converters.WellKnownText.GeometryFromWKT.Parse(dr["sharpmap_tempgeometry"].ToString());
                                fdt.AddRow(fdr);
                            }                        
                            return fdt;
                        }
                    }
                    return null;
                }
                finally
                {
                    watch.Stop();
                    Debug.WriteLine("Elapsed miliseconds: " + watch.ElapsedMilliseconds);
                }
            }            
        }

        /// <summary>   
        /// Returns the box filter string needed in SQL query   
        /// </summary>   
        /// <param name="bbox"></param>   
        /// <returns></returns>   
        private string GetBoxFilterStr(IEnvelope bbox)
        {
            ICoordinate ll = new Coordinate(bbox.MinX, bbox.MinY);
            ICoordinate lr = new Coordinate(bbox.MaxX, bbox.MinY);
            ICoordinate ur = new Coordinate(bbox.MaxX, bbox.MaxY);
            ICoordinate ul = new Coordinate(bbox.MinX, bbox.MaxY);
            LinearRing ring = new LinearRing(new[] { ll, lr, ur, ul, ll });
            Polygon p = new Polygon(ring);
            string bboxText = SharpMap.Converters.WellKnownText.GeometryToWKT.Write(p);
            string whereClause = String.Format(
                "{0}{1}.STIntersects({4}::STGeomFromText('{2}', {3})) = 1",
                this.GeometryColumn, this.MakeValidString, bboxText, this.SRID, this._spatialObject);
            return whereClause;
        }

        /// <summary> 
        /// NS 2013-05-27, 
        /// Returns the envelove center filter string needed in SQL query   
        /// </summary>   
        /// <param name="bbox"></param>   
        /// <returns></returns>   
        private string GetEnvelopeCenterFilterStr(string wktString)
        {
            string whereClause = string.Empty;
                
            if (this.SpatialObjectType == SqlServerSpatialObjectType.Geography)
                whereClause = String.Format(
                      "{0}{1}.STEquals({4}::STGeomFromText('{2}', {3})) = 1",
                      this.GeometryColumn, this.MakeValidString, wktString, this.SRID, this._spatialObject);
            else
                whereClause = String.Format(
                      "{0}{1}.STEquals({4}::STGeomFromText('{2}', {3})) = 1",
                      this.GeometryColumn, this.MakeValidString, wktString, this.SRID, this._spatialObject);
            return whereClause;
        }

        /// <summary>   
        /// Returns the number of features in the dataset   
        /// </summary>   
        /// <returns>number of features</returns>   
        public int GetFeatureCount()
        {
            int count;
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                string strSQL = String.Format("SELECT COUNT(*) FROM {0}", this.Table);
                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += String.Format(" WHERE {0}", this.DefinitionQuery);

                using (SqlCommand command = new SqlCommand(strSQL, conn))
                {
                    conn.Open();
                    count = (int)command.ExecuteScalar();
                    conn.Close();
                }
            }
            return count;
        }

        //public IFeature GetFeature(int index)
        //{
        //    return GetFeature(index, null);
        //}

        public IFeature GetFeature(int index)
        {
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                int rowId = index + 1; // index looks zero-based
                string strSQL = String.Format(
                    "select g.* , g.{0}.STAsBinary() As sharpmap_tempgeometry from {1} g WHERE {2}={3}",
                    //"select g.* , g.{0}.STAsText() As sharpmap_tempgeometry from {1} g WHERE {2}={3}",
                    this.GeometryColumn, this.Table, this.ObjectIdColumn, rowId);

                using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
                {
                    DataSet ds = new DataSet();
                    conn.Open();
                    adapter.Fill(ds);
                    conn.Close();
                    if (ds.Tables.Count > 0)
                    {
                        FeatureDataTable fdt = new FeatureDataTable(ds.Tables[0]);
                        foreach (DataColumn col in ds.Tables[0].Columns)
                            if (col.ColumnName != this.GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                        if (ds.Tables[0].Rows.Count > 0)
                        {
                            DataRow dr = ds.Tables[0].Rows[0];
                            FeatureDataRow fdr = fdt.NewRow();
                            foreach (DataColumn col in ds.Tables[0].Columns)
                                if (col.ColumnName != this.GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                    fdr[col.ColumnName] = dr[col];
                            fdr.Geometry = GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
                            //fdr.Geometry = SharpMap.Converters.WellKnownText.GeometryFromWKT.Parse(dr["sharpmap_tempgeometry"].ToString());
                            
                            return fdr;
                        }
                        return null;
                    }
                    return null;
                }
            }
        }

        public bool Contains(IFeature feature)
        {
            throw new NotImplementedException();
        }

        public int IndexOf(IFeature feature)
        {
            throw new NotImplementedException();
        }

        /// <summary>   
        /// Definition query used for limiting dataset   
        /// </summary>   
        public string DefinitionQuery { get; set; }

        /// <summary>   
        /// Gets a collection of columns in the dataset   
        /// </summary>   
        public DataColumnCollection Columns
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>   
        /// Spacial Reference ID   
        /// </summary>   
        public int SRID { get; set; }

        /// <summary>   
        /// Boundingbox of dataset   
        /// </summary>   
        /// <returns>boundingbox</returns>   
        public IEnvelope GetExtents()
        {
            using (SqlConnection conn = new SqlConnection(this._connectionString))
            {
                //NS 2013-05-15
                string strCmd = string.Empty;                
                if (this._spatialObjectType == SqlServerSpatialObjectType.Geometry)
                {
                    strCmd = "SELECT g.{0}{1}.STEnvelope().STAsText() FROM {2} g ";
                }else
                {
                    strCmd = "SELECT g.{0}{1}.STAsText() FROM {2} g ";
                }

                string strSQL = String.Format(
                    //"SELECT g.{0}{1}.STEnvelope().STAsText() FROM {2} g ",
                    //NS 2013-05-15
                    //"SELECT g.{0}{1}.STAsText() FROM {2} g ",
                    strCmd,
                    this.GeometryColumn, this.MakeValidString, this.Table);
                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += " WHERE " + this.DefinitionQuery;

                using (SqlCommand command = new SqlCommand(strSQL, conn))
                {
                    conn.Open();
                    IEnvelope bx = null;
                    SqlDataReader dr = command.ExecuteReader();
                    while (dr.Read())
                    {
                        string wkt = dr.GetString(0);
                        IGeometry g = SharpMap.Converters.WellKnownText.GeometryFromWKT.Parse(wkt);
                        IEnvelope bb = g.EnvelopeInternal;
                        bx = bx == null ? bb : bx.Union(bb);
                    }
                    dr.Close();
                    conn.Close();
                    return bx;
                }
            }
        }

        /// <summary>   
        /// Returns the features that intersects with 'geom'   
        /// </summary>   
        /// <param name="geom"></param>   
        /// <param name="ds">FeatureDataSet to fill data into</param>   
        public void ExecuteIntersectionQuery(Geometry geom, FeatureDataSet ds)
        {
            List<Geometry> features = new List<Geometry>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                string strGeom = string.Empty;
                //TODO: Convert to SQL Server 
                if (this._spatialObjectType == SqlServerSpatialObjectType.Geometry)
                {
                    strGeom = "geography::STGeomFromText('" + geom.AsText() + "', #SRID#)";
                }
                else
                {
                    strGeom = "geometry::STGeomFromText('" + geom.AsText() + "', #SRID#)";
                }

                if (SRID > 0)
                {
                    strGeom = strGeom.Replace("#SRID#", SRID.ToString());
                }
                else
                {
                    strGeom = strGeom.Replace("#SRID#", "0");
                }
                strGeom = GeometryColumn + ".STIntersects(" + strGeom + ") = 1";

                string strSQL = "SELECT g.* , g." + GeometryColumn + ".STAsBinary() As sharpmap_tempgeometry FROM " + Table + " g WHERE ";

                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += this.DefinitionQuery + " AND ";

                strSQL += strGeom;

                using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
                {
                    conn.Open();
                    adapter.Fill(ds);
                    conn.Close();
                    if (ds.Tables.Count > 0)
                    {
                        FeatureDataTable fdt = new FeatureDataTable(ds.Tables[0]);
                        foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
                            if (col.ColumnName != GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                        foreach (System.Data.DataRow dr in ds.Tables[0].Rows)
                        {
                            Data.FeatureDataRow fdr = fdt.NewRow();
                            foreach (System.Data.DataColumn col in ds.Tables[0].Columns)
                                if (col.ColumnName != GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                    fdr[col.ColumnName] = dr[col];
                            fdr.Geometry = Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
                            fdt.AddRow(fdr);
                        }
                        ds.Tables.Add(fdt);
                    }
                }
            }
        }

        /// <summary>   
        /// Returns all features with the view box   
        /// </summary>   
        /// <param name="bbox">view box</param>   
        /// <param name="ds">FeatureDataSet to fill data into</param>   
        public void ExecuteIntersectionQuery(IEnvelope bbox, FeatureDataSet ds)
        {
            List<Geometry> features = new List<Geometry>();
            using (SqlConnection conn = new SqlConnection(_connectionString))
            {
                //Get bounding box string   
                string strBbox = GetBoxFilterStr(bbox);

                //string strSQL = "SELECT g.*, g." + GeometryColumn + ".STAsBinary() AS sharpmap_tempgeometry ";   
                string strSQL = String.Format(
                    "SELECT g.*, g.{0}{1}.STAsBinary() AS sharpmap_tempgeometry FROM {2} g WHERE ",
                    GeometryColumn, MakeValidString, Table);

                if (!String.IsNullOrEmpty(this.DefinitionQuery))
                    strSQL += DefinitionQuery + " AND ";

                strSQL += strBbox;

                using (SqlDataAdapter adapter = new SqlDataAdapter(strSQL, conn))
                {
                    conn.Open();
                    System.Data.DataSet ds2 = new System.Data.DataSet();
                    adapter.Fill(ds2);
                    conn.Close();
                    if (ds2.Tables.Count > 0)
                    {
                        FeatureDataTable fdt = new FeatureDataTable(ds2.Tables[0]);
                        foreach (System.Data.DataColumn col in ds2.Tables[0].Columns)
                            if (col.ColumnName != GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                fdt.Columns.Add(col.ColumnName, col.DataType, col.Expression);
                        foreach (System.Data.DataRow dr in ds2.Tables[0].Rows)
                        {
                            Data.FeatureDataRow fdr = fdt.NewRow();
                            foreach (System.Data.DataColumn col in ds2.Tables[0].Columns)
                                if (col.ColumnName != GeometryColumn && col.ColumnName != "sharpmap_tempgeometry")
                                    fdr[col.ColumnName] = dr[col];
                            fdr.Geometry = Converters.WellKnownBinary.GeometryFromWKB.Parse((byte[])dr["sharpmap_tempgeometry"]);
                            fdt.AddRow(fdr);
                        }
                        ds.Tables.Add(fdt);
                    }
                }
            }
        }  

        /// <summary>   
        /// Gets the connection ID of the datasource   
        /// </summary>   
        public string ConnectionID
        {
            get { return this._connectionString; }
        }
    }
}