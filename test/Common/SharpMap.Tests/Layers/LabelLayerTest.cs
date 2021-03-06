using System;
using System.Drawing;
using GeoAPI.Extensions.Feature;
using NetTopologySuite.Extensions.Features;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMap.Layers;
using Point = GisSharpBlog.NetTopologySuite.Geometries.Point;

namespace SharpMap.Tests.Layers
{
    [TestFixture]
    public class LabelLayerTest
    {
        [Test]
        public void SetGetText()
        {
            var map = new Map(new Size(100, 100));
            
            var feature1 = new SimpleFeature { Data = 1, Geometry = new Point(50, 50) };
            var feature2 = new SimpleFeature { Data = 5, Geometry = new Point(50, 55) };
            var featureCollection = new FeatureCollection { Features = {feature1, feature2}};

            var callCount = 0;

            var labelLayer = new LabelLayer
                                 {
                                     Visible = true,
                                     DataSource = featureCollection,
                                     Map = map,
                                     LabelStringDelegate = delegate(IFeature feature)
                                                               {
                                                                   callCount++;

                                                                   feature
                                                                       .Should().Be.OfType<SimpleFeature>();

                                                                   return ((SimpleFeature) feature).Data.ToString();
                                                               }
                                 };

            labelLayer.Render();

            callCount
                .Should("labels of 2 simple feature rendered").Be.EqualTo(2);
        }

        public class SimpleFeature : Feature
        {
            public int Data { get; set; }
        }
    }
}