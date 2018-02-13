/*
 * Created  : Sony NS 
 * Descript : DXF File
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DelftTools.Utils.Data;
using DelftTools.Utils.IO;
using GeoAPI.CoordinateSystems;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
//using log4net;
using SharpMap.Converters.Geometries;
using SharpMap.Converters.WellKnownText;
using SharpMap.Data;
using SharpMap.Data.Providers;
using System.IO;

namespace DxfFileProvider
{
    public class DxfFile : Unique<long>, IFileBasedFeatureProvider
    {
        #region Delegates

        public delegate bool FilterMethod(FeatureDataRow dr);

        #endregion

        private ICoordinateSystem _CoordinateSystem;
        //private IEnvelope _Envelope;
        private FeatureDataTable _FeatureTable;
        private FilterMethod _FilterDelegate;
        private int _FeatureCount;
        //private bool _FileBasedIndex;
        private string path;
        private bool _IsOpen;
        //private int _LineCode;
        //private int _LineNo;
        //private string _LineName;

        private SharpMap.Data.Providers.ShapeType _ShapeType;
        private int _SRID = -1;//32750; //WGS 84/ UTM Zone 50S
        private DxfReader _DxfFile;
        
        public DxfFile(string filename)
        {
            Open(filename);
        }

        private void InitializeDxf(string filename)
        {
            if (!File.Exists(filename))
                throw new FileNotFoundException(String.Format("Could not find file \"{0}\"", filename));
            if (!filename.ToLower().EndsWith(".dxf"))
                throw (new Exception("Invalid dxf file filename: " + filename));	
        }

        /// <summary>
        /// Gets the <see cref="SharpMap.Data.Providers.ShapeType">shape geometry type</see> in this shapefile.
        /// </summary>
        /// <remarks>
        /// The property isn't set until the first time the datasource has been opened,
        /// and will throw an exception if this property has been called since initialization. 
        /// <para>All the non-Null shapes in a shapefile are required to be of the same shape
        /// type.</para>
        /// </remarks>
        public virtual ShapeType ShapeType
        {
            get { return _ShapeType; }
        }

        public virtual FilterMethod FilterDelegate
        {
            get { return _FilterDelegate; }
            set { _FilterDelegate = value; }
        }

        //"PROJCS["WGS_1984_UTM_Zone_50S",
        //GEOGCS["GCS_WGS_1984",
        //DATUM["D_WGS_1984",
        //SPHEROID["WGS_1984",6378137,298.257223563]],
        //PRIMEM["Greenwich",0],
        //UNIT["Degree",0.017453292519943295]],
        //PROJECTION["Transverse_Mercator"],
        //PARAMETER["latitude_of_origin",0],
        //PARAMETER["central_meridian",117],
        //PARAMETER["scale_factor",0.9996],
        //PARAMETER["false_easting",500000],
        //PARAMETER["false_northing",10000000],UNIT["Meter",1]]";
        private GeoAPI.CoordinateSystems.IProjectedCoordinateSystem CreateUtmProjection(int utmZone)
        {
            GeoAPI.CoordinateSystems.ICoordinateSystemFactory cFac = new SharpMap.CoordinateSystems.CoordinateSystemFactory();
            //Create geographic coordinate system based on the WGS84 datum
            GeoAPI.CoordinateSystems.IEllipsoid ellipsoid = cFac.CreateFlattenedSphere("WGS_1984",
                6378137, 298.257223563, SharpMap.CoordinateSystems.LinearUnit.Metre);
            GeoAPI.CoordinateSystems.IHorizontalDatum datum = cFac.CreateHorizontalDatum("D_WGS_1984",
                GeoAPI.CoordinateSystems.DatumType.HD_Geocentric, ellipsoid, null);
            GeoAPI.CoordinateSystems.IGeographicCoordinateSystem gcsWGS84 = cFac.CreateGeographicCoordinateSystem("GCS_WGS_1984",
                SharpMap.CoordinateSystems.AngularUnit.Degrees, datum, SharpMap.CoordinateSystems.PrimeMeridian.Greenwich,
                new GeoAPI.CoordinateSystems.AxisInfo("Lon", GeoAPI.CoordinateSystems.AxisOrientationEnum.East),
                new GeoAPI.CoordinateSystems.AxisInfo("Lat", GeoAPI.CoordinateSystems.AxisOrientationEnum.North));

            //Create UTM projection
            List<GeoAPI.CoordinateSystems.ProjectionParameter> parameters =
                new List<GeoAPI.CoordinateSystems.ProjectionParameter>();
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("latitude_of_origin", 0));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("central_meridian", -183 + 6 * utmZone));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("scale_factor", 0.9996));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("false_easting", 500000));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("false_northing", 10000000));
            GeoAPI.CoordinateSystems.IProjection projection = cFac.CreateProjection("Transverse Mercator", "Transverse Mercator", parameters);

            return cFac.CreateProjectedCoordinateSystem("WGS84 UTM Zone " + utmZone.ToString() + "S",
                gcsWGS84, projection, SharpMap.CoordinateSystems.LinearUnit.Metre,
                new GeoAPI.CoordinateSystems.AxisInfo("East", GeoAPI.CoordinateSystems.AxisOrientationEnum.East),
                new GeoAPI.CoordinateSystems.AxisInfo("North", GeoAPI.CoordinateSystems.AxisOrientationEnum.North));
        }

        public virtual ICoordinateSystem CoordinateSystem
        {
            get { return _CoordinateSystem; }
            set { _CoordinateSystem = value; }
        }

        #region Disposers and finalizers

        private bool _Disposed = false;
        /// <summary>
        /// Disposes the object
        /// </summary>
        public virtual void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!_Disposed)
            {
                if (disposing)
                {
                    Close();
                    //_Envelope = null;
                }
                _Disposed = true;
            }
        }

        /// <summary>
        /// Finalizes the object
        /// </summary>
        ~DxfFile()
        {
            Dispose();
        }
        #endregion

        #region DxfFile

        //private FeatureDataTable ReadFile(Stream stream)
        //{
        //    try
        //    {
        //        _Reader = new StreamReader(stream, true);

        //        FeatureDataTable fdt = new FeatureDataTable();
        //        FeatureDataRow fdr = null;
                
        //        fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_HANDLE, typeof(string));
        //        fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_LAYER_NAME, typeof(string));
        //        fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_ELEVATION, typeof(double));
        //        fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_LABEL, typeof(string));
        //        fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_COLOR, typeof(double));

        //        _LineNo = 0;
        //        _LineName = "";

        //        try
        //        {
        //            bool found = false;
        //            while ((_Reader.ReadLine() != null) && (!dxfTestLine(DxfSchema.GIS_DXF_CEOF, DxfSchema.GIS_DXF_NEOF)))
        //            {
        //                if (dxfTestLine(DxfSchema.GIS_DXF_CENTITIES, DxfSchema.GIS_DXF_NENTITIES))
        //                {
        //                    found=true;
        //                    break;
        //                }
        //                dxfFetchLine();
        //            }

        //            if (found)
        //            {
        //                dxfFetchLine();
        //                while ((_Reader.ReadLine() != null) && (!dxfTestLine(DxfSchema.GIS_DXF_CEOF, DxfSchema.GIS_DXF_NEOF)))
        //                {
        //                    if (dxfTestLine(DxfSchema.GIS_DXF_CPOINT, DxfSchema.GIS_DXF_NPOINT))
        //                        DoPoint(fdt);
        //                    else if (dxfTestLine(DxfSchema.GIS_DXF_CTEXT, DxfSchema.GIS_DXF_NTEXT))
        //                        DoText(fdt);
        //                    else if (dxfTestLine(DxfSchema.GIS_DXF_CSOLID, DxfSchema.GIS_DXF_NSOLID))
        //                        DoSolid(fdt);
        //                    else if (dxfTestLine(DxfSchema.GIS_DXF_CLINE, DxfSchema.GIS_DXF_NLINE))
        //                        DoLine(fdt);
        //                    else if (dxfTestLine(DxfSchema.GIS_DXF_CARC, DxfSchema.GIS_DXF_NARC))
        //                        DoArc(fdt);
        //                    else if (dxfTestLine(DxfSchema.GIS_DXF_CCIRCLE, DxfSchema.GIS_DXF_NCIRCLE))
        //                        DoCircle(fdt);
        //                    else if (dxfTestLine(DxfSchema.GIS_DXF_CPOLYLINE, DxfSchema.GIS_DXF_NPOLYLINE))
        //                        DoPolyline(fdt);
        //                    else if (dxfTestLine(DxfSchema.GIS_DXF_CLWPOLYLINE, DxfSchema.GIS_DXF_NLWPOLYLINE))
        //                        DoLwPolyline(fdt);
        //                    else dxfFetchLine();
        //                }
        //            }
        //        }
        //        catch
        //        {
        //            //
        //        }
        //    }
        //    catch //(Exception ex)
        //    {
        //        //throw (new "Unknow error opening the reader.", ex));
        //    }
        //}

        #endregion

        #region IFileBased Members

        /// <summary>
        /// Returns true if the datasource is currently open
        /// </summary>		
        public virtual bool IsOpen
        {
            get { return _IsOpen; }
        }

        public virtual string Path
        {
            get { return path; }
            set
            {
                path = value;
                Open(path);
            }
        }

        public virtual void CreateNew(string path)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Closes the datasource
        /// </summary>
        public virtual void Close()
        {
            if (!_Disposed)
            {
                //TODO: (ConnectionPooling)
                /*	if (connector != null)
					{ Pooling.ConnectorPool.ConnectorPoolManager.Release...()
				}*/
                if (_IsOpen)
                {
                    _DxfFile.Close();
                    //_fsDxfFile.Close();

                    _IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Opens the datasource
        /// </summary>
        public virtual void Open(string path)
        {
            // Get a Connector.  The connector returned is guaranteed to be connected and ready to go.
            // Pooling.Connector connector = Pooling.ConnectorPool.ConnectorPoolManager.RequestConnector(this,true);

            if (!File.Exists(path))
            {
                //log.Error("Could not find " + path);
                return;
            }

            if (!_IsOpen || this.path != path)
            {
                try
                {
                    this.path = path;
                    //string wkt = SharpMap.Converters.WellKnownText.SpatialReference.SridToWkt(32750);
                        //"PROJCS["WGS_1984_UTM_Zone_50S",GEOGCS["GCS_WGS_1984",DATUM["D_WGS_1984",SPHEROID["WGS_1984",6378137,298.257223563]],PRIMEM["Greenwich",0],UNIT["Degree",0.017453292519943295]],PROJECTION["Transverse_Mercator"],PARAMETER["latitude_of_origin",0],PARAMETER["central_meridian",117],PARAMETER["scale_factor",0.9996],PARAMETER["false_easting",500000],PARAMETER["false_northing",10000000],UNIT["Meter",1]]";
                    _DxfFile = new DxfFileProvider.DxfReader(path);
                    InitializeDxf(this.path);
                    //Read feature
                    _CoordinateSystem = CreateUtmProjection(50);
                    
                    _DxfFile.Read();
                    _FeatureTable = _DxfFile.FeatureTable;
                    _IsOpen = true;
                }
                catch //(IOException e)
                {
                    //log.Error(e.Message);
                    _IsOpen = false;
                }
            }
        }

        public virtual void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public virtual void SwitchTo(string newPath)
        {
            Close();
            Open(newPath);
        }

        public virtual void Delete()
        {
            File.Delete(Path);
        }

        #endregion

        #region IFileBasedFeatureProvider

        public virtual string FileFilter
        {
            get { return "Dxf file (*.dxf)|*.dxf"; }
        }

        public virtual bool IsRelationalDataBase
        {
            get { return false; }
        }

        #endregion

        #region IFeatureProvider Members

        public virtual Type FeatureType
        {
            get { return typeof(SharpMap.Data.FeatureDataRow); }
        }

        public virtual IList Features
        {
            get
            {
                //problem with reuse 
                return GetFeatures(GetExtents());
            }
            set { throw new NotImplementedException(); }
        }

        public virtual IFeature Add(IGeometry geometry)
        {
            throw new NotImplementedException();
        }

        public virtual Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox)
        {
            return GetGeometriesInView(bbox, -1);
        }

        /// <summary>
        /// Returns geometries whose bounding box intersects 'bbox'
        /// </summary>
        /// <remarks>
        /// <para>Please note that this method doesn't guarantee that the geometries returned actually intersect 'bbox', but only
        /// that their boundingbox intersects 'bbox'.</para>
        /// <para>This method is much faster than the QueryFeatures method, because intersection tests
        /// are performed on objects simplifed by their boundingbox, and using the Spatial Index.</para>
        /// </remarks>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double tolerance)
        {
            if (!IsOpen)
            {
                Open(Path);
            }

            System.Collections.ObjectModel.Collection<IGeometry> geom = new System.Collections.ObjectModel.Collection<IGeometry>();

            foreach (FeatureDataRow r in _FeatureTable)
            {
                if (r.Geometry != null)
                    geom.Add(r.Geometry);
            }

            return geom;
        }

        /// <summary>
        /// Returns all objects whose boundingbox intersects bbox.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Please note that this method doesn't guarantee that the geometries returned actually intersect 'bbox', but only
        /// that their boundingbox intersects 'bbox'.
        /// </para>
        /// </remarks>
        /// <param name="bbox"></param>
        /// <returns></returns>
        public virtual IList GetFeatures(IEnvelope bbox)
        {
            if (!IsOpen)
            {
                Open(Path);
            }

            if (!_IsOpen) return new List<IFeature>(); //return empty list in case there is no connection

            var table = _DxfFile.NewTable();

            foreach (FeatureDataRow fdr in _FeatureTable)
            {
                if (fdr.Geometry != null)
                {
                    if (fdr.Geometry.EnvelopeInternal.Intersects(bbox))
                    {
                        if (FilterDelegate == null || FilterDelegate(fdr))
                        {
                            FeatureDataRow dr = table.NewRow();
                            dr[DxfSchema.GIS_DXF_FLD_ID] = fdr[DxfSchema.GIS_DXF_FLD_ID];
                            dr[DxfSchema.GIS_DXF_FLD_HANDLE] = fdr[DxfSchema.GIS_DXF_FLD_HANDLE];
                            dr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME];
                            dr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE];
                            dr[DxfSchema.GIS_DXF_FLD_LABEL] = fdr[DxfSchema.GIS_DXF_FLD_LABEL];
                            dr.Geometry = fdr.Geometry;
                            table.AddRow(dr);
                        }
                    }
                }
            }

            return table;
        }

        /// <summary>
        /// Returns geometry Object IDs whose bounding box intersects 'envelope'
        /// </summary>
        /// <param name="envelope"></param>
        /// <returns></returns>
        public virtual ICollection<int> GetObjectIDsInView(IEnvelope envelope)
        {
            if (!IsOpen)
            {
                Open(Path);
            }

            // Identifies all the features within the given BoundingBox
            System.Collections.ObjectModel.Collection<int> geoms = new System.Collections.ObjectModel.Collection<int>();
            foreach (FeatureDataRow fdr in _FeatureTable)
                if (envelope.Intersects(((IFeature)fdr).Geometry.EnvelopeInternal))
                    geoms.Add(Convert.ToInt32(fdr.Attributes["DXF_ID"]));
            return geoms;    
        }

        /// <summary>
        /// Returns the geometry corresponding to the Object ID
        /// </summary>
        /// <param name="oid">Object ID</param>
        /// <returns>geometry</returns>
        public virtual IGeometry GetGeometryByID(int oid)
        {
            if (FilterDelegate != null) //Apply filtering
            {
                // TODO: this should work as IFeature
                var fdr = (FeatureDataRow)GetFeature(oid);
                if (fdr != null)
                    return fdr.Geometry;
                else
                    return null;
            }
            else return ReadGeometry(oid);
        }

        /// <summary>
        /// Returns the data associated with all the geometries that are intersected by 'geom'.
        /// Please note that the DxfFile provider currently doesn't fully support geometryintersection
        /// and thus only BoundingBox/BoundingBox querying are performed. The results are NOT
        /// guaranteed to lie withing 'geom'.
        /// </summary>
        /// <param name="geom"></param>
        public virtual IList GetFeatures(IGeometry geom)
        {
            if (!IsOpen)
            {
                Open(Path);
            }

            var dt = _DxfFile.NewTable();
            foreach (FeatureDataRow fdr in _FeatureTable)
            {
                if (fdr.Geometry != null)
                    if (fdr.Geometry.Intersects(geom))
                        if (FilterDelegate == null || FilterDelegate(fdr))
                            dt.AddRow(fdr);
            }

            return dt;
        }


        /// <summary>
        /// Returns the total number of features in the datasource (without any filter applied)
        /// </summary>
        /// <returns></returns>
        public virtual int GetFeatureCount()
        {
            return _FeatureCount;
        }

        /// <summary>
        /// Gets a datarow from the datasource at the specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public virtual IFeature GetFeature(int index)
        {
            return GetFeature(index, null);
        }

        public virtual bool Contains(IFeature feature)
        {
            //if (feature is FeatureDataRow)
            //{
            //    return dbaseFile.NewTable.Rows.Contains(feature);
            //}

            return false;
            //throw new NotImplementedException();
        }

        public virtual int IndexOf(IFeature feature)
        {
            //if (feature is FeatureDataRow)
            //{
            //    return dbaseFile.NewTable.Rows.IndexOf((FeatureDataRow)feature);
            //}
            return -1;
            //throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the extents of the datasource
        /// </summary>
        /// <returns></returns>
        public virtual IEnvelope GetExtents()
        {
            if (!IsOpen)
            {
                Open(path);
            }

            IEnvelope envelope = new GisSharpBlog.NetTopologySuite.Geometries.Envelope();
            foreach (FeatureDataRow feature in _FeatureTable)
                envelope.ExpandToInclude(feature.Geometry.EnvelopeInternal);

            return envelope;
        }

        /// <summary>
        /// The spatial reference ID (CRS)
        /// </summary>
        public virtual int SRID
        {
            get { return _SRID; }
            set { _SRID = value; }
        }

        private List<IEnvelope> GetAllFeatureBoundingBoxes()
        {
            if (!IsOpen)
            {
                Open(Path);
            }

            var boxes = new List<IEnvelope>();
            foreach (FeatureDataRow r in _FeatureTable)
            {
                boxes.Add(r.Geometry.EnvelopeInternal);
            }
            return boxes;
        }

        private IGeometry ReadGeometry(int oid)
        {
            if (!IsOpen)
            {
                Open(Path);
            }

            IGeometry geom = null;
            foreach (FeatureDataRow r in _FeatureTable)
            {
                if (Convert.ToInt32(r.Attributes[DxfSchema.GIS_DXF_FLD_ID]) == oid)
                {
                    geom = r.Geometry;
                    break;
                }
            }
            return geom;
        }

        public virtual FeatureDataRow GetFeature(int RowID, FeatureDataTable dt)
        {
            if (!IsOpen)
            {
                Open(Path);
            }

            FeatureDataTable fdt = _DxfFile.NewTable();
            FeatureDataRow fdr = fdt.NewRow();
            foreach (FeatureDataRow r in _FeatureTable)
            {
                if (Convert.ToInt32(r.Attributes[DxfSchema.GIS_DXF_FLD_ID]) == RowID)
                {
                    fdr = r;
                    break;
                }
            }
            return fdr;
        }

        #endregion

        #region FileWriting

        public void ExportToShp(string path, string shpType)
        {
            DxfToShp export = new DxfToShp(_FeatureTable, shpType, _CoordinateSystem.WKT, path);
        }

        #endregion

    }
}
