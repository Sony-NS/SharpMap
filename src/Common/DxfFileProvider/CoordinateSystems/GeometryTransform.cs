/*
 * Created  : Sony NS @ SNC Bandung 2014-06-19 
 * Descript : dxf geometry transform
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DxfFileProvider.CoordinateSystems
{
    public class GeometryTransform
    {
        private GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory _UTM50Factory;
        GeoAPI.CoordinateSystems.Transformations.ICoordinateTransformation _Transform;

        private void Initialize()
        {
            GisSharpBlog.NetTopologySuite.Geometries.PrecisionModel precisionModel =
                new GisSharpBlog.NetTopologySuite.Geometries.PrecisionModel(GeoAPI.Geometries.PrecisionModels.Floating);

            SharpMap.CoordinateSystems.CoordinateSystem wgs84 = SharpMap.CoordinateSystems.GeographicCoordinateSystem.WGS84;

            GeoAPI.CoordinateSystems.ICoordinateSystemFactory cFac = new SharpMap.CoordinateSystems.CoordinateSystemFactory();
            //Create geographic coordinate system based on the WGS84 datum
            GeoAPI.CoordinateSystems.IEllipsoid ellipsoid = cFac.CreateFlattenedSphere("WGS 84",
                6378137, 298.257223563, SharpMap.CoordinateSystems.LinearUnit.Metre);
            GeoAPI.CoordinateSystems.IHorizontalDatum datum = cFac.CreateHorizontalDatum("WGS_1984",
                GeoAPI.CoordinateSystems.DatumType.HD_Geocentric, ellipsoid, null);
            GeoAPI.CoordinateSystems.IGeographicCoordinateSystem gcsWGS84 = cFac.CreateGeographicCoordinateSystem("WGS 84",
                SharpMap.CoordinateSystems.AngularUnit.Degrees, datum, SharpMap.CoordinateSystems.PrimeMeridian.Greenwich,
                new GeoAPI.CoordinateSystems.AxisInfo("Lon", GeoAPI.CoordinateSystems.AxisOrientationEnum.East),
                new GeoAPI.CoordinateSystems.AxisInfo("Lat", GeoAPI.CoordinateSystems.AxisOrientationEnum.North));

            List<GeoAPI.CoordinateSystems.ProjectionParameter> parameters =
                new List<GeoAPI.CoordinateSystems.ProjectionParameter>();
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("latitude_of_origin", 0));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("central_meridian", 117));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("scale_factor", 0.9996));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("false_easting", 500000));
            parameters.Add(new GeoAPI.CoordinateSystems.ProjectionParameter("false_northing", 10000000));
            GeoAPI.CoordinateSystems.IProjection projection = cFac.CreateProjection("Transverse Mercator", "Transverse Mercator", parameters);

            GeoAPI.CoordinateSystems.IProjectedCoordinateSystem utmWGS84 = cFac.CreateProjectedCoordinateSystem("WGS84 UTM Zone 50S",
                gcsWGS84, projection, SharpMap.CoordinateSystems.LinearUnit.Metre,
                new GeoAPI.CoordinateSystems.AxisInfo("East", GeoAPI.CoordinateSystems.AxisOrientationEnum.East),
                new GeoAPI.CoordinateSystems.AxisInfo("North", GeoAPI.CoordinateSystems.AxisOrientationEnum.North));

            int SRID_utm50 = Convert.ToInt32(utmWGS84.AuthorityCode);    //UTM50 SRID

            SharpMap.CoordinateSystems.Transformations.CoordinateTransformationFactory ctFact =
                new SharpMap.CoordinateSystems.Transformations.CoordinateTransformationFactory();
            
            _Transform = ctFact.CreateFromCoordinateSystems(utmWGS84, wgs84);
            _UTM50Factory = new GisSharpBlog.NetTopologySuite.Geometries.GeometryFactory(precisionModel, SRID_utm50);
        }

        public GeoAPI.Geometries.IGeometry CreateGeometry(GeoAPI.Geometries.IGeometry g)
        {
            return GisSharpBlog.NetTopologySuite.CoordinateSystems.Transformations.GeometryTransform.TransformGeometry(_UTM50Factory,
            g, _Transform.MathTransform);
        }
    }
}
