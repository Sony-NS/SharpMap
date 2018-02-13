﻿using System;

namespace GeoAPI.Extensions.Feature
{
    /// <summary>
    /// Attribute to be used for properties which need to be declared as Feature Attributes.
    /// </summary>
    public class FeatureAttributeAttribute: Attribute
    {
        /// <summary>
        /// Name as used for table columns
        /// </summary>
        public string DisplayName { get; set; }
    }
}
