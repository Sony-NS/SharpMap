﻿using System.Linq;
using System.Threading;
using System.Windows.Forms;
using DelftTools.TestUtils;
using GisSharpBlog.NetTopologySuite.Geometries;
using NUnit.Framework;
using SharpMap.Data.Providers;
using SharpMap.Extensions.Layers;
using SharpMap.Layers;
using SharpMap.UI.Forms;
using SharpMap.UI.Tools.Zooming;

namespace SharpMap.UI.Tests.Forms
{
    [TestFixture]
    public class MapControlTest
    {
        [SetUp]
        public void SetUp()
        {
            LogHelper.ConfigureLogging();
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void DisablingLayerShouldRefreshMapControlOnce()
        {
            var mapControl = new MapControl();
            WindowsFormsTestHelper.Show(mapControl);

            mapControl.Map.Layers.Add(new GroupLayer("group1"));

            while (mapControl.IsProcessing)
            {
                Application.DoEvents();
            }

            var refreshCount = 0;
            mapControl.MapRefreshed += delegate
                                           {
                                               refreshCount++;
                                           };

            
            mapControl.Map.Layers.First().Visible = false;
            
            while (mapControl.IsProcessing)
            {
                Application.DoEvents();
            }
            
            refreshCount.Should("map should be refreshed once when layer property changes").Be.EqualTo(1);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void PanZoomUsingMouseWheelTest()
        {
            var map = new Map();

            var layer = new OpenStreetMapLayer();
            map.Layers.Add(layer);

            var mapControl = new MapControl { Map = map };

            var tool = new PanZoomUsingMouseWheelTool(mapControl);
            mapControl.ActivateTool(tool);
           
            WindowsFormsTestHelper.ShowModal(mapControl);
        }

        [Test]
        public void SelectToolIsActiveByDefault()
        {
            var mapControl = new MapControl();

            Assert.IsTrue(mapControl.SelectTool.IsActive);
        }

        [Test]
        [Category(TestCategory.WindowsForms)]
        [Category(TestCategory.WorkInProgress)] // slow
        public void ChangingEnvelopeAfterMapControlIsShownWorksCorrectly()
        {
            var layer = new VectorLayer { DataSource = new DataTableFeatureProvider("POINT(1 1)") };
            var map = new Map { Layers = { layer } };

            var mapControl = new MapControl { Map = map };

            var viewEnvelope = new Envelope(10000, 10010, 10000, 10010);

            WindowsFormsTestHelper.Show(mapControl,
                delegate
                    {
                        map.ZoomToFit(viewEnvelope);
                    });

            for (var i = 0; i < 10; i++)
            {
                Application.DoEvents();
                Thread.Sleep(100);
            }

            Assert.IsTrue(map.Envelope.Contains(viewEnvelope));
        }
    }
}
