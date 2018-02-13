/*
 * Created  : Sony NS 
 * Descript : DXF to shp
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace DxfFileProvider
{
    public class DxfToShp
    {
        #region const dbf file
        private const int _DoubleLength = 18;
        private const int _DoubleDecimals = 8;
        private const int _IntLength = 10;
        private const int _IntDecimals = 0;
        private const int _StringLength = 254;
        private const int _StringDecimals = 0;
        private const int _BoolLength = 1;
        private const int _BoolDecimals = 0;
        private const int _DateLength = 8;
        private const int _DateDecimals = 0;
        #endregion
        
        private GisSharpBlog.NetTopologySuite.IO.DbaseFileHeader GetColumnHeader(SharpMap.Data.FeatureDataTable table)
        {
            GisSharpBlog.NetTopologySuite.IO.DbaseFileHeader header = new GisSharpBlog.NetTopologySuite.IO.DbaseFileHeader();
            header.NumRecords = table.Rows.Count;

            foreach (System.Data.DataColumn col in table.Columns)
            {
                //Hanya kolom selain hasil generate SharpMap yang dijadikan column header  
                if (col.ColumnName != "DSHELL_ADDED_OBJECTID")
                {
                    Type type = col.DataType;
                    if (type == typeof(double) || type == typeof(float))
                        header.AddColumn(col.ColumnName, 'N', _DoubleLength, _DoubleDecimals);
                    else if (type == typeof(short) || type == typeof(ushort) ||
                             type == typeof(int) || type == typeof(uint) ||
                             type == typeof(long) || type == typeof(ulong))
                        header.AddColumn(col.ColumnName, 'N', _IntLength, _IntDecimals);
                    else if (type == typeof(string))
                        header.AddColumn(col.ColumnName, 'C', _StringLength, _StringDecimals);
                    else if (type == typeof(bool))
                        header.AddColumn(col.ColumnName, 'L', _BoolLength, _BoolDecimals);
                    else if (type == typeof(DateTime))
                        header.AddColumn(col.ColumnName, 'D', _DateLength, _DateDecimals);
                    else throw new ArgumentException("Type " + type.Name + " not supported");
                }
            }

            return header;
        }

        private void CreateShp(SharpMap.Data.FeatureDataTable table, string shpType, string path)
        {
            List<GeoAPI.Geometries.IGeometry> geometries = new List<GeoAPI.Geometries.IGeometry>();

            foreach (SharpMap.Data.FeatureDataRow feature in table)
            {
                if (feature.Geometry.GeometryType.ToUpper() == shpType)
                    geometries.Add(feature.Geometry);
            }

            //Shp
            GisSharpBlog.NetTopologySuite.IO.ShapefileWriter shapeWriter = new GisSharpBlog.NetTopologySuite.IO.ShapefileWriter();
            shapeWriter.Write(path, new GisSharpBlog.NetTopologySuite.Geometries.GeometryCollection(geometries.ToArray()));
            //Dbf
            GisSharpBlog.NetTopologySuite.IO.DbaseFileWriter dataWriter =
                new GisSharpBlog.NetTopologySuite.IO.DbaseFileWriter(path + ".dbf");
            try
            {
                //write column header
                GisSharpBlog.NetTopologySuite.IO.DbaseFileHeader clHeader = GetColumnHeader(table);
                dataWriter.Write(clHeader);
                //write data
                foreach (SharpMap.Data.FeatureDataRow feature in table)
                {
                    if (feature.Geometry.GeometryType.ToUpper() == shpType)
                    {
                        GeoAPI.Extensions.Feature.IFeatureAttributeCollection attribs = feature.Attributes;
                        System.Collections.ArrayList values = new System.Collections.ArrayList();
                        for (int i = 0; i < clHeader.NumFields; i++)
                            values.Add(attribs[clHeader.Fields[i].Name]);
                        dataWriter.Write(values);
                    }
                }
            }
            finally
            {
                dataWriter.Close();
            }
        }

        /// <summary>
        /// Create shp without file .prj 
        /// </summary>
        /// <param name="table"></param>
        /// <param name="shpType"></param>
        /// <param name="path"></param>
        public DxfToShp(SharpMap.Data.FeatureDataTable table, string shpType, string path)
        {
            CreateShp(table, shpType, path);       
        }

        /// <summary>
        /// Create shp with file .prj
        /// </summary>
        /// <param name="table"></param>
        /// <param name="shpType"></param>
        /// <param name="projection"></param>
        /// <param name="path"></param>
        public DxfToShp(SharpMap.Data.FeatureDataTable table, string shpType, string projection, string path)
        {
            CreateShp(table, shpType, path);

            using (System.IO.StreamWriter sw = System.IO.File.AppendText(path + ".prj"))
            {
                if (!string.IsNullOrEmpty(projection))
                {
                    sw.WriteLine("{0}", projection);
                }

                sw.Flush();
                sw.Close();
            }
        }
    }
}
