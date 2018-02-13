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

namespace SharpMap.Data.Providers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Data;
    using System.Data.SqlClient;
    using System.Diagnostics;
    using Converters.WellKnownBinary;
    using Converters.WellKnownText;
    using DelftTools.Utils.IO;
    using GeoAPI.Extensions.Feature;
    using GeoAPI.Geometries;
    using GisSharpBlog.NetTopologySuite.Geometries;

    //NS 2013-04-03, SNC Bandung
    using DelftTools.Utils.Data;
    using DelftTools.Utils.IO;
    using System.IO;

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

    ///// <summary>   
    ///// Method used to determine extents of all features
    ///// </summary>
    //public enum SqlServer2008ExtentsMode
    //{
    //    /// <summary>
    //    /// Reads through all features in the table to determine extents
    //    /// </summary>
    //    QueryIndividualFeatures,
    //    /// <summary>
    //    /// Directly reads the bounds of the spatial index from the system tables (very fast, but does not take <see cref="SqlServer2008.DefinitionQuery"/> into account)
    //    /// </summary>
    //    SpatialIndex,
    //    /// <summary>
    //    /// Uses the EnvelopeAggregate aggregate function introduced in SQL Server 2012
    //    /// </summary>
    //    EnvelopeAggregate
    //}

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
    //public class SqlServer2008 : IFeatureProvider, IFileBased //NS 2013-04-03
    public class SqlServer2008 : Unique<long>, IFeatureProvider, IFileBased
    {
        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="geometryColumnName">Name of geometry column</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        //public SqlServer2008(string connectionStr, string tablename, string geometryColumnName, string oidColumnName) :
        //    this(connectionStr, tablename, geometryColumnName, oidColumnName, SqlServerSpatialObjectType.Geometry) { }
       
        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="geometryColumnName">Name of geometry column</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        /// <param name="spatialObjectType">The type of the spatial object to use for spatial queries</param>
        /// <param name="useSpatialIndexExtentAsExtent">If true, the bounds of the spatial index is used for the GetExtents() method which heavily increases performance instead of reading through all features in the table</param>
        /// <param name="SRID">The spatial reference id</param>
        public SqlServer2008(string connectionStr, string tablename, string geometryColumnName, string oidColumnName,
            SqlServerSpatialObjectType spatialObjectType, 
            //bool useSpatialIndexExtentAsExtent, 
            int SRID)
        {
            this.ConnectionString = connectionStr;
            this.Table = tablename;
            this.GeometryColumn = geometryColumnName;
            this.ObjectIdColumn = oidColumnName;
            this.spatialObjectType = spatialObjectType;
            switch (spatialObjectType)
            {
                case SqlServerSpatialObjectType.Geometry:
                    this.spatialObject = "geometry";
                    break;

                default:
                    this.spatialObject = "geography";
                    break;
            }

            //_extentsMode = (useSpatialIndexExtentAsExtent ? SqlServer2008ExtentsMode.SpatialIndex : SqlServer2008ExtentsMode.QueryIndividualFeatures);
            this.SRID = SRID;
        }

        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        public SqlServer2008(string connectionStr, string tablename, string oidColumnName) :
            this(connectionStr, tablename, "shape", oidColumnName, SqlServerSpatialObjectType.Geometry) { }

        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="geometryColumnName">Name of geometry column</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        /// <param name="spatialObjectType">The type of the spatial object to use for spatial queries</param>
        public SqlServer2008(string connectionStr, string tablename, string geometryColumnName, string oidColumnName, 
            SqlServerSpatialObjectType spatialObjectType)
            : this(connectionStr, tablename, geometryColumnName, oidColumnName, spatialObjectType, 0)
        {
        }

        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="geometryColumnName">Name of geometry column</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>   
        /// <param name="spatialObjectType">The type of the spatial object to use for spatial queries</param>
        /// <param name="useSpatialIndexExtentAsExtent">If true, the bounds of the spatial index is used for the GetExtents() method which heavily increases performance instead of reading through all features in the table</param>
        //public SqlServer2008(string connectionStr, string tablename, string geometryColumnName, string oidColumnName, 
        //    SqlServerSpatialObjectType spatialObjectType, int SRID)
        //    : this(connectionStr, tablename, geometryColumnName, oidColumnName, spatialObjectType, SRID)
        //{
        //}

        /// <summary>   
        /// Initializes a new connection to SQL Server   
        /// </summary>   
        /// <param name="connectionStr">Connectionstring</param>   
        /// <param name="tablename">Name of data table</param>   
        /// <param name="oidColumnName">Name of column with unique identifier</param>
        /// <param name="spatialObjectType">The type of the spatial object to use for spatial queries</param>
        public SqlServer2008(string connectionStr, string tablename, string oidColumnName, SqlServerSpatialObjectType spatialObjectType) :
            this(connectionStr, tablename, "shape", oidColumnName, spatialObjectType) { }

        //NS 2013-04-03
        //private SqlServer2008ExtentsMode _extentsMode = SqlServer2008ExtentsMode.QueryIndividualFeatures;

        /// <summary>
        /// NS 2013-04-03
        /// Gets or sets the method used in the <see cref="GetExtents"/> method.
        /// </summary>
        //public SqlServer2008ExtentsMode ExtentsMode
        //{
        //    get { return _extentsMode; }
        //    set { _extentsMode = value; }
        //}

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

        //NS 2013-04-03
        public virtual void Delete()
        {
            System.IO.File.Delete(Path);
        }
        //NS 2013-04-03
        public void SwitchTo(string newPath)
        {
            throw new NotImplementedException();
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

        private bool disposed;

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
            if (this.disposed)
                return;

            if (disposing)
            {
                //Close();   
            }
            this.disposed = true;
        }

        /// <summary>   
        /// Finalizer   
        /// </summary>   
        ~SqlServer2008()
        {
            this.Dispose();
        }

        private string connectionString;

        /// <summary>   
        /// Connectionstring   
        /// </summary>   
        public string ConnectionString
        {
            get { return this.connectionString; }
            set { this.connectionString = value; }
        }

        private string table;

        /// <summary>   
        /// Data table name   
        /// </summary>   
        public string Table
        {
            get { return this.table; }
            set { this.table = value; }
        }

        private string geometryColumn;

        /// <summary>   
        /// Name of geometry column   
        /// </summary>   
        public string GeometryColumn
        {
            get { return this.geometryColumn; }
            set { this.geometryColumn = value; }
        }

        private string objectIdColumn;

        /// <summary>   
        /// Name of column that contains the Object ID   
        /// </summary>   
        public string ObjectIdColumn
        {
            get { return this.objectIdColumn; }
            set { this.objectIdColumn = value; }
        }

        private bool makeValid;

        /// <summary>
        /// Gets/Sets whether all <see cref="GisSharpBlog.NetTopologySuite.Geometries"/> passed to SqlServer2008 should me made valid using this function.
        /// </summary>
        public Boolean ValidateGeometries { get { return this.makeValid; } set { this.makeValid = value; } }

        private String MakeValidString
        {
            get { return this.makeValid ? ".MakeValid()" : String.Empty; }
        }

        private readonly string spatialObject;
        private readonly SqlServerSpatialObjectType spatialObjectType;        

        /// <summary>
        /// Spatial object type for  
        /// </summary>
        public SqlServerSpatialObjectType SpatialObjectType
        {
            get { return this.spatialObjectType; }
        }

        private long id;

        public long Id
        {
            get { return this.id; }
            set { this.id = value; }
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
            using (SqlConnection conn = new SqlConnection(this.connectionString))
            {
                string strBbox = this.GetBoxFilterStr(bbox);
                string strSQL = String.Format("SELECT g.{0}.STAsBinary() FROM {0} g WHERE ", this.GeometryColumn, this.Table);
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
            using (SqlConnection conn = new SqlConnection(this.connectionString))
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
                                int oid = (int)(decimal)dr[0];
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

        public IGeometry GetGeometryByID(int oid)
        {
            IGeometry geom = null;
            using (SqlConnection conn = new SqlConnection(this.connectionString))
            {
                string strSQL = String.Format(
                    "SELECT g.{0}.STAsBinary() FROM {1} g WHERE {2}='{3}'", 
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
                        }
                    }
                }
                conn.Close();
            }
            return geom;
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

            using (SqlConnection conn = new SqlConnection(this.connectionString))
            {
                string strBbox = this.GetBoxFilterStr(box);
                string strSQL = String.Format(
                    "SELECT g.*, g.{0}{1}.STAsBinary() AS sharpmap_tempgeometry FROM {2} g WHERE ",
                    this.GeometryColumn, this.MakeValidString, this.Table);
                    //NS, 2013-04-03
                    //"SELECT g.id, g.name, g.{0}{1}.STAsBinary() AS sharpmap_tempgeometry FROM {2} g WHERE ",
                    //this.GeometryColumn, this.MakeValidString, this.Table);
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
                        DataSet dataset = new DataSet();
                        //NS, 2013-04-03
                        //FeatureDataSet dataset = new FeatureDataSet();
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
            string bboxText = GeometryToWKT.Write(p);
            string whereClause = String.Format(
                "{0}{1}.STIntersects({4}::STGeomFromText('{2}', {3})) = 1",
                this.GeometryColumn, this.MakeValidString, bboxText, this.SRID, this.spatialObject);
            return whereClause;
        }

        /// <summary>   
        /// Returns the number of features in the dataset   
        /// </summary>   
        /// <returns>number of features</returns>   
        public int GetFeatureCount()
        {
            int count;
            using (SqlConnection conn = new SqlConnection(this.connectionString))
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

        public IFeature GetFeature(int index)
        {
            using (SqlConnection conn = new SqlConnection(this.connectionString))
            {
                int rowId = index + 1; // index looks zero-based
                string strSQL = String.Format(
                    "select g.* , g.{0}.STAsBinary() As sharpmap_tempgeometry from {1} g WHERE {2}={3}", 
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
            using (SqlConnection conn = new SqlConnection(this.connectionString))
            {
                string strSQL = String.Format(
                    //"SELECT g.{0}{1}.STEnvelope().STAsText() FROM {2} g ",
                    //NS 2013-04-03
                    "SELECT g.{0}{1}.STAsText() FROM {2} g ",
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
                        IGeometry g = GeometryFromWKT.Parse(wkt);
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
        /// Gets the connection ID of the datasource   
        /// </summary>   
        public string ConnectionID
        {
            get { return this.connectionString; }
        }
    }
}