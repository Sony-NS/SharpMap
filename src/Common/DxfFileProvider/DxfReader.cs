/*
 * Created  : Sony NS 
 * Descript : Encapsulates the entire DXF document, containing it's layers and entities.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Threading;

namespace DxfFileProvider
{
    public class DxfReader
    {
        //General declarations
        private StreamReader _dxfReader;
        private int _dxfLinesRead = 0;
        private bool _isFileOpen;
        //2014-06-19 coordinate transform
        private GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory _utm50Factory;
        private GeoAPI.CoordinateSystems.Transformations.ICoordinateTransformation _coordTransform;
        private GeoAPI.CoordinateSystems.IProjectedCoordinateSystem _utmWGS84;

        #region Entities
        public List<Layer> Layers { get; set; }
        public List<Line> Lines { get; set; }
        public List<Polyline> Polylines { get; set; }
        public List<Circle> Circles { get; set; }
        public List<Arc> Arcs { get; set; }
        public List<Text> Texts { get; set; }
        public List<Point> Points { get; set; }
        public SharpMap.Data.FeatureDataTable FeatureTable { get; set; }
        #endregion

        /// <summary>
        /// Initializes a new instance of the <c>DXFDoc</c> class.
        /// </summary>
        /// <param name="dxfFile">The path of the DXF file to load</param>
        public DxfReader(string dxfFile)
        {
            Layers = new List<Layer>();
            Lines = new List<Line>();
            Polylines = new List<Polyline>();
            Circles = new List<Circle>();
            Arcs = new List<Arc>();
            Texts = new List<Text>();
            Points = new List<Point>();

            //Make sure we read the DXF decimal separator (.) correctly
            CultureInfo cultureInfo = CultureInfo.CurrentCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            if (File.Exists(dxfFile))
            {
                _dxfReader = new StreamReader(dxfFile);
                this._isFileOpen = true;
            }
        }

        public void Close()
        {
            if (FeatureTable != null)
                FeatureTable.Dispose();
            if (this._isFileOpen)
                _dxfReader.Close();
            this._isFileOpen = false;
        }

        public bool IsFileOpen
        {
            get { return this._isFileOpen; }
        }

        #region SharpMap
        public SharpMap.Data.FeatureDataTable NewTable()
        {
            SharpMap.Data.FeatureDataTable fdt = new SharpMap.Data.FeatureDataTable();

            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_ID, typeof(int));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_HANDLE, typeof(string));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_LAYER_NAME, typeof(string));
            //fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_ELEVATION, typeof(double));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_SHAPE_TYPE, typeof(string));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_LABEL, typeof(string));
            //fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_COLOR, typeof(double));

            return fdt;
        }

        public void ExportToShp(string path, string shpType)
        {
            DxfToShp export = new DxfToShp(FeatureTable, shpType, _utmWGS84.WKT, path);
        }

        /// <summary>
        /// NS, 2014-06-19, Geometry Transform
        /// </summary>
        private void TransformInitialize()
        {
            int utmZone = 50;
            GisSharpBlog.NetTopologySuite.Geometries.PrecisionModel precisionModel =
                new GisSharpBlog.NetTopologySuite.Geometries.PrecisionModel(GeoAPI.Geometries.PrecisionModels.Floating);

            SharpMap.CoordinateSystems.CoordinateSystem wgs84 = SharpMap.CoordinateSystems.GeographicCoordinateSystem.WGS84;

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

            List<GeoAPI.CoordinateSystems.ProjectionParameter> parameters =
                new List<GeoAPI.CoordinateSystems.ProjectionParameter>();
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("latitude_of_origin", 0));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("central_meridian", -183 + 6 * utmZone));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("scale_factor", 0.9996));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("false_easting", 500000));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("false_northing", 10000000));
            GeoAPI.CoordinateSystems.IProjection projection = cFac.CreateProjection("Transverse Mercator", "Transverse Mercator", parameters);

            _utmWGS84 = cFac.CreateProjectedCoordinateSystem("WGS84 UTM Zone " + utmZone.ToString() + "S",
                gcsWGS84, projection, SharpMap.CoordinateSystems.LinearUnit.Metre,
                new GeoAPI.CoordinateSystems.AxisInfo("East", GeoAPI.CoordinateSystems.AxisOrientationEnum.East),
                new GeoAPI.CoordinateSystems.AxisInfo("North", GeoAPI.CoordinateSystems.AxisOrientationEnum.North));

            int SRID_utm50 = Convert.ToInt32(_utmWGS84.AuthorityCode);    //UTM50 SRID

            SharpMap.CoordinateSystems.Transformations.CoordinateTransformationFactory ctFact =
                new SharpMap.CoordinateSystems.Transformations.CoordinateTransformationFactory();

            _coordTransform = ctFact.CreateFromCoordinateSystems(_utmWGS84, wgs84);
            _utm50Factory = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory(precisionModel, SRID_utm50);
        }

        /// <summary>
        /// NS, 2014-06-19, Transform to UTM 50
        /// </summary>
        /// <param name="g"></param>
        /// <returns></returns>
        private GeoAPI.Geometries.IGeometry CreateGeometry(GeoAPI.Geometries.IGeometry g)
        {
            return GisSharpBlog.NetTopologySuite.CoordinateSystems.Transformations.GeometryTransform.TransformGeometry(_utm50Factory,
            g, _coordTransform.MathTransform);
        }

        #endregion

        #region Read And Parse the DXF file

        /// <summary>
        /// Read and parse the DXF file
        /// </summary>
        //public void Read()
        //{
        //    bool entitysection = false;

        //    CodePair code = this.ReadPair();
        //    while ((code.Value != "EOF") && (!_dxfReader.EndOfStream))
        //    {
        //        if (code.Code == 0)
        //        {
        //            //Have we reached the entities section yet?
        //            if (!entitysection)
        //            {
        //                //No, so keep going until we find the ENTIIES section (and since we are here, let's try to read the layers)
        //                switch (code.Value)
        //                {
        //                    case "SECTION":
        //                        string sec = ReadSection(ref code);
        //                        if (sec == "ENTITIES")
        //                            entitysection = true;
        //                        break;
        //                    case "LAYER":
        //                        Layer layer = ReadLayer(ref code);
        //                        Layers.Add(layer);
        //                        break;
        //                    default:
        //                        code = this.ReadPair();
        //                        break;
        //                }
        //            }
        //            else
        //            {
        //                //Yes, so let's read the entities
        //                switch (code.Value)
        //                {
        //                    case "LINE":
        //                        Line line = ReadLine(ref code);
        //                        Lines.Add(line);
        //                        break;
        //                    case "CIRCLE":
        //                        Circle circle = ReadCircle(ref code);
        //                        Circles.Add(circle);
        //                        break;
        //                    case "ARC":
        //                        Arc arc = ReadArc(ref code);
        //                        Arcs.Add(arc);
        //                        break;
        //                    case "POINT":
        //                        Point point = ReadPoint(ref code);
        //                        Points.Add(point);
        //                        break;
        //                    case "TEXT":
        //                        Text text = ReadText(ref code);
        //                        Texts.Add(text);
        //                        break;
        //                    case "POLYLINE":
        //                        Polyline polyline = ReadPolyline(ref code);
        //                        Polylines.Add(polyline);
        //                        break;
        //                    case "LWPOLYLINE":
        //                        Polyline lwpolyline = ReadLwPolyline(ref code);
        //                        Polylines.Add(lwpolyline);
        //                        break;
        //                    default:
        //                        code = this.ReadPair();
        //                        break;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            code = this.ReadPair();
        //        }
        //    }
        //}

        /// <summary>
        /// NS, 2014-06-18
        /// Read and parse the dxf file into feature data table
        /// </summary>
        public void Read()
        {
            bool entitysection = false;
            int idx = 0;
            FeatureTable = new SharpMap.Data.FeatureDataTable();
            SharpMap.Data.FeatureDataRow fdr = null;
            GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory gf = null;
            TransformInitialize();

            FeatureTable.Columns.Add(DxfSchema.GIS_DXF_FLD_ID, typeof(int));
            FeatureTable.Columns.Add(DxfSchema.GIS_DXF_FLD_HANDLE, typeof(string));
            FeatureTable.Columns.Add(DxfSchema.GIS_DXF_FLD_LAYER_NAME, typeof(string));
            //_FeatureTable.Columns.Add(DxfSchema.GIS_DXF_FLD_ELEVATION, typeof(double));
            FeatureTable.Columns.Add(DxfSchema.GIS_DXF_FLD_SHAPE_TYPE, typeof(string));
            FeatureTable.Columns.Add(DxfSchema.GIS_DXF_FLD_LABEL, typeof(string));
            //_FeatureTable.Columns.Add(DxfSchema.GIS_DXF_FLD_COLOR, typeof(double));

            CodePair code = this.ReadPair();
            while ((code.Value != "EOF") && (!_dxfReader.EndOfStream))
            {
                if (code.Code == 0)
                {
                    //Have we reached the entities section yet?
                    if (!entitysection)
                    {
                        //No, so keep going until we find the ENTIIES section (and since we are here, let's try to read the layers)
                        switch (code.Value)
                        {
                            case "SECTION":
                                string sec = ReadSection(ref code);
                                if (sec == "ENTITIES")
                                    entitysection = true;
                                break;
                            case "LAYER":
                                Layer layer = ReadLayer(ref code);
                                Layers.Add(layer);
                                break;
                            default:
                                code = this.ReadPair();
                                break;
                        }
                    }
                    else
                    {
                        //Yes, so let's read the entities
                        switch (code.Value)
                        {
                            case "LINE":
                                Line line = ReadLine(ref code);
                                Lines.Add(line);
                                idx++;
                                fdr = FeatureTable.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = line.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = line.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NLINE;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                //fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(line.Location.ToArray());
                                //fdr.Geometry.SRID = 32750; //WGS 84/ UTM Zone 50S 
                                fdr.Geometry = CreateGeometry((GeoAPI.Geometries.IGeometry)gf.CreateLineString(line.Location.ToArray()));
                                FeatureTable.AddRow(fdr);
                                break;
                            case "CIRCLE":
                                Circle circle = ReadCircle(ref code);
                                Circles.Add(circle);
                                idx++;
                                fdr = FeatureTable.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = circle.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = circle.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NCIRCLE;
                                var cl = new GisSharpBlog.NetTopologySuite.Geometries.CoordinateList(VertexConverter.GetCircleCoordinates(circle, 3), false);
                                cl.CloseRing();
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                //fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreatePolygon(SharpMap.Converters.Geometries.GeometryFactory.CreateLinearRing(GisSharpBlog.NetTopologySuite.Geometries.CoordinateArrays.AtLeastNCoordinatesOrNothing(4, cl.ToCoordinateArray())), null);
                                //fdr.Geometry.SRID = 32750; //WGS 84/ UTM Zone 50S
                                fdr.Geometry = CreateGeometry((GeoAPI.Geometries.IGeometry)gf.CreatePolygon(SharpMap.Converters.Geometries.GeometryFactory.CreateLinearRing(GisSharpBlog.NetTopologySuite.Geometries.CoordinateArrays.AtLeastNCoordinatesOrNothing(4, cl.ToCoordinateArray())), null));
                                FeatureTable.AddRow(fdr);
                                break;
                            case "ARC":
                                Arc arc = ReadArc(ref code);
                                Arcs.Add(arc);
                                idx++;
                                fdr = FeatureTable.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = arc.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = arc.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NARC;
                                List<GeoAPI.Geometries.ICoordinate> coord = VertexConverter.GetArcCoordinates(arc, 2);
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                //fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(coord.ToArray());
                                //fdr.Geometry.SRID = 32750; //WGS 84/ UTM Zone 50S
                                fdr.Geometry = CreateGeometry((GeoAPI.Geometries.IGeometry)gf.CreateLineString(coord.ToArray()));
                                FeatureTable.AddRow(fdr);
                                break;
                            case "POINT":
                                Point point = ReadPoint(ref code);
                                Points.Add(point);
                                idx++;
                                fdr = FeatureTable.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = point.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = point.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NPOINT;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                //fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreatePoint(point.Location);
                                //fdr.Geometry.SRID = 32750; //WGS 84/ UTM Zone 50S
                                fdr.Geometry = CreateGeometry((GeoAPI.Geometries.IGeometry)gf.CreatePoint(point.Location));
                                FeatureTable.AddRow(fdr);
                                break;
                            case "TEXT":
                                Text text = ReadText(ref code);
                                Texts.Add(text);
                                idx++;
                                fdr = FeatureTable.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = text.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = text.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_LABEL] = text.Value;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NTEXT;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                //fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreatePoint(text.Location);
                                //fdr.Geometry.SRID = 32750; //WGS 84/ UTM Zone 50S
                                fdr.Geometry = CreateGeometry((GeoAPI.Geometries.IGeometry)gf.CreatePoint(text.Location));
                                FeatureTable.AddRow(fdr);
                                break;
                            case "POLYLINE":
                                Polyline polyline = ReadPolyline(ref code);
                                Polylines.Add(polyline);
                                idx++;
                                fdr = FeatureTable.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = polyline.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = polyline.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NPOLYLINE;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                //fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(polyline.Location.ToArray());
                                //fdr.Geometry.SRID = 32750; //WGS 84/ UTM Zone 50S
                                fdr.Geometry = CreateGeometry((GeoAPI.Geometries.IGeometry)gf.CreateLineString(polyline.Location.ToArray()));
                                FeatureTable.AddRow(fdr);
                                break;
                            case "LWPOLYLINE":
                                Polyline lwpolyline = ReadLwPolyline(ref code);
                                Polylines.Add(lwpolyline);
                                idx++;
                                fdr = FeatureTable.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = lwpolyline.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = lwpolyline.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NLWPOLYLINE;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                //fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(lwpolyline.Location.ToArray());
                                //fdr.Geometry.SRID = 32750; //WGS 84/ UTM Zone 50S
                                fdr.Geometry = CreateGeometry((GeoAPI.Geometries.IGeometry)gf.CreateLineString(lwpolyline.Location.ToArray()));
                                FeatureTable.AddRow(fdr);
                                break;
                            default:
                                code = this.ReadPair();
                                break;
                        }
                    }
                }
                else
                {
                    code = this.ReadPair();
                }
            }
        }

        /// <summary>
        /// Read and parse the DXF file to FeatureDataRow
        /// </summary>
        /// <returns></returns>
        private SharpMap.Data.FeatureDataTable ReadFeature()
        {
            bool entitysection = false;
            int idx = 0;
            SharpMap.Data.FeatureDataTable fdt = new SharpMap.Data.FeatureDataTable();
            SharpMap.Data.FeatureDataRow fdr = null;
            GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory gf = null;

            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_ID, typeof(int));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_HANDLE, typeof(string));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_LAYER_NAME, typeof(string));
            //fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_ELEVATION, typeof(double));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_SHAPE_TYPE, typeof(string));
            fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_LABEL, typeof(string));
            //fdt.Columns.Add(DxfSchema.GIS_DXF_FLD_COLOR, typeof(double));

            CodePair code = this.ReadPair();
            while ((code.Value != "EOF") && (!_dxfReader.EndOfStream))
            {
                if (code.Code == 0)
                {
                    //Have we reached the entities section yet?
                    if (!entitysection)
                    {
                        //No, so keep going until we find the ENTIIES section (and since we are here, let's try to read the layers)
                        switch (code.Value)
                        {
                            case "SECTION":
                                string sec = ReadSection(ref code);
                                if (sec == "ENTITIES")
                                    entitysection = true;
                                break;
                            case "LAYER":
                                Layer layer = ReadLayer(ref code);
                                Layers.Add(layer);
                                break;
                            default:
                                code = this.ReadPair();
                                break;
                        }
                    }
                    else
                    {
                        //Yes, so let's read the entities
                        switch (code.Value)
                        {
                            case "LINE":
                                Line line = ReadLine(ref code);
                                Lines.Add(line);
                                idx++;
                                fdr = fdt.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = line.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = line.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NLINE;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(line.Location.ToArray());
                                fdt.AddRow(fdr);
                                break;
                            case "CIRCLE":
                                Circle circle = ReadCircle(ref code);
                                Circles.Add(circle);
                                idx++;
                                fdr = fdt.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = circle.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = circle.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NCIRCLE;
                                var cl = new GisSharpBlog.NetTopologySuite.Geometries.CoordinateList(VertexConverter.GetCircleCoordinates(circle, 3), false);
                                cl.CloseRing();
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreatePolygon(SharpMap.Converters.Geometries.GeometryFactory.CreateLinearRing(GisSharpBlog.NetTopologySuite.Geometries.CoordinateArrays.AtLeastNCoordinatesOrNothing(4, cl.ToCoordinateArray())), null);
                                fdt.AddRow(fdr);
                                break;
                            case "ARC":
                                Arc arc = ReadArc(ref code);
                                Arcs.Add(arc);
                                idx++;
                                fdr = fdt.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = arc.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = arc.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NARC;
                                List<GeoAPI.Geometries.ICoordinate> coord = VertexConverter.GetArcCoordinates(arc, 2); 
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(coord.ToArray());
                                fdt.AddRow(fdr);
                                break;
                            case "POINT":
                                Point point = ReadPoint(ref code);
                                Points.Add(point);
                                idx++;
                                fdr = fdt.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = point.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = point.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NPOINT;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                fdr.Geometry = (GeoAPI.Geometries.IGeometry) gf.CreatePoint(point.Location);
                                fdt.AddRow(fdr);
                                break;
                            case "TEXT":
                                Text text = ReadText(ref code);
                                Texts.Add(text);
                                idx++;
                                fdr = fdt.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = text.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = text.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_LABEL] = text.Value;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NTEXT;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                fdr.Geometry = (GeoAPI.Geometries.IGeometry) gf.CreatePoint(text.Location);
                                fdt.AddRow(fdr);
                                break;
                            case "POLYLINE":
                                Polyline polyline = ReadPolyline(ref code);
                                Polylines.Add(polyline);
                                idx++;
                                fdr = fdt.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = polyline.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = polyline.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NPOLYLINE;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(polyline.Location.ToArray());
                                fdt.AddRow(fdr);
                                break;
                            case "LWPOLYLINE":
                                Polyline lwpolyline = ReadLwPolyline(ref code);
                                Polylines.Add(lwpolyline);
                                idx++;
                                fdr = fdt.NewRow();
                                fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
                                fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = lwpolyline.Handle;
                                fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = lwpolyline.Layer;
                                fdr[DxfSchema.GIS_DXF_FLD_SHAPE_TYPE] = DxfSchema.GIS_DXF_NLWPOLYLINE;
                                gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
                                 //new GisSharpBlog.NetTopologySuite.Geometries.PrecisionModel(), 32750);
                                //gf.SRID = 32750;
                                fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(lwpolyline.Location.ToArray());
                                fdt.AddRow(fdr);
                                break;
                            default:
                                code = this.ReadPair();
                                break;
                        }
                    }
                }
                else
                {
                    code = this.ReadPair();
                }
            }

            return fdt;
        }

        #endregion

        #region Entities
        //public List<GeoAPI.Geometries.IEnvelope> ReadEnvelope()
        //{
        //    bool entitysection = false;
        //    int idx = 0;
        //    var boxes = new List<GeoAPI.Geometries.IEnvelope>();
        //    GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory gf = null;

        //    CodePair code = this.ReadPair();
        //    while ((code.Value != "EOF") && (!_dxfReader.EndOfStream))
        //    {
        //        if (code.Code == 0)
        //        {
        //            //Have we reached the entities section yet?
        //            if (!entitysection)
        //            {
        //                //No, so keep going until we find the ENTIIES section (and since we are here, let's try to read the layers)
        //                switch (code.Value)
        //                {
        //                    case "SECTION":
        //                        string sec = ReadSection(ref code);
        //                        if (sec == "ENTITIES")
        //                            entitysection = true;
        //                        break;
        //                    case "LAYER":
        //                        //Layer layer = ReadLayer(ref code);
        //                        //Layers.Add(layer);
        //                        break;
        //                    default:
        //                        code = this.ReadPair();
        //                        break;
        //                }
        //            }
        //            else
        //            {
        //                //Yes, so let's read the entities
        //                switch (code.Value)
        //                {
        //                    case "LINE":
        //                        Line line = ReadLine(ref code);
        //                        Lines.Add(line);
        //                        idx++;
        //                        gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
        //                        fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(line.Location.ToArray());
        //                        fdt.AddRow(fdr);
        //                        break;
        //                    case "CIRCLE":
        //                        Circle circle = ReadCircle(ref code);
        //                        Circles.Add(circle);
        //                        idx++;
        //                        fdr = fdt.NewRow();
        //                        fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
        //                        fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = circle.Handle;
        //                        fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = circle.Layer;
        //                        var cl = new GisSharpBlog.NetTopologySuite.Geometries.CoordinateList(VertexConverter.GetCircleCoordinates(circle, 3), false);
        //                        cl.CloseRing();
        //                        gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
        //                        fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreatePolygon(SharpMap.Converters.Geometries.GeometryFactory.CreateLinearRing(GisSharpBlog.NetTopologySuite.Geometries.CoordinateArrays.AtLeastNCoordinatesOrNothing(4, cl.ToCoordinateArray())), null);
        //                        fdt.AddRow(fdr);
        //                        break;
        //                    case "ARC":
        //                        Arc arc = ReadArc(ref code);
        //                        Arcs.Add(arc);
        //                        idx++;
        //                        fdr = fdt.NewRow();
        //                        fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
        //                        fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = arc.Handle;
        //                        fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = arc.Layer;
        //                        List<GeoAPI.Geometries.ICoordinate> coord = VertexConverter.GetArcCoordinates(arc, 2);
        //                        gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
        //                        fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(coord.ToArray());
        //                        fdt.AddRow(fdr);
        //                        break;
        //                    case "POINT":
        //                        Point point = ReadPoint(ref code);
        //                        boxes.Add(SharpMap.Converters.Geometries.GeometryFactory.CreateEnvelope(
        //                            point.Location.X, point.Location.X, point.Location.Y, point.Location.Y));
        //                        break;
        //                    case "TEXT":
        //                        Text text = ReadText(ref code);
        //                        boxes.Add(SharpMap.Converters.Geometries.GeometryFactory.CreateEnvelope(
        //                            text.Location.X, text.Location.X, text.Location.Y, text.Location.Y));
        //                        break;
        //                    case "POLYLINE":
        //                        Polyline polyline = ReadPolyline(ref code);
        //                        boxes.Add(SharpMap.Converters.Geometries.GeometryFactory.CreateEnvelope(
        //                            polyline.Location[0].X, text.Location.X, text.Location.Y, text.Location.Y));
        //                        break;
        //                    case "LWPOLYLINE":
        //                        Polyline lwpolyline = ReadLwPolyline(ref code);
        //                        Polylines.Add(lwpolyline);
        //                        idx++;
        //                        fdr = fdt.NewRow();
        //                        fdr[DxfSchema.GIS_DXF_FLD_ID] = idx;
        //                        fdr[DxfSchema.GIS_DXF_FLD_HANDLE] = lwpolyline.Handle;
        //                        fdr[DxfSchema.GIS_DXF_FLD_LAYER_NAME] = lwpolyline.Layer;
        //                        gf = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory();
        //                        fdr.Geometry = (GeoAPI.Geometries.IGeometry)gf.CreateLineString(lwpolyline.Location.ToArray());
        //                        fdt.AddRow(fdr);
        //                        break;
        //                    default:
        //                        code = this.ReadPair();
        //                        break;
        //                }
        //            }
        //        }
        //        else
        //        {
        //            code = this.ReadPair();
        //        }
        //    }

        //    return fdt;
        //}

        /// <summary>
        /// Reads a code/value pair at the current line from DXF file
        /// </summary>
        /// <returns>A CodePair object containing code and value for the current line pair</returns>
        private CodePair ReadPair()
        {
            string line, value;
            int code;

            line = _dxfReader.ReadLine();
            _dxfLinesRead++;

            //Only through an exepction if the code value is not numeric, indicating a corrupted file
            if (!int.TryParse(line, out code))
            {
                throw new Exception("Invalid code (" + line + ") at line " + this._dxfLinesRead);
            }
            else
            {
                value = _dxfReader.ReadLine();
                return new CodePair(code, value);
            }
        }

        /// <summary>
        /// Read the HANDLE from the dxf file
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string ReadHandle(ref CodePair code)
        {
            string returnval = "";

            code = this.ReadPair();
            while (code.Code != 0)
            {
                if (code.Code == DxfSchema.GIS_DXF_CHANDLE)
                {
                    returnval = code.Value;
                    break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// Reads the ELEVATION value from the dxf file
        /// </summary>
        /// <param name="code"></param>
        /// <returns></returns>
        private string ReadElevation(ref CodePair code)
        {
            string returnval = "";

            code = this.ReadPair();
            while (code.Code != 0)
            {
                if (code.Code == DxfSchema.GIS_DXF_C30)
                {
                    returnval = code.Value;
                    break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// Reads the SECTION name from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A string containing the section name</returns>
        private string ReadSection(ref CodePair code)
        {
            string returnval = "";

            code = this.ReadPair();
            while (code.Code != 0)
            {
                if (code.Code == 2)
                {
                    returnval = code.Value;
                    break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// Reads the LINE data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Line object with layer and two point data</returns>
        private Line ReadLine(ref CodePair code)
        {
            Line returnval = new Line(new List<GeoAPI.Geometries.ICoordinate>(), "0", "0");
            GeoAPI.Geometries.ICoordinate coord1 = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate();
            GeoAPI.Geometries.ICoordinate coord2 = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate();
            //int point = 0;
            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CHANDLE:
                        returnval.Handle = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_C10:
                        //returnval.Location.X = double.Parse(code.Value);
                        coord1.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        //returnval.Location.Y = double.Parse(code.Value);
                        coord1.Y = double.Parse(code.Value);
                        //returnval.Location.Add(coord1);
                        break;            
                    case DxfSchema.GIS_DXF_C30:
                        //returnval.Elevation = double.Parse(code.Value);
                        coord1.Z = double.Parse(code.Value);
                        coord2.Z = double.Parse(code.Value);
                        //if (returnval.Location.Count > 0)
                        //{
                        //    var toUpdate = returnval.Location.First<GeoAPI.Geometries.ICoordinate>();
                        //    returnval.Location.Remove(toUpdate);
                        //    returnval.Location.Add(coord);
                        //}
                        //returnval.Location[point].Z = coord1.Z;
                        break; 
                    case DxfSchema.GIS_DXF_C11:
                        //returnval.P2.X = double.Parse(code.Value);
                        coord2.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C21:
                        //returnval.P2.Y = double.Parse(code.Value);
                        coord2.Y = double.Parse(code.Value);
                        //returnval.Location.Add(coord2);
                        //point++;
                        break;                    
                }
                code = this.ReadPair();
            }

            returnval.Location.Add(coord1);
            returnval.Location.Add(coord2);
            return returnval;
        }

        /// <summary>
        /// Reads the ARC data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>An Arc object with layer, center point, radius, start angle and end angle data</returns>
        private Arc ReadArc(ref CodePair code)
        {
            Arc returnval = new Arc(new GisSharpBlog.NetTopologySuite.Geometries.Coordinate(), 0, 0, 0, "0", "0");

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CHANDLE:
                        returnval.Handle = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_C10:
                        returnval.Center.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        returnval.Center.Y = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C30:
                        returnval.Center.Z = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C40:
                        returnval.Radius = double.Parse(code.Value);
                        break;                    
                    case DxfSchema.GIS_DXF_C50:
                        returnval.StartAngle = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C51:
                        returnval.EndAngle = double.Parse(code.Value);
                        break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// 2014-06-16
        /// Reads the LWPOLYLINE data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Polyline object with layer, closed flag and vertex list data</returns>
        private Polyline ReadLwPolyline(ref CodePair code)
        {
            Polyline returnval = new Polyline(new List<GeoAPI.Geometries.ICoordinate>(), "0", "0", false);
            GeoAPI.Geometries.ICoordinate vtx;// = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate();
            int flags = 0;
            double pointX = 0, pointY = 0;
            double pointZ = 0;

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CHANDLE:
                        returnval.Handle = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CVERTEXATTR:
                        flags = int.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C10:
                        //vtx = new Vertex(Vector2d.Zero);
                        //if (point > 0) returnval.Location.Add(vtx);
                        //point++;
                        pointX = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        pointY = double.Parse(code.Value);
                        vtx = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate();
                        vtx.X = pointX;
                        vtx.Y = pointY;
                        vtx.Z = pointZ;
                        returnval.Location.Add(vtx);
                        break;
                    case DxfSchema.GIS_DXF_C30:
                        pointZ = double.Parse(code.Value);
                        //if (returnval.Location.Count > 0)
                        //{
                        //    var toUpdate = returnval.Location.First<GeoAPI.Geometries.ICoordinate>();
                        //    returnval.Location.Remove(toUpdate);
                        //    returnval.Location.Add(vtx);
                        //}
                        break;
                    //case 42:
                    //    vtx.Bulge = double.Parse(code.Value);
                    //    break;
                }
                code = this.ReadPair();
            }

            if ((flags & 1) == 1)
                returnval.Closed = true;

            return returnval;
        }

        //private Polyline ReadLightWeightPolyline(ref CodePair code)
        //{
        //    Polyline returnval = new Polyline(new List<GeoAPI.Geometries.ICoordinate>(), "0", "0", false);
        //    GeoAPI.Geometries.ICoordinate vtx = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate();
        //    int flags = 0;

        //    code = this.ReadPair();
        //    while (code.Code != 0)
        //    {
        //        switch (code.Code)
        //        {
        //            case DxfSchema.GIS_DXF_CHANDLE:
        //                returnval.Handle = code.Value;
        //                break;
        //            case DxfSchema.GIS_DXF_CLAYER:
        //                returnval.Layer = code.Value;
        //                break;
        //            case DxfSchema.GIS_DXF_CVERTEXATTR:
        //                flags = int.Parse(code.Value);
        //                break;
        //            case DxfSchema.GIS_DXF_C10:
        //                vtx.X = double.Parse(code.Value);
        //                break;
        //            case DxfSchema.GIS_DXF_C20:
        //                vtx.Y = double.Parse(code.Value);
        //                returnval.Location.Add(vtx);
        //                break;
        //            case DxfSchema.GIS_DXF_C30:
        //                vtx.Z = double.Parse(code.Value);
        //                break;
        //            //case 42:
        //            //    vtx.Bulge = double.Parse(code.Value);
        //            //    break;
        //        }
        //        code = this.ReadPair();
        //    }

        //    if ((flags & 1) == 1)
        //        returnval.Closed = true;

        //    return returnval;
        //}

        /// <summary>
        /// Reads the POLYLINE data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Polyline object with layer, closed flag and vertex list data</returns>
        private Polyline ReadPolyline(ref CodePair code)
        {
            Polyline returnval = new Polyline(new List<GeoAPI.Geometries.ICoordinate>(), "0", "0", false);
            int flags = 0;

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CHANDLE:
                        returnval.Handle = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CVERTEXATTR:
                        flags = int.Parse(code.Value);
                        break;
                }
                code = this.ReadPair();
            }

            while (code.Value != DxfSchema.GIS_DXF_NSEQEND)
            {
                if (code.Value == DxfSchema.GIS_DXF_NVERTEX)
                {
                    GeoAPI.Geometries.ICoordinate vtx = ReadCoordinate(ref code);
                    returnval.Location.Add(vtx);
                }
                else
                {
                    code = this.ReadPair();
                }
            }

            if ((flags & 1) == 1)
                returnval.Closed = true;

            return returnval;
        }

        /// <summary>
        /// Reads the VERTEX data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Vertex object with layer, position and bulge data</returns>
        private Vertex ReadVertex(ref CodePair code)
        {
            Vertex returnval = new Vertex(0, 0, 0, 0, "0");

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_C10:
                        returnval.Position.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        returnval.Position.Y = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C30:
                        returnval.Elevation = double.Parse(code.Value);
                        break;
                    case 42:
                        returnval.Bulge = double.Parse(code.Value);
                        break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        private GeoAPI.Geometries.ICoordinate ReadCoordinate(ref CodePair code)
        {
            GeoAPI.Geometries.ICoordinate returnval = new GisSharpBlog.NetTopologySuite.Geometries.Coordinate();

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    //case DxfSchema.GIS_DXF_CLAYER:
                    //    returnval.Layer = code.Value;
                    //    break;
                    case DxfSchema.GIS_DXF_C10:
                        returnval.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        returnval.Y = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C30:
                        returnval.Z = double.Parse(code.Value);
                        break;
                    //case 42:
                    //    returnval.Bulge = double.Parse(code.Value);
                    //    break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// Reads the CIRCLE data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Circle object with layer, center point and radius data</returns>
        private Circle ReadCircle(ref CodePair code)
        {
            Circle returnval = new Circle(new GisSharpBlog.NetTopologySuite.Geometries.Coordinate(), 0, "0", "0");

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CHANDLE:
                        returnval.Handle = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_C10:
                        returnval.Center.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        returnval.Center.Y = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C30:
                        returnval.Center.Z = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C40:
                        returnval.Radius = double.Parse(code.Value);
                        break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// Reads the POINT data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Point object with layer and position data</returns>
        private Point ReadPoint(ref CodePair code)
        {
            Point returnval = new Point(new GisSharpBlog.NetTopologySuite.Geometries.Coordinate(), "0", "0");

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CHANDLE:
                        returnval.Handle = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_C10:
                        returnval.Location.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        returnval.Location.Y = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C30:
                        returnval.Location.Z = double.Parse(code.Value);
                        break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// Reads the TEXT data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Text object with layer, value (text) and position data</returns>
        private Text ReadText(ref CodePair code)
        {
            Text returnval = new Text(new GisSharpBlog.NetTopologySuite.Geometries.Coordinate(), "", "0", "0");

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CHANDLE:
                        returnval.Handle = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_C1:
                        returnval.Value = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CLAYER:
                        returnval.Layer = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_C10:
                        returnval.Location.X = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C20:
                        returnval.Location.Y = double.Parse(code.Value);
                        break;
                    case DxfSchema.GIS_DXF_C30:
                        returnval.Location.Z = double.Parse(code.Value);
                        break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        /// <summary>
        /// Reads the LAYER data from the DXF file
        /// </summary>
        /// <param name="code">A reference to the current CodePair read</param>
        /// <returns>A Layer object with name and AciColor index</returns>
        private Layer ReadLayer(ref CodePair code)
        {
            Layer returnval = new Layer("0", 0);

            code = this.ReadPair();
            while (code.Code != 0)
            {
                switch (code.Code)
                {
                    case DxfSchema.GIS_DXF_CENTITIES:
                        returnval.Name = code.Value;
                        break;
                    case DxfSchema.GIS_DXF_CCOLOR:
                        returnval.ColorIndex = int.Parse(code.Value);
                        break;
                }
                code = this.ReadPair();
            }

            return returnval;
        }

        #endregion
    }
}
