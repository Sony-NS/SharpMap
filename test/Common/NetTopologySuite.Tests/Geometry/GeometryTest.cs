﻿using System;
using DelftTools.TestUtils;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Algorithm;
using GisSharpBlog.NetTopologySuite.CoordinateSystems.Transformations;
using GisSharpBlog.NetTopologySuite.Geometries;
using GisSharpBlog.NetTopologySuite.IO;
using GisSharpBlog.NetTopologySuite.Operation.Overlay;
using GisSharpBlog.NetTopologySuite.Operation.Overlay.Snap;
using NUnit.Framework;
using System.Linq;

namespace NetTopologySuite.Tests.Geometry
{
    [TestFixture]
    public class GeometryTest
    {
        [Test]
        public void DifferenceDoesNotCrashForCertainPolygons()
        {
            //TEST demonstrates problem in NTS 1.7.1 was the reason for upgrade to 1.7.3
            var g1 = new WKTReader().Read("POLYGON ((5 -10, 2 -10, 0 0, 0 1, 5 1, 5 -10))");
            var g2 = new WKTReader().Read("POLYGON ((0.6 -3, 2.6 -3, 3.4 -7, 1.4 -7, 0.6 -3))");

            //this used to crash..
            g1.Difference(g2);
        }

        [Test]
        public void DifferenceProblemForOtherPolygons()
        {
            
            var wktReader = new WKTReader(new GeometryFactory(new PrecisionModel(1000)));
            var g1 = wktReader.Read("POLYGON((0.0000001 -3,5 -3,5 -7,0.000001 -7,0.0000001 -3))");
            var g2 = wktReader.Read("POLYGON((5 -10,2 -10,0.000001 0,0.000001 1,5 1,5 -10))");
            
            var diff  = g1.Difference(g2);
        }

        /// <summary>
        /// Crashes with default precision model.
        /// </summary>
        [Test]
        public void IntersectTwoLines()
        {
            var wktReader = new WKTReader(new GeometryFactory(new PrecisionModel(PrecisionModel.MaximumPreciseValue)));
            var g1 = wktReader.Read("LINESTRING(280 0.01, 285 -0.07)");
            var g2 = wktReader.Read("LINESTRING(-900.0 0, 1520.0 0)");
            
            var intersection = g1.Intersection(g2);

             Assert.AreEqual(1, intersection.Coordinates.Count());
        }

        [Test]
        [Category(TestCategory.Performance)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void GetHashCodeShouldBeComputedLazyAndShouldBeVeryFast()
        {
            var geometryCount = 1000000;
            var geometries = new IGeometry[geometryCount];

            for (int i = 0; i < geometryCount; i++)
            {
                geometries[i] = new Polygon(new LinearRing(new[] { new Coordinate(1.0, 2.0), new Coordinate(2.0, 3.0), new Coordinate(3.0, 4.0), new Coordinate(1.0, 2.0) }));
            }

            var polygon = new Polygon(new LinearRing(new[] { new Coordinate(1.0, 2.0), new Coordinate(2.0, 3.0), new Coordinate(3.0, 4.0), new Coordinate(1.0, 2.0) }));

            // computes hash code every call
            var t0 = DateTime.Now;
            for (int i = 0; i < geometryCount; i++)
            {
                geometries[i].GetHashCode();
            }
            var t1 = DateTime.Now;

            var dt1 = t1 - t0;

            // computes hash code only first time (lazy)
            t0 = DateTime.Now;
            for (int i = 0; i < geometryCount; i++)
            {
                polygon.GetHashCode();
            }
            t1 = DateTime.Now;

            var dt2 = t1 - t0;

            Assert.IsTrue(dt2.TotalMilliseconds < 15 * dt1.TotalMilliseconds);
        }

        [Test]
        public void GetHashCodeTakesYIntoAccount()
        {
            var point = new Point(1, 2);

            var c1 = point.GetHashCode();

            point.Y = 3;
            Assert.AreNotEqual(c1, point.GetHashCode());
        }

        [Test]
        public void GeometryTransformScaleTest()
        {
            var wktReader = new WKTReader(new GeometryFactory(new PrecisionModel(1000)));
            var geometry = wktReader.Read("POLYGON((0 -3,5 -3,5 -7,0 -7,0 -3))");
            
            var scale = 5.0;
            
            var scaledGeometry = SharpMap.CoordinateSystems.Transformations.GeometryTransform.Scale(geometry, scale);

            Assert.AreEqual(scaledGeometry.Coordinates.Length,geometry.Coordinates.Length);
            Assert.AreEqual(scaledGeometry.Centroid, geometry.Centroid);

            Assert.AreEqual(-10.0, scaledGeometry.Coordinates[0].X);
            Assert.AreEqual(5.0, scaledGeometry.Coordinates[0].Y);
            Assert.AreEqual(15.0, scaledGeometry.Coordinates[1].X);
            Assert.AreEqual(5.0, scaledGeometry.Coordinates[1].Y);
            Assert.AreEqual(15.0, scaledGeometry.Coordinates[2].X);
            Assert.AreEqual(-15.0, scaledGeometry.Coordinates[2].Y);
            Assert.AreEqual(-10.0, scaledGeometry.Coordinates[3].X);
            Assert.AreEqual(-15.0, scaledGeometry.Coordinates[3].Y);
            Assert.AreEqual(-10.0, scaledGeometry.Coordinates[4].X);
            Assert.AreEqual(5.0, scaledGeometry.Coordinates[4].Y);
        }
    }
}