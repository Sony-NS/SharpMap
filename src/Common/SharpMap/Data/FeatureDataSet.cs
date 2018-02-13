// Copyright 2005, 2006 - Morten Nielsen (www.iter.dk)
//
// This file is part of SharpMap.
// SharpMap is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA 

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Xml;
using System.Runtime.Serialization;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;

namespace SharpMap.Data
{
	/// <summary>
	/// Represents an in-memory cache of spatial data. The FeatureDataSet is an extension of System.Data.DataSet
	/// </summary>
	[Serializable()]
	public class FeatureDataSet : DataSet
	{
		/// <summary>
		/// Initializes a new instance of the FeatureDataSet class.
		/// </summary>
		public FeatureDataSet()
		{
			this.InitClass();
			System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler = new System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
			//this.Tables.CollectionChanged += schemaChangedHandler;
			this.Relations.CollectionChanged += schemaChangedHandler;
			this.InitClass();
		}

		/// <summary>
		/// nitializes a new instance of the FeatureDataSet class.
		/// </summary>
		/// <param name="info">serialization info</param>
		/// <param name="context">streaming context</param>
		protected FeatureDataSet(SerializationInfo info, StreamingContext context)
		{
			string strSchema = ((string)(info.GetValue("XmlSchema", typeof(string))));
			if ((strSchema != null))
			{
				DataSet ds = new DataSet();
				ds.ReadXmlSchema(new XmlTextReader(new System.IO.StringReader(strSchema)));
				if ((ds.Tables["FeatureTable"] != null))
				{
					this.Tables.Add(new FeatureDataTable(ds.Tables["FeatureTable"]));
				}
				this.DataSetName = ds.DataSetName;
				this.Prefix = ds.Prefix;
				this.Namespace = ds.Namespace;
				this.Locale = ds.Locale;
				this.CaseSensitive = ds.CaseSensitive;
				this.EnforceConstraints = ds.EnforceConstraints;
				this.Merge(ds, false, System.Data.MissingSchemaAction.Add);
			}
			else
			{
				this.InitClass();
			}
			this.GetSerializationData(info, context);
			System.ComponentModel.CollectionChangeEventHandler schemaChangedHandler = new System.ComponentModel.CollectionChangeEventHandler(this.SchemaChanged);
			//this.Tables.CollectionChanged += schemaChangedHandler;
			this.Relations.CollectionChanged += schemaChangedHandler;
		}

		private FeatureTableCollection _FeatureTables;

		/// <summary>
		/// Gets the collection of tables contained in the FeatureDataSet
		/// </summary>
		public new FeatureTableCollection Tables
		{
			get
			{
				return _FeatureTables;
			}
		}

		/// <summary>
		/// Copies the structure of the FeatureDataSet, including all FeatureDataTable schemas, relations, and constraints. Does not copy any data. 
		/// </summary>
		/// <returns></returns>
		public new FeatureDataSet Clone()
		{
			FeatureDataSet cln = ((FeatureDataSet)(base.Clone()));
			return cln;
		}

		/// <summary>
		/// Gets a value indicating whether Tables property should be persisted.
		/// </summary>
		/// <returns></returns>
		protected override bool ShouldSerializeTables()
		{
			return false;
		}

