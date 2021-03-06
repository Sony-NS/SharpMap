using System.IO;
using DelftTools.TestUtils;
using NUnit.Framework;
using SharpMap.Data.Providers;
using GeoAPI.Extensions.Feature;

namespace SharpMap.Tests.Data.Providers
{
    [TestFixture]
    public class ShapeFileTests
    {
        // TODO: write tests for shapefile

        [Test, Category(TestCategory.DataAccess)]
        public void ContainsShouldWorkForShapeFile()
        {
            string path = TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTests, "Europe_Lakes.shp");
            var s = new ShapeFile(path);
            var feature = s.Features[0];
            s.Contains((IFeature)feature); // -> should not throw an exception
        }

        [Test, Category(TestCategory.DataAccess)]
        [Ignore("Should we drop the ShapeFile class and continue with the OgrFeatureProvider?")]
        public void GetFeatureShouldWorkForShapeFile()
        {
            string path = @"..\..\..\..\data\Europe_Lakes.shp";
            var s = new ShapeFile(path);
            var feature = s.Features[1];
            Assert.LessOrEqual(0, s.IndexOf((IFeature)feature));
        }

        [Test, Category(TestCategory.DataAccess)]
        [Ignore("Should we drop the ShapeFile class and continue with the OgrFeatureProvider?")]
        public void GetFeatureShouldWorkForShapeFileWithoutObjectID()
        {
            string path = @"..\..\..\..\data\gemeenten.shp";
            var s = new ShapeFile(path);
            var feature = s.Features[0];
            Assert.LessOrEqual(0, s.IndexOf((IFeature)feature));
        }


        [Test]
        [Category(TestCategory.DataAccess)]
        public void FeatureCount()
        {
            string path = TestHelper.GetTestDataPath(TestDataPath.DeltaShell.DeltaShellDeltaShellPluginsSharpMapGisTests, "Europe_Lakes.shp");
            IFeatureProvider dataSource = new ShapeFile(path);
            Assert.AreEqual(37, dataSource.Features.Count);

            Assert.IsTrue(dataSource.Features.Count == dataSource.GetFeatures(dataSource.GetExtents()).Count);
        }

    }

}
