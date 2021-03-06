﻿using System;
using DelftTools.Controls.Swf.Charting;
using NUnit.Framework;

namespace DelftTools.Tests.Controls.Swf.Charting
{
    [TestFixture]
    public class QuarterViewDateTimeFormatProviderTest
    {
        [Test]
        public void CheckQuarterViewDateTimeFormatProviderOutput()
        {
            var provider = new QuarterNavigatableLabelFormatProvider();

            var minDate = new DateTime(2001, 1, 1);
            var maxDate = new DateTime(2002, 7, 1);

            provider.GetRangeLabel(minDate, maxDate).Should("Unexpected quarter datetime string.").Be.EqualTo("1st Qtr 2001 till 3rd Qtr 2002");
        }

    }
}