		/// <summary>
		/// Gets a value indicating whether Relations property should be persisted.
		/// </summary>
		/// <returns></returns>
		protected override bool ShouldSerializeRelations()
		{
			return false;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="reader"></param>
		protected override void ReadXmlSerializable(XmlReader reader)
		{
			this.Reset();
			DataSet ds = new DataSet();
			ds.ReadXml(reader);
			//if ((ds.Tables["FeatureTable"] != null))
			//{
			//    this.Tables.Add(new FeatureDataTable(ds.Tables["FeatureTable"]));
			//}
			this.DataSetName = ds.DataSetName;
			this.Prefix = ds.Prefix;
			this.Namespace = ds.Namespace;
			this.Locale = ds.Locale;
			this.CaseSensitive = ds.CaseSensitive;
			this.EnforceConstraints = ds.EnforceConstraints;
			this.Merge(ds, false, System.Data.MissingSchemaAction.Add);
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override System.Xml.Schema.XmlSchema GetSchemaSerializable()
		{
			System.IO.MemoryStream stream = new System.IO.MemoryStream();
			this.WriteXmlSchema(new XmlTextWriter(stream, null));
			stream.Position = 0;
			return System.Xml.Schema.XmlSchema.Read(new XmlTextReader(stream), null);
		}


		private void InitClass()
		{
			_FeatureTables = new FeatureTableCollection();
			//this.DataSetName = "FeatureDataSet";
			this.Prefix = "";
			this.Namespace = "http://tempuri.org/FeatureDataSet.xsd";
			this.Locale = new System.Globalization.CultureInfo("en-US");
			this.CaseSensitive = false;
			this.EnforceConstraints = true;
		}

		private bool ShouldSerializeFeatureTable()
		{
			return false;
		}

		private void SchemaChanged(object sender, System.ComponentModel.CollectionChangeEventArgs e)
		{
			if ((e.Action == System.ComponentModel.CollectionChangeAction.Remove))
			{
				//this.InitVars();
			}
		}
	}

	/// <summary>
	/// Represents the method that will handle the RowChanging, RowChanged, RowDeleting, and RowDeleted events of a FeatureDataTable. 
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="e"></param>
	public delegate void FeatureDataRowChangeEventHandler(object sender, FeatureDataRowChangeEventArgs e);

    /// NS 15-03-2013
    /// 
    public class FeatureDataTable<TOid> : FeatureDataTable, System.Collections.Generic.IEnumerable<FeatureDataRow<TOid>>
    {
        private DataColumn _idColumn;

        public static FeatureDataTable<TOid> CreateEmpty(string idColumnName)
        {
            return CreateTableWithId(new FeatureDataTable(), idColumnName);
        }

        private FeatureDataTable()
            : base()
        {
        }

        public FeatureDataTable(string idColumnName)
            : base()
        {
            setIdColumn(idColumnName);
        }

        public FeatureDataTable(DataTable table, string idColumnName)
            : base(table)
        {
            setIdColumn(idColumnName);
        }

        public static FeatureDataTable<TOid> CreateTableWithId(FeatureDataTable table, string columnName)
        {
            if (table == null)
                throw new ArgumentNullException("table");

            if (columnName == null)
                throw new ArgumentNullException("columnName");

            //table = table.Copy() as FeatureDataTable;

            if (!table.Columns.Contains(columnName))
                table.Columns.Add(columnName, typeof(TOid));

            return InternalCreateTableWithId(table, table.Columns[columnName]);
        }

        public static FeatureDataTable<TOid> CreateTableWithId(FeatureDataTable table, DataColumn column)
        {
            if (table == null)
                throw new ArgumentNullException("table");

            if (column == null)
                throw new ArgumentNullException("column");

            return InternalCreateTableWithId(table.Copy() as FeatureDataTable, column);
        }

        private static FeatureDataTable<TOid> InternalCreateTableWithId(FeatureDataTable tableCopy, DataColumn objectIdColumn)
        {
            FeatureDataTable<TOid> tableWithId = new FeatureDataTable<TOid>(tableCopy, objectIdColumn.ColumnName);

            // TODO: shouldn't this be in the base class? Need to check if changing base behavior will break stuff.
            foreach (DataColumn col in tableCopy.Columns)
            {
                if (String.Compare(col.ColumnName, objectIdColumn.ColumnName, StringComparison.InvariantCultureIgnoreCase) == 0)
                    continue;
                DataColumn colCopy = new DataColumn(col.ColumnName, col.DataType);
                colCopy.AllowDBNull = col.AllowDBNull;
                colCopy.AutoIncrement = col.AutoIncrement;
                colCopy.AutoIncrementSeed = col.AutoIncrementSeed;
                colCopy.AutoIncrementStep = col.AutoIncrementStep;
                colCopy.DateTimeMode = col.DateTimeMode;
                colCopy.DefaultValue = col.DefaultValue;
                foreach (DictionaryEntry entry in col.ExtendedProperties)
                    colCopy.ExtendedProperties[entry.Key] = entry.Value;
                colCopy.MaxLength = col.MaxLength;
                colCopy.ReadOnly = col.ReadOnly;
                colCopy.Unique = col.Unique;
                tableWithId.Columns.Add(colCopy);
            }

            foreach (DataRow row in tableCopy)
            {
                FeatureDataRow<TOid> newRow = tableWithId.NewRow() as FeatureDataRow<TOid>;
                int itemCount = newRow.ItemArray.Length;
                newRow.ItemArray = new object[itemCount];
                //Array.Copy(row.ItemArray, newRow.ItemArray, itemCount);
                newRow.ItemArray = row.ItemArray;
                tableWithId.AddRow(newRow);
            }

            return tableWithId;
        }

        public DataColumn IdColumn
        {
            get { return _idColumn; }
            private set { _idColumn = value; }
        }

        /// <summary>
        /// Gets the feature data row at the specified index
        /// </summary>
        /// <param name="index">row index</param>
        /// <returns>FeatureDataRow</returns>
        public new FeatureDataRow<TOid> this[int index]
        {
            get { return (FeatureDataRow<TOid>)base.Rows[index]; }
        }

        public void AddRow(FeatureDataRow<TOid> row)
        {
            base.Rows.Add(row);
        }

        /// <summary>
        /// Returns an enumerator for enumering the rows of the <see cref="FeatureDataTable{TOid}">table</see> instance.
        /// </summary>
        /// <returns></returns>
        public new IEnumerator<FeatureDataRow<TOid>> GetEnumerator()
        {
            foreach (FeatureDataRow<TOid> row in Rows)
                yield return row;
        }

        /// <summary>
        /// Clones the structure of the FeatureDataTable, including all FeatureDataTable schemas and constraints. 
        /// </summary>
        /// <returns></returns>
        public new FeatureDataTable<TOid> Clone()
        {
            FeatureDataTable<TOid> clone = ((FeatureDataTable<TOid>)(base.Clone()));
            clone.IdColumn = clone.Columns[IdColumn.ColumnName];
            return clone;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override DataTable CreateInstance()
        {
            return new FeatureDataTable<TOid>();
        }

        /// <summary>
        /// Creates a new FeatureDataRow with the same schema as the table.
        /// </summary>
        /// <returns></returns>
        public FeatureDataRow<TOid> NewRow(TOid id)
        {
            FeatureDataRow<TOid> row = base.NewRow() as FeatureDataRow<TOid>;
            row[IdColumn] = id;
            return row;
        }

        /// <summary>
        /// Creates a new FeatureDataRow with the same schema as the table, based on a datarow builder
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
        {
            return new FeatureDataRow<TOid>(builder);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        protected override System.Type GetRowType()
        {
            return typeof(FeatureDataRow<TOid>);
        }

        /// <summary>
        /// Removes the row from the table
        /// </summary>
        /// <param name="row">Row to remove</param>
        public void RemoveRow(FeatureDataRow<TOid> row)
        {
            base.Rows.Remove(row);
        }

        private void setIdColumn(string idColumnName)
        {
            if (String.IsNullOrEmpty(idColumnName))
                throw new ArgumentNullException("idColumnName");

            if (Columns.Contains(idColumnName))
            {
                if (Columns[idColumnName].DataType != typeof(TOid))
                    throw new InvalidOperationException("Column with name " + idColumnName + " exists, but has different type from type parameter type: " + typeof(TOid).Name);

                IdColumn = Columns[idColumnName];
            }
            else
            {
                IdColumn = new DataColumn(idColumnName, typeof(TOid));
                Columns.Add(IdColumn);
            }
        }
    }

	/// <summary>
	/// Represents one feature table of in-memory spatial data. 
	/// </summary>
	//[System.Diagnostics.DebuggerStepThrough()]
	[Serializable()]
	public class FeatureDataTable : DataTable, IList, IEnumerable<IFeature>
	{
		/// <summary>
		/// Initializes a new instance of the FeatureDataTable class with no arguments.
		/// </summary>
		public FeatureDataTable() : base()
		{
			this.InitClass();
		}

		/// <summary>
		/// Intitalizes a new instance of the FeatureDataTable class with the specified table name.
		/// 
		/// Todo: This is misleading, since the variable supplied is a table and not a tablename. 
		/// </summary>
		/// <param name="table"></param>
		public FeatureDataTable(DataTable table): base(table.TableName)
		{
            if (table.DataSet != null)
            {
                if ((table.CaseSensitive != table.DataSet.CaseSensitive))
                {
                    CaseSensitive = table.CaseSensitive;
                }
                if ((table.Locale.ToString() != table.DataSet.Locale.ToString()))
                {
                    Locale = table.Locale;
                }
                if ((table.Namespace != table.DataSet.Namespace))
                {
                    Namespace = table.Namespace;
                }
            }

			Prefix = table.Prefix;
			MinimumCapacity = table.MinimumCapacity;
			DisplayExpression = table.DisplayExpression;
		}

	    public int Add(object item)
	    {
            if(item is FeatureDataRow)
            {
                Rows.Add(item);
                return Count - 1;
            }

            throw new NotImplementedException();
	    }

	    public bool Contains(object item)
	    {
	        return Rows.Contains(item);
	    }

	    public void CopyTo(Array array, int arrayIndex)
	    {
	        Rows.CopyTo(array, arrayIndex);
	    }

	    public void Remove(object item)
	    {
	        Rows.Remove((DataRow) item);
	    }

	    /// <summary>
		/// Gets the number of rows in the table
		/// </summary>
		[System.ComponentModel.Browsable(false)]
		public int Count
		{
			get { return Rows.Count; }
		}

	    public object SyncRoot
	    {
	        get { return Rows.SyncRoot; }
	    }

	    public bool IsSynchronized
	    {
	        get { return Rows.IsSynchronized; }
	    }

	    public bool IsReadOnly
	    {
	        get { return Rows.IsReadOnly; }
	    }

	    public bool IsFixedSize
	    {
	        get { return false; }
	    }

	    public int IndexOf(object item)
	    {
            return Rows.IndexOf((DataRow) item);
	    }

	    public void Insert(int index, object item)
	    {
            if(item is FeatureDataRow)
            {
                Rows.InsertAt((DataRow) item, index);
            }

	        throw new NotSupportedException();
	    }

	    public void RemoveAt(int index)
	    {
	        Rows.RemoveAt(index);
	    }

	    /// <summary>
		/// Gets the feature data row at the specified index
		/// </summary>
		/// <param name="index">row index</param>
		/// <returns>FeatureDataRow</returns>
		public object this[int index]
		{
		    get
			{
				return Rows[index];
			}
		    set
		    {
		        throw new NotImplementedException();
		    }
		}

	    /// <summary>
		/// Occurs after a FeatureDataRow has been changed successfully. 
		/// </summary>
		public event FeatureDataRowChangeEventHandler FeatureDataRowChanged;

		/// <summary>
		/// Occurs when a FeatureDataRow is changing. 
		/// </summary>
		public event FeatureDataRowChangeEventHandler FeatureDataRowChanging;

		/// <summary>
		/// Occurs after a row in the table has been deleted.
		/// </summary>
		public event FeatureDataRowChangeEventHandler FeatureDataRowDeleted;

		/// <summary>
		/// Occurs before a row in the table is about to be deleted.
		/// </summary>
		public event FeatureDataRowChangeEventHandler FeatureDataRowDeleting;

        /// <summary>
        /// Occurs after new geometry has been set to a feature data row.
        /// </summary>
        public event FeatureDataRowChangeEventHandler FeatureDataRowGeometryChanged;

		/// <summary>
		/// Adds a row to the FeatureDataTable
		/// </summary>
		/// <param name="row"></param>
		public void AddRow(FeatureDataRow row)
		{
			base.Rows.Add(row);
		}

	    IEnumerator<IFeature> IEnumerable<IFeature>.GetEnumerator()
	    {
	        foreach (var feature in Rows)
	        {
	            yield return (IFeature) feature;
	        }
	    }

	    /// <summary>
		/// Returns an enumerator for enumering the rows of the FeatureDataTable
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			return Rows.GetEnumerator();
		}

		/// <summary>
		/// Clones the structure of the FeatureDataTable, including all FeatureDataTable schemas and constraints. 
		/// </summary>
		/// <returns></returns>
		public new FeatureDataTable Clone()
		{
			FeatureDataTable clone = ((FeatureDataTable)(base.Clone()));
			clone.InitVars();
			return clone;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override DataTable CreateInstance()
		{
			return new FeatureDataTable();
		}

		internal void InitVars()
		{
			//this.columnFeatureGeometry = this.Columns["FeatureGeometry"];
		}

		private void InitClass()
		{
			//this.columnFeatureGeometry = new DataColumn("FeatureGeometry", typeof(SharpMap.Geometries.Geometry), null, System.Data.MappingType.Element);
			//this.Columns.Add(this.columnFeatureGeometry);
		}

		/// <summary>
		/// Creates a new FeatureDataRow with the same schema as the table.
		/// </summary>
		/// <returns></returns>
		public new FeatureDataRow NewRow()
		{
			var newRow = (FeatureDataRow)base.NewRow();
		    newRow.Attributes = new FeatureDataRowAttributeAccessor(newRow);
		    return newRow;
		}

		/// <summary>
		/// Creates a new FeatureDataRow with the same schema as the table, based on a datarow builder
		/// </summary>
		/// <param name="builder"></param>
		/// <returns></returns>
		protected override DataRow NewRowFromBuilder(DataRowBuilder builder)
		{
		    var newRow = new FeatureDataRow(builder);
		    newRow.Attributes = new FeatureDataRowAttributeAccessor(newRow);
		    return newRow;
		}

	    /// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		protected override System.Type GetRowType()
		{
			return typeof(FeatureDataRow);
		}

		/// <summary>
		/// Raises the FeatureDataRowChanged event. 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnRowChanged(DataRowChangeEventArgs e)
		{
			base.OnRowChanged(e);
			if ((this.FeatureDataRowChanged != null))
			{
				this.FeatureDataRowChanged(this, new FeatureDataRowChangeEventArgs(((FeatureDataRow)(e.Row)), e.Action));
			}
		}

		/// <summary>
		/// Raises the FeatureDataRowChanging event. 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnRowChanging(DataRowChangeEventArgs e)
		{
			base.OnRowChanging(e);
			if ((this.FeatureDataRowChanging != null))
			{
				this.FeatureDataRowChanging(this, new FeatureDataRowChangeEventArgs(((FeatureDataRow)(e.Row)), e.Action));
			}
		}

		/// <summary>
		/// Raises the FeatureDataRowDeleted event
		/// </summary>
		/// <param name="e"></param>
		protected override void OnRowDeleted(DataRowChangeEventArgs e)
		{
			base.OnRowDeleted(e);
			if ((this.FeatureDataRowDeleted != null))
			{
				this.FeatureDataRowDeleted(this, new FeatureDataRowChangeEventArgs(((FeatureDataRow)(e.Row)), e.Action));
			}
		}

		/// <summary>
		/// Raises the FeatureDataRowDeleting event. 
		/// </summary>
		/// <param name="e"></param>
		protected override void OnRowDeleting(DataRowChangeEventArgs e)
		{
			base.OnRowDeleting(e);
			if ((this.FeatureDataRowDeleting != null))
			{
				this.FeatureDataRowDeleting(this, new FeatureDataRowChangeEventArgs(((FeatureDataRow)(e.Row)), e.Action));
			}
		}

        internal void OnFeatureDataRowGeometryChanged(FeatureDataRowChangeEventArgs e)
        {
            if(FeatureDataRowGeometryChanged != null)
            {
                FeatureDataRowGeometryChanged(this, e);
            }
        }

        ///// <summary>
        ///// Gets the collection of rows that belong to this table.
        ///// </summary>
        //public new DataRowCollection Rows
        //{
        //    get { throw (new NotSupportedException()); }
        //    set { throw (new NotSupportedException()); }
        //}

		/// <summary>
		/// Removes the row from the table
		/// </summary>
		/// <param name="row">Row to remove</param>
		public void RemoveRow(FeatureDataRow row)
		{
			base.Rows.Remove(row);
		}
	}

    /// <summary>
	/// Represents the collection of tables for the FeatureDataSet.
	/// </summary>
	[Serializable()]
	public class FeatureTableCollection : System.Collections.Generic.List<FeatureDataTable>
	{
	}

    /// <summary>
	/// Occurs after a FeatureDataRow has been changed successfully.
	/// </summary>
	[System.Diagnostics.DebuggerStepThrough()]
	public class FeatureDataRowChangeEventArgs : EventArgs
	{

		private FeatureDataRow eventRow;

		private DataRowAction eventAction;
	    private IGeometry oldGeometry;

	    /// <summary>
		/// Initializes a new instance of the FeatureDataRowChangeEventArgs class.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="action"></param>
		public FeatureDataRowChangeEventArgs(FeatureDataRow row, DataRowAction action)
		{
			this.eventRow = row;
			this.eventAction = action;
		}

		/// <summary>
		/// Gets the row upon which an action has occurred.
		/// </summary>
		public FeatureDataRow Row
		{
			get
			{
				return this.eventRow;
			}
		}

		/// <summary>
		/// Gets the action that has occurred on a FeatureDataRow.
		/// </summary>
		public DataRowAction Action
		{
			get
			{
				return this.eventAction;
			}
		}

        // TODO: make remember all row / feature including attributes. 
        // Maybe combine with ADO.NET RowChanged event if it uses memento for changed rows
	    public IGeometry OldGeometry
	    {
	        get { return oldGeometry; }
	        set { oldGeometry = value; }
	    }
	}
}
