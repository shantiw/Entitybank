using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    // SQL Server 2012
    // not supported: geography, geometry, hierarchyid, sql_variant
    public class SqlSchemaProvider : DbSchemaProvider, IDbSchemaProvider
    {
        public SqlSchemaProvider(string connectionString) : base(connectionString)
        {
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new SqlConnection(connectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new SqlDataAdapter();
        }

        protected override DataSet GetSchemaSet()
        {
            DataSet dataSet = new DataSet();

            DataTable schemaTable = GetTable("SELECT TABLE_NAME, TABLE_TYPE FROM INFORMATION_SCHEMA.TABLES ORDER BY TABLE_NAME");
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row[0];
                if (tableName == "sysdiagrams") continue;

                DataTable table = new DataTable(tableName);

                string tableType = (string)row[1];
                if (tableType == "BASE TABLE")
                {
                    table.ExtendedProperties.Add("TableType", "Table");
                }
                else if (tableType == "VIEW")
                {
                    table.ExtendedProperties.Add("TableType", "View");
                }
                dataSet.Tables.Add(table);
            }

            FillSchema(dataSet, "SELECT * FROM [{0}]");

            SetColumns(dataSet);

            SetForeignKeys(dataSet);

            return dataSet;
        }

        protected void SetForeignKeys(DataSet dataSet)
        {
            //SELECT CONSTRAINT_NAME, UNIQUE_CONSTRAINT_NAME FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS
            //SELECT TABLE_NAME, COLUMN_NAME, CONSTRAINT_NAME, ORDINAL_POSITION FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE

            string sql = @"
SELECT T.CONSTRAINT_NAME, T.TABLE_NAME, T.COLUMN_NAME, C.TABLE_NAME R_TABLE_NAME, C.COLUMN_NAME R_TABLE_NAME FROM
(SELECT B.CONSTRAINT_NAME, TABLE_NAME, COLUMN_NAME, ORDINAL_POSITION, UNIQUE_CONSTRAINT_NAME FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE A
INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS B ON A.CONSTRAINT_NAME = B.CONSTRAINT_NAME) T
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE C ON T.UNIQUE_CONSTRAINT_NAME = C.CONSTRAINT_NAME AND T.ORDINAL_POSITION = C.ORDINAL_POSITION
ORDER BY T.CONSTRAINT_NAME, T.ORDINAL_POSITION";
            DataTable schemaTable = GetTable(sql);

            SetForeignKeys(dataSet, schemaTable);
        }

        protected void SetColumns(DataSet dataSet)
        {
            DataTable schemaTable = GetTable("SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, COLUMN_DEFAULT FROM INFORMATION_SCHEMA.COLUMNS");
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row[0];
                string columnName = (string)row[1];
                string sqlDbType = (string)row[2];
                int? maxLength = (row[3] == DBNull.Value) ? null : (int?)row[3];
                maxLength = (maxLength == -1) ? int.MaxValue : maxLength;
                string columnDefault = (row[4] == DBNull.Value) ? null : (string)row[4];

                DataTable table = dataSet.Tables[tableName];
                if (table == null) continue;
                if (table.Columns.Count == 0) continue;

                DataColumn column = table.Columns[columnName];
                column.ExtendedProperties.Add("SqlDbType", sqlDbType);
                if (maxLength != null)
                {
                    column.ExtendedProperties.Add("MaxLength", maxLength);
                }

                if (sqlDbType == "sql_variant")
                {
                    column.DefaultValue = DBNull.Value;
                }
                else if (columnDefault != null)
                {
                    SetDefaultValue(column, columnDefault);
                }
            }
        }

        protected void SetDefaultValue(DataColumn column, string columnDefault)
        {
            if (column.DataType == typeof(DateTime))
            {
                if (columnDefault.StartsWith("('") && columnDefault.EndsWith("')"))
                {
                    string s = columnDefault.Substring(2, columnDefault.Length - 4);
                    if (DateTime.TryParse(s, out DateTime result))
                    {
                        column.DefaultValue = result;
                        return;
                    }
                }
            }
            else if (column.DataType == typeof(DateTimeOffset))
            {
                if (columnDefault.StartsWith("('") && columnDefault.EndsWith("')"))
                {
                    string s = columnDefault.Substring(2, columnDefault.Length - 4);
                    if (DateTimeOffset.TryParse(s, out DateTimeOffset result))
                    {
                        column.DefaultValue = result;
                        return;
                    }
                }
            }
            else if (column.DataType == typeof(TimeSpan))
            {
                if (columnDefault.StartsWith("('") && columnDefault.EndsWith("')"))
                {
                    string s = columnDefault.Substring(2, columnDefault.Length - 4);
                    if (TimeSpan.TryParse(s, out TimeSpan result))
                    {
                        column.DefaultValue = result;
                        return;
                    }
                }
            }
            else if (column.DataType == typeof(string))
            {
                if ((columnDefault.StartsWith("('") || columnDefault.StartsWith("(N'")) && columnDefault.EndsWith("')"))
                {
                    string s = columnDefault.TrimStart('(').TrimStart('N').TrimEnd(')');
                    column.DefaultValue = s.Substring(1, s.Length - 2);
                    return;
                }
            }
            else if (column.DataType == typeof(bool))
            {
                string s = columnDefault.TrimStart('(').TrimEnd(')');
                if (s == "0")
                {
                    column.DefaultValue = false;
                    return;
                }
                else if (s == "1")
                {
                    column.DefaultValue = true;
                    return;
                }
            }
            else if (column.DataType == typeof(Guid))
            {
                if (columnDefault.StartsWith("('") && columnDefault.EndsWith("')"))
                {
                    string s = columnDefault.Substring(2, columnDefault.Length - 4);
                    if (Guid.TryParse(s, out Guid result))
                    {
                        column.DefaultValue = result;
                        return;
                    }
                }
            }
            else if (column.DataType == typeof(byte[]))
            {
                string s = columnDefault.TrimStart('(').TrimEnd(')');

            }
            else if (IsNumeric(column.DataType))
            {
                if (columnDefault.StartsWith("((") && columnDefault.EndsWith("))"))
                {
                    string s = columnDefault.TrimStart('(').TrimEnd(')');
                    if (double.TryParse(s, out double result))
                    {
                        column.DefaultValue = Convert.ChangeType(s, column.DataType);
                        return;
                    }
                }
            }

            // function
            if (columnDefault.StartsWith("(") && columnDefault.EndsWith(")"))
            {
                string s = columnDefault.Substring(1, columnDefault.Length - 2);
                column.ExtendedProperties.Add("DefaultValue", s);
            }
        }

        protected override string GetTimezoneOffset()
        {
            DataTable schemaTable = GetTable("SELECT SYSDATETIMEOFFSET()");
            DateTimeOffset dateTimeOffset = (DateTimeOffset)schemaTable.Rows[0][0];
            TimeSpan offset = dateTimeOffset.Offset;
            string format = (offset < TimeSpan.Zero ? "\\-" : "\\+") + "hh\\:mm";
            return offset.ToString(format);
        }

        protected override IEnumerable<string> GetSequences()
        {
            List<string> list = new List<string>();
            DataTable schemaTable = GetTable("SELECT SEQUENCE_NAME FROM INFORMATION_SCHEMA.SEQUENCES ORDER BY SEQUENCE_NAME");
            foreach (DataRow row in schemaTable.Rows)
            {
                string sequenceName = (string)row[0];
                list.Add(sequenceName);
            }
            return list;
        }


    }
}
