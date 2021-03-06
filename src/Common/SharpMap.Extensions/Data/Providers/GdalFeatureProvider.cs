using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DelftTools.Utils.Data;
using DelftTools.Utils.IO;
using GeoAPI.Extensions.Coverages;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using log4net;
using NetTopologySuite.Extensions.Coverages;
using OSGeo.GDAL;
using SharpMap.Data.Providers;

namespace SharpMap.Extensions.Data.Providers
{
    /// <summary>
    /// Works as a proxy to GdalFunctionStore. 
    /// 
    /// Returns 1st function of the GdalFunctionStore as a feature.
    /// </summary>
    public class GdalFeatureProvider : Unique<long>, IFeatureProvider, IFileBased
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(GdalFeatureProvider));
        
        private GdalFunctionStore store;

        private GdalFunctionStore Store
        {
            get
            {
                if (store == null)
                {
                    store = new GdalFunctionStore();
                }

                return store;
            }
        }

        public virtual IRegularGridCoverage Grid
        {
            get { return Store.Grid; }
        }
        
        public virtual Type FeatureType
        {
            get { return typeof(RegularGridCoverage); }
        }

        public virtual IList Features
        {
            get { return (IList) Store.Functions; }
            set { throw new System.NotImplementedException(); }
        }

        public virtual IFeature Add(IGeometry geometry)
        {
            throw new System.NotImplementedException();
        }

        public virtual Func<IFeatureProvider, IGeometry, IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox)
        {
            return GetGeometriesInView(bbox, -1);
        }

        public virtual ICollection<IGeometry> GetGeometriesInView(IEnvelope bbox, double minGeometrySize)
        {
            return null;
        }

        public virtual ICollection<int> GetObjectIDsInView(IEnvelope envelope)
        {
            return null;
        }

        public virtual IGeometry GetGeometryByID(int oid)
        {
            return Grid.Geometry;
        }

        public virtual void SwitchTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public virtual void Delete()
        {
            File.Delete(Path);
        }

        public virtual void CopyTo(string newPath)
        {
            throw new NotImplementedException();
        }

        public virtual IList GetFeatures(IGeometry geom)
        {
            IList result = new ArrayList();

            if (Grid.Geometry.Intersects(geom))
            {
                result.Add(Grid);
            }

            return result;
        }

        public virtual IList GetFeatures(IEnvelope box)
        {
            IList result = new ArrayList();

            if (Grid.Geometry.EnvelopeInternal.Intersects(box))
            {
                result.Add(Grid);
            }

            return result;
        }

        public virtual int GetFeatureCount()
        {
            return 1;
        }

        public virtual IFeature GetFeature(int index)
        {
            if(index != 0)
            {
                throw new InvalidOperationException("Feature provider contains only a single feaure (grid)");
            }

            return Grid;
        }

        public virtual bool Contains(IFeature feature)
        {
            // Only true if the grid itself is searched for
            return Grid.Equals(feature);
        }

        public virtual int IndexOf(IFeature feature)
        {
            // The grid is the only feature, which is 'at position 0' (the first element)
            return 0;
        }

        public virtual IEnvelope GetExtents()
        {
            return Store.GetExtents();
        }

        public virtual string Path
        {
            get { return Store.Path; }
            set { Store.Path = value; }
        }

        public virtual void CreateNew(string path)
        {
            Store.CreateNew(path);
        }

        public virtual void Close()
        {
            Store.Close();
        }

        public virtual void Open(string path)
        {
            Store.Open(path);
        }

        public virtual bool IsOpen
        {
            get { return Store.IsOpen; }
        }

        public virtual int SRID { get; set; }

        public virtual Dataset GdalDataset
        {
            get { return Store.GdalDataset; }
        }

        public virtual void Dispose()
        {
        }

    }
}
