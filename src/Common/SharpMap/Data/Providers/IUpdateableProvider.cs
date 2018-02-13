using System;
using System.Collections.Generic;
using System.Text;
using SharpMap.Converters.Geometries;

namespace SharpMap.Data.Providers
{
    public interface IUpdateableProvider : IFeatureProvider
    {
        void Save(FeatureDataTable features);
        void Save(FeatureDataRow feature);
        void Delete(FeatureDataRow feature);
    }
}
