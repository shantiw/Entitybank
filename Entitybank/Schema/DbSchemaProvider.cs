using XData.Data.Helpers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XData.Data.Schema
{
    public abstract class DbSchemaProvider : IDbSchemaProvider
    {
        protected abstract DbConnection CreateConnection(string connectionString);
        protected abstract DbDataAdapter CreateDataAdapter();

        protected readonly DbConnection Connection;

        public DbSchemaProvider(string connectionString)
        {
            Connection = CreateConnection(connectionString);
        }

        protected DataTable GetTable(string sql)
        {
            DbCommand command = Connection.CreateCommand();
            command.CommandText = sql;
            DbDataAdapter adapter = CreateDataAdapter();
            adapter.SelectCommand = command;
            DataTable table = new DataTable();
            adapter.Fill(table);
            return table;
        }

        protected static bool IsNumeric(Type type)
        {
            return TypeHelper.IsNumeric(type);
        }

        public virtual XElement GetDbSchema()
        {
            XElement schema = new XElement(SchemaVocab.Schema);

            //
            schema.SetAttributeValue(SchemaVocab.TimezoneOffset, GetTimezoneOffset());

            DataSet schemaSet = GetSchemaSet();
            foreach (DictionaryEntry ep in schemaSet.ExtendedProperties)
            {
                schema.SetAttributeValue(SchemaVocab.ExtendedPropertyPrefix + ep.Key, ep.Value);
            }

            foreach (DataTable table in schemaSet.Tables)
            {
                XElement xTable = new XElement(SchemaVocab.Table);
                xTable.SetAttributeValue(SchemaVocab.Name, table.TableName);
                if (table.PrimaryKey.Length > 0)
                {
                    xTable.SetAttributeValue(SchemaVocab.PrimaryKey, string.Join(",", table.PrimaryKey.Select(c => c.ColumnName)));
                }
                foreach (DictionaryEntry ep in table.ExtendedProperties)
                {
                    xTable.SetAttributeValue(SchemaVocab.ExtendedPropertyPrefix + ep.Key, ep.Value);
                }
                schema.Add(xTable);
                foreach (DataColumn column in table.Columns)
                {
                    XElement xColumn = new XElement(SchemaVocab.Column);
                    xColumn.SetAttributeValue(SchemaVocab.Name, column.ColumnName);
                    xColumn.SetAttributeValue(SchemaVocab.DataType, column.DataType);
                    xColumn.SetAttributeValue(SchemaVocab.AllowDBNull, column.AllowDBNull);
                    if (column.AutoIncrement)
                    {
                        xColumn.SetAttributeValue(SchemaVocab.AutoIncrement, column.AutoIncrement);
                        xColumn.SetAttributeValue(SchemaVocab.AutoIncrementSeed, column.AutoIncrementSeed);
                        xColumn.SetAttributeValue(SchemaVocab.AutoIncrementStep, column.AutoIncrementStep);
                    }
                    if (column.DataType == typeof(DateTime))
                    {
                        if (column.DateTimeMode != DataSetDateTime.UnspecifiedLocal)
                        {
                            xColumn.SetAttributeValue(SchemaVocab.DateTimeMode, column.DateTimeMode);
                        }
                    }
                    if (column.DefaultValue != DBNull.Value)
                    {
                        string sDataType = column.DataType.ToString();
                        if (sDataType == "Microsoft.SqlServer.Types.SqlGeography")
                        {
                            bool isNull = column.DefaultValue.ToString() == "Null";
                        }
                        else if (sDataType == "Microsoft.SqlServer.Types.SqlGeometry")
                        {
                            bool isNull = column.DefaultValue.ToString() == "Null";
                        }
                        else if (sDataType == "Microsoft.SqlServer.Types.SqlHierarchyId")
                        {
                            bool isNull = column.DefaultValue.ToString() == "NULL";
                        }
                        else
                        {
                            xColumn.SetAttributeValue(SchemaVocab.DefaultValue, column.DefaultValue);
                        }
                    }
                    if (!string.IsNullOrWhiteSpace(column.Expression))
                    {
                        xColumn.SetAttributeValue(SchemaVocab.Expression, column.Expression);
                    }
                    if (column.MaxLength > 0)
                    {
                        xColumn.SetAttributeValue(SchemaVocab.MaxLength, column.MaxLength);
                    }
                    if (column.ReadOnly)
                    {
                        xColumn.SetAttributeValue(SchemaVocab.Readonly, column.ReadOnly);
                    }
                    if (column.Unique)
                    {
                        xColumn.SetAttributeValue(SchemaVocab.Unique, column.Unique);
                    }

                    if (column.ExtendedProperties.ContainsKey(SchemaVocab.ForeignKey))
                    {
                        xColumn.SetAttributeValue(SchemaVocab.ForeignKey, column.ExtendedProperties[SchemaVocab.ForeignKey]);
                        column.ExtendedProperties.Remove(SchemaVocab.ForeignKey);
                    }
                    foreach (DictionaryEntry ep in column.ExtendedProperties)
                    {
                        xColumn.SetAttributeValue(SchemaVocab.ExtendedPropertyPrefix + ep.Key, ep.Value);
                    }
                    xTable.Add(xColumn);
                }
                foreach (Constraint constraintc in table.Constraints)
                {
                    if (constraintc is ForeignKeyConstraint)
                    {
                        ForeignKeyConstraint foreignKey = constraintc as ForeignKeyConstraint;
                        XElement xForeignKey = new XElement(SchemaVocab.ForeignKey);
                        xForeignKey.SetAttributeValue(SchemaVocab.Name, foreignKey.ConstraintName);
                        xForeignKey.SetAttributeValue(SchemaVocab.RelatedTable, foreignKey.RelatedTable);
                        for (int i = 0; i < foreignKey.Columns.Length; i++)
                        {
                            XElement xCol = new XElement(SchemaVocab.Column);
                            xCol.SetAttributeValue(SchemaVocab.Name, foreignKey.Columns[i].ColumnName);
                            xCol.SetAttributeValue(SchemaVocab.RelatedColumn, foreignKey.RelatedColumns[i].ColumnName);
                            xForeignKey.Add(xCol);
                        }
                        xTable.Add(xForeignKey);
                    }
                }
            }

            //
            IEnumerable<string> sequences = GetSequences();
            foreach (string sequence in sequences)
            {
                XElement xSequence = new XElement(SchemaVocab.Sequence);
                xSequence.SetAttributeValue(SchemaVocab.Name, sequence);
                schema.Add(xSequence);
            }

            return schema;
        }

        protected abstract DataSet GetSchemaSet();

        protected abstract string GetTimezoneOffset();

        protected abstract IEnumerable<string> GetSequences();

        protected void FillSchema(DataSet dataSet, string sqlFormat)
        {
            DbCommand command = Connection.CreateCommand();
            foreach (DataTable table in dataSet.Tables)
            {
                command.CommandText = string.Format(sqlFormat, table.TableName);
                DbDataAdapter adapter = CreateDataAdapter();
                adapter.SelectCommand = command;
                try
                {
                    adapter.FillSchema(table, SchemaType.Source);
                }
                catch (Exception ex)
                {
                    if ((string)table.ExtendedProperties["TableType"] == "View")
                    {
                        //throw new SchemaException(string.Format(SchemaMessages.ErrorView, table.TableName), ex);
                    }
                    else
                    {
                        throw ex;
                    }
                }
            }
        }

        protected void SetForeignKeys(DataSet dataSet, DataTable schemaTable)
        {
            Dictionary<string, List<DataRow>> dict = new Dictionary<string, List<DataRow>>();
            foreach (DataRow row in schemaTable.Rows)
            {
                string fkName = (string)row[0];
                if (!dict.ContainsKey(fkName))
                {
                    dict.Add(fkName, new List<DataRow>());
                }
                dict[fkName].Add(row);
            }
            foreach (KeyValuePair<string, List<DataRow>> pair in dict)
            {
                string constraintName = pair.Key;
                List<DataColumn> parentColumns = new List<DataColumn>();
                List<DataColumn> childColumns = new List<DataColumn>();
                foreach (DataRow row in pair.Value)
                {
                    string tableName = (string)row[1];
                    string columnName = (string)row[2];
                    string relatedTableName = (string)row[3];
                    string relatedColumnName = (string)row[4];
                    DataColumn column = dataSet.Tables[tableName].Columns[columnName];
                    column.ExtendedProperties[SchemaVocab.ForeignKey] = constraintName;
                    childColumns.Add(column);
                    DataColumn refColumn = dataSet.Tables[relatedTableName].Columns[relatedColumnName];
                    parentColumns.Add(refColumn);
                }
                ForeignKeyConstraint foreignKeyConstraint = new ForeignKeyConstraint(constraintName, parentColumns.ToArray(), childColumns.ToArray());
                childColumns[0].Table.Constraints.Add(foreignKeyConstraint);
            }
        }


    }
}
