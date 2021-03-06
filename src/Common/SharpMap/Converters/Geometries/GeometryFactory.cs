/*
 * Erstellt mit SharpDevelop.
 * Benutzer: Christian
 * Datum: 20.11.2007
 * Zeit: 21:38
 * 
 * Sie k�nnen diese Vorlage unter Extras > Optionen > Codeerstellung > Standardheader �ndern.
 */

using System;
using NTS = GisSharpBlog.NetTopologySuite;
using GeoAPI.Geometries;

namespace SharpMap.Converters.Geometries
{
	/// <summary>
	/// Description of GeometryFactory.
	/// </summary>
	// TODO: remove this, inject it using NTS, DON'T USE IT DIRECTLY
    [Obsolete]
	public class GeometryFactory
	{
		private static NTS.Geometries.GeometryFactory geomFactory = new NTS.Geometries.GeometryFactory();

	    public static ICoordinate CreateCoordinate(double x, double y)
        {
            // use 0.0 as default for z
            return new NTS.Geometries.Coordinate(x, y, 0.0);
        }

	    public static IPoint CreatePoint(double x, double y)
        {
            return geomFactory.CreatePoint(new NTS.Geometries.Coordinate(x, y, 0.0));
        }

        public static IPoint CreatePoint(ICoordinate coord)
        {
            return geomFactory.CreatePoint(coord);
        }
		
		public static IMultiPoint CreateMultiPoint(IPoint[] points)
		{
			return geomFactory.CreateMultiPoint(points);
		}

        public static IEnvelope CreateEnvelope(double minx, double maxx, double miny, double maxy)
		{
			return new NTS.Geometries.Envelope(minx, maxx, miny, maxy);
		}
		
		public static IEnvelope CreateEnvelope()
		{
			return new NTS.Geometries.Envelope();
		}

		public static ILineString CreateLineString(ICoordinate[] coords)
		{
			return geomFactory.CreateLineString(coords);
		}		
		
		public static IMultiLineString CreateMultiLineString(ILineString[] lineStrings)
		{
			return geomFactory.CreateMultiLineString(lineStrings);
		}		
		
		public static ILinearRing CreateLinearRing(ICoordinate[] coords)
		{
			return geomFactory.CreateLinearRing(coords);
		}		

		public static IPolygon CreatePolygon(ILinearRing shell, ILinearRing[] holes)
		{
			return geomFactory.CreatePolygon(shell, holes);
		}		
		
		public static IMultiPolygon CreateMultiPolygon(IPolygon[] polygons)
		{
			return geomFactory.CreateMultiPolygon(polygons);
		}			
		public static IMultiPolygon CreateMultiPolygon()
		{
			return geomFactory.CreateMultiPolygon(null);
		}	
		
		public static IGeometryCollection CreateGeometryCollection(IGeometry[] geometries)
		{
			return geomFactory.CreateGeometryCollection(geometries);
		}		
		public static IGeometryCollection CreateGeometryCollection()
		{
			return geomFactory.CreateGeometryCollection(null);
		}		
		
		public static bool IsCCW(ICoordinate[] ring)
		{
			return NTS.Algorithm.CGAlgorithms.IsCCW(ring);
		}

        
	}
}
