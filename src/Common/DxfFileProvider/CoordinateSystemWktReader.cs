using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DxfFileProvider
{
    public class CoordinateSystemWktReader
    {
        /// <summary> 
        /// Reads and parses a WKT-formatted projection string. 
        /// </summary> 
        /// <param name="wkt">String containing WKT.</param> 
        /// <returns>Object representation of the WKT.</returns> 
        /// <exception cref="System.ArgumentException">If a token is not recognised.</exception> 
        public static GeoAPI.CoordinateSystems.IInfo Parse(string wkt)
        {
            GeoAPI.CoordinateSystems.IInfo returnObject = null;
            StringReader reader = new StringReader(wkt);
            GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer = new GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer(reader);
            tokenizer.NextToken();
            string objectName = tokenizer.GetStringValue();
            switch (objectName)
            {
                case "UNIT":
                    returnObject = ReadUnit(tokenizer);
                    break;
                //case "VERT_DATUM": 
                //    IVerticalDatum verticalDatum = ReadVerticalDatum(tokenizer); 
                //    returnObject = verticalDatum; 
                //    break; 
                case "SPHEROID":
                    returnObject = ReadEllipsoid(tokenizer);
                    break;
                case "DATUM":
                    returnObject = ReadHorizontalDatum(tokenizer); ;
                    break;
                case "PRIMEM":
                    returnObject = ReadPrimeMeridian(tokenizer);
                    break;
                //case "VERT_CS": 
                //    IVerticalCoordinateSystem verticalCS = ReadVerticalCoordinateSystem(tokenizer); 
                //    returnObject = verticalCS; 
                //    break; 
                case "GEOGCS":
                    returnObject = ReadGeographicCoordinateSystem(tokenizer);
                    break;
                case "PROJCS":
                    returnObject = ReadProjectedCoordinateSystem(tokenizer);
                    break;
                //case "COMPD_CS": 
                //    ICompoundCoordinateSystem compoundCS = ReadCompoundCoordinateSystem(tokenizer); 
                //    returnObject = compoundCS; 
                //    break; 
                case "GEOCCS":
                case "FITTED_CS":
                case "LOCAL_CS":
                    throw new NotSupportedException(String.Format("{0} is not implemented.", objectName));
                default:
                    throw new ArgumentException(String.Format("'{0'} is not recongnized.", objectName));

            }
            reader.Close();
            return returnObject;
        }

        /// <summary> 
        /// Returns a IUnit given a piece of WKT. 
        /// </summary> 
        /// <param name="tokenizer">WktStreamTokenizer that has the WKT.</param> 
        /// <returns>An object that implements the IUnit interface.</returns> 
        private static GeoAPI.CoordinateSystems.IUnit ReadUnit(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            //UNIT["degree",0.01745329251994433,AUTHORITY["EPSG","9102"]] 
            GeoAPI.CoordinateSystems.IUnit unit = null;
            tokenizer.ReadToken("[");
            string unitName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double unitsPerUnit = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");
            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.ReadAuthority(ref authority, ref authorityCode);
            tokenizer.ReadToken("]");
            switch (unitName)
            {
                // take into account the different spellings of the word meter/metre. 
                case "meter":
                case "metre":
                    unit = new LinearUnit(unitsPerUnit, String.Empty, authority, authorityCode, unitName, String.Empty, String.Empty);
                    break;
                case "degree":
                case "radian":
                    unit = new AngularUnit(unitsPerUnit, String.Empty, authority, authorityCode, unitName, String.Empty, String.Empty);
                    break;
                default:
                    throw new NotImplementedException(String.Format("{0} is not recognized is a unit of measure.", unitName));
            }
            return unit;
        }

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="coordinateSystem"></param> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.ICoordinateSystem ReadCoordinateSystem(string coordinateSystem, 
            GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            GeoAPI.CoordinateSystems.ICoordinateSystem returnCS = null;
            switch (coordinateSystem)
            {
                /*case "VERT_CS": 
                    IVerticalCoordinateSystem verticalCS = ReadVerticalCoordinateSystem(tokenizer); 
                    returnCS = verticalCS; 
                    break;*/
                case "GEOGCS":
                    GeoAPI.CoordinateSystems.IGeographicCoordinateSystem geographicCS = ReadGeographicCoordinateSystem(tokenizer);
                    returnCS = geographicCS;
                    break;
                case "PROJCS":
                    GeoAPI.CoordinateSystems.IProjectedCoordinateSystem projectedCS = ReadProjectedCoordinateSystem(tokenizer);
                    returnCS = projectedCS;
                    break;
                case "COMPD_CS":
                /*	ICompoundCoordinateSystem compoundCS = ReadCompoundCoordinateSystem(tokenizer); 
                    returnCS = compoundCS; 
                    break;*/
                case "GEOCCS":
                case "FITTED_CS":
                case "LOCAL_CS":
                    throw new InvalidOperationException(String.Format("{0} coordinate system is not recongized.", coordinateSystem));
            }
            return returnCS;
        }

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.Wgs84ConversionInfo ReadWGS84ConversionInfo(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            //TOWGS84[0,0,0,0,0,0,0] 
            tokenizer.ReadToken("[");
            GeoAPI.CoordinateSystems.Wgs84ConversionInfo info = new GeoAPI.CoordinateSystems.Wgs84ConversionInfo();
            tokenizer.NextToken();
            info.Dx = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Dy = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Dz = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Ex = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Ey = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Ez = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            tokenizer.NextToken();
            info.Ppm = tokenizer.GetNumericValue();

            tokenizer.ReadToken("]");
            return info;
        }


        /* 
        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static ICompoundCoordinateSystem ReadCompoundCoordinateSystem(WktStreamTokenizer tokenizer) 
        { 
			 
            //COMPD_CS[ 
            //"OSGB36 / British National Grid + ODN", 
            //PROJCS[] 
            //VERT_CS[] 
            //AUTHORITY["EPSG","7405"] 
            //] 
 
            //TODO add a ReadCoordinateSystem - that determines the correct coordinate system to  
            //read. Right now this hard coded for a projected and a vertical coord sys - so the UK 
            //national grid works. 
            tokenizer.ReadToken("["); 
            string name=tokenizer.ReadDoubleQuotedWord(); 
            tokenizer.ReadToken(","); 
            tokenizer.NextToken(); 
            string headCSCode =  tokenizer.GetStringValue(); 
            ICoordinateSystem headCS = ReadCoordinateSystem(headCSCode,tokenizer); 
            tokenizer.ReadToken(","); 
            tokenizer.NextToken(); 
            string tailCSCode =  tokenizer.GetStringValue(); 
            ICoordinateSystem tailCS = ReadCoordinateSystem(tailCSCode,tokenizer); 
            tokenizer.ReadToken(","); 
            string authority=String.Empty; 
            string authorityCode=String.Empty;  
            tokenizer.ReadAuthority(ref authority, ref authorityCode); 
            tokenizer.ReadToken("]"); 
            ICompoundCoordinateSystem compoundCS = new CompoundCoordinateSystem(headCS,tailCS,String.Empty,authority,authorityCode,name,String.Empty,String.Empty);  
            return compoundCS; 
			 
        }*/

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.IEllipsoid ReadEllipsoid(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            //SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]] 
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double majorAxis = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double e = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");

            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.ReadAuthority(ref authority, ref authorityCode);
            tokenizer.ReadToken("]");
            GeoAPI.CoordinateSystems.IEllipsoid ellipsoid = new Ellipsoid(majorAxis, 0.0, e, true, LinearUnit.Metre, name, authority, authorityCode, String.Empty, string.Empty, string.Empty);
            return ellipsoid;
        }

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.IProjection ReadProjection(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            //tokenizer.NextToken();// PROJECTION 
            tokenizer.ReadToken("PROJECTION");
            tokenizer.ReadToken("[");//[ 
            string projectionName = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken("]");//] 
            tokenizer.ReadToken(",");//, 
            tokenizer.ReadToken("PARAMETER");
            List<GeoAPI.CoordinateSystems.ProjectionParameter> paramList = new List<GeoAPI.CoordinateSystems.ProjectionParameter>();
            while (tokenizer.GetStringValue() == "PARAMETER")
            {
                tokenizer.ReadToken("[");
                string paramName = tokenizer.ReadDoubleQuotedWord();
                tokenizer.ReadToken(",");
                tokenizer.NextToken();
                double paramValue = tokenizer.GetNumericValue();
                tokenizer.ReadToken("]");
                tokenizer.ReadToken(",");
                paramList.Add(new GeoAPI.CoordinateSystems.ProjectionParameter(paramName, paramValue));
                tokenizer.NextToken();
            }

            string authority = String.Empty;
            long authorityCode = -1;
            GeoAPI.CoordinateSystems.IProjection projection = new Projection(projectionName, paramList, String.Empty, authority, authorityCode, String.Empty, String.Empty, string.Empty);
            return projection;
        }

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.IProjectedCoordinateSystem ReadProjectedCoordinateSystem(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            /*			PROJCS[ 
                "OSGB 1936 / British National Grid", 
                GEOGCS[ 
                    "OSGB 1936", 
                    DATUM[...] 
                    PRIMEM[...] 
                    AXIS["Geodetic latitude","NORTH"] 
                    AXIS["Geodetic longitude","EAST"] 
                    AUTHORITY["EPSG","4277"] 
                ], 
                PROJECTION["Transverse Mercator"], 
                PARAMETER["latitude_of_natural_origin",49], 
                PARAMETER["longitude_of_natural_origin",-2], 
                PARAMETER["scale_factor_at_natural_origin",0.999601272], 
                PARAMETER["false_easting",400000], 
                PARAMETER["false_northing",-100000], 
                AXIS["Easting","EAST"], 
                AXIS["Northing","NORTH"], 
                AUTHORITY["EPSG","27700"] 
            ] 
            */
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("GEOGCS");
            GeoAPI.CoordinateSystems.IGeographicCoordinateSystem geographicCS = ReadGeographicCoordinateSystem(tokenizer);
            tokenizer.ReadToken(",");
            GeoAPI.CoordinateSystems.IProjection projection = ReadProjection(tokenizer);
            GeoAPI.CoordinateSystems.IUnit unit = ReadUnit(tokenizer);

            tokenizer.ReadToken(",");
            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.ReadAuthority(ref authority, ref authorityCode);
            tokenizer.ReadToken("]");
            List<GeoAPI.CoordinateSystems.AxisInfo> axes = new List<GeoAPI.CoordinateSystems.AxisInfo>();
            GeoAPI.CoordinateSystems.IProjectedCoordinateSystem projectedCS = new ProjectedCoordinateSystem(geographicCS.HorizontalDatum, geographicCS, unit as LinearUnit, projection, axes, name, authority, authorityCode, String.Empty, String.Empty, String.Empty);
            return projectedCS;
        }

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.IGeographicCoordinateSystem ReadGeographicCoordinateSystem(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            /* 
            GEOGCS["OSGB 1936", 
            DATUM["OSGB 1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]]TOWGS84[0,0,0,0,0,0,0],AUTHORITY["EPSG","6277"]] 
            PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]] 
            AXIS["Geodetic latitude","NORTH"] 
            AXIS["Geodetic longitude","EAST"] 
            AUTHORITY["EPSG","4277"] 
            ] 
            */
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("DATUM");
            GeoAPI.CoordinateSystems.IHorizontalDatum horizontalDatum = ReadHorizontalDatum(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("PRIMEM");
            GeoAPI.CoordinateSystems.IPrimeMeridian primeMeridian = ReadPrimeMeridian(tokenizer);
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("UNIT");
            GeoAPI.CoordinateSystems.IUnit angularUnit = ReadUnit(tokenizer);
            tokenizer.ReadToken(",");

            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.ReadAuthority(ref authority, ref authorityCode);
            tokenizer.ReadToken("]");
            GeoAPI.CoordinateSystems.IGeographicCoordinateSystem geographicCS = new GeographicCoordinateSystem(angularUnit as GeoAPI.CoordinateSystems.IAngularUnit, horizontalDatum,
                    primeMeridian, new List<GeoAPI.CoordinateSystems.AxisInfo>(), name, authority, authorityCode, String.Empty, String.Empty, String.Empty);
            return geographicCS;
        }

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.IHorizontalDatum ReadHorizontalDatum(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            //DATUM["OSGB 1936",SPHEROID["Airy 1830",6377563.396,299.3249646,AUTHORITY["EPSG","7001"]]TOWGS84[0,0,0,0,0,0,0],AUTHORITY["EPSG","6277"]] 

            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.ReadToken("SPHEROID");
            GeoAPI.CoordinateSystems.IEllipsoid ellipsoid = ReadEllipsoid(tokenizer);
            tokenizer.ReadToken(",");
            GeoAPI.CoordinateSystems.Wgs84ConversionInfo wgsInfo = new GeoAPI.CoordinateSystems.Wgs84ConversionInfo();
            if (tokenizer.GetStringValue() == "TOWGS84")
            {
                tokenizer.ReadToken("TOWGS84");
                wgsInfo = ReadWGS84ConversionInfo(tokenizer);
                tokenizer.ReadToken(",");
            }
            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.ReadAuthority(ref authority, ref authorityCode);
            // make an assumption about the datum type. 
            GeoAPI.CoordinateSystems.IHorizontalDatum horizontalDatum = new HorizontalDatum(ellipsoid, wgsInfo, GeoAPI.CoordinateSystems.DatumType.HD_Geocentric, name, authority, authorityCode, String.Empty, String.Empty, String.Empty);
            tokenizer.ReadToken("]");
            return horizontalDatum;
        }

        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static GeoAPI.CoordinateSystems.IPrimeMeridian ReadPrimeMeridian(GisSharpBlog.NetTopologySuite.IO.WktStreamTokenizer tokenizer)
        {
            //PRIMEM["Greenwich",0,AUTHORITY["EPSG","8901"]] 
            tokenizer.ReadToken("[");
            string name = tokenizer.ReadDoubleQuotedWord();
            tokenizer.ReadToken(",");
            tokenizer.NextToken();
            double longitude = tokenizer.GetNumericValue();
            tokenizer.ReadToken(",");
            string authority = String.Empty;
            long authorityCode = -1;
            tokenizer.ReadAuthority(ref authority, ref authorityCode);
            // make an assumption about the Angular units - degrees. 
            GeoAPI.CoordinateSystems.IPrimeMeridian primeMeridian = new PrimeMeridian(longitude, new AngularUnit(180 / Math.PI), name, authority, authorityCode, String.Empty, String.Empty, String.Empty);
            tokenizer.ReadToken("]");
            return primeMeridian;
        }
        /* 
        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static IVerticalCoordinateSystem ReadVerticalCoordinateSystem(WktStreamTokenizer tokenizer) 
        { 
            //VERT_CS["Newlyn", 
            //VERT_DATUM["Ordnance Datum Newlyn",2005,AUTHORITY["EPSG","5101"]] 
            //UNIT["metre",1,AUTHORITY["EPSG","9001"]] 
            //AUTHORITY["EPSG","5701"] 
			 
            tokenizer.ReadToken("["); 
            string name=tokenizer.ReadDoubleQuotedWord(); 
            tokenizer.ReadToken(","); 
            tokenizer.ReadToken("VERT_DATUM"); 
            IVerticalDatum verticalDatum = ReadVerticalDatum(tokenizer); 
            tokenizer.ReadToken("UNIT"); 
            IUnit unit = ReadUnit(tokenizer); 
            string authority=String.Empty; 
            string authorityCode=String.Empty;  
            tokenizer.ReadAuthority(ref authority, ref authorityCode); 
            tokenizer.ReadToken("]"); 
 
            IVerticalCoordinateSystem verticalCS = new VerticalCoordinateSystem(name,verticalDatum,String.Empty,authority,authorityCode,String.Empty,String.Empty); 
            return verticalCS; 
        }*/

        /* 
        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        private static IVerticalDatum  ReadVerticalDatum(WktStreamTokenizer tokenizer) 
        { 
            //VERT_DATUM["Ordnance Datum Newlyn",2005,AUTHORITY["5101","EPSG"]] 
            tokenizer.ReadToken("["); 
            string datumName=tokenizer.ReadDoubleQuotedWord(); 
            tokenizer.ReadToken(","); 
            tokenizer.NextToken(); 
            string datumTypeNumber = tokenizer.GetStringValue(); 
            tokenizer.ReadToken(","); 
            string authority=String.Empty; 
            string authorityCode=String.Empty;  
            tokenizer.ReadAuthority(ref authority, ref authorityCode); 
            DatumType datumType = (DatumType)Enum.Parse(typeof(DatumType),datumTypeNumber); 
            IVerticalDatum verticalDatum = new VerticalDatum(datumType,String.Empty,authorityCode,authority,datumName,String.Empty,String.Empty); 
            tokenizer.ReadToken("]"); 
            return verticalDatum; 
        }*/


        /* 
        /// <summary> 
        ///  
        /// </summary> 
        /// <param name="tokenizer"></param> 
        /// <returns></returns> 
        [Obsolete("Since the related objects have not been implemented")] 
        private static IGeocentricCoordinateSystem ReadGeocentricCoordinateSystem(WktStreamTokenizer tokenizer) 
        { 
            throw new NotImplementedException("IGeocentricCoordinateSystem is not implemented"); 
        }*/
    } 
}
