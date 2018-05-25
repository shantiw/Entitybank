using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    // MySQL 5.5.47-log
    // not supported: GEOMETRY, POINT, LINESTRING, POLYGON, MULTIPOINT, MULTILINESTRING, MULTIPOLYGON, GEOMETRYCOLLECTION
    public class MySqlSchemaProvider : DbSchemaProvider, IDbSchemaProvider
    {
        protected readonly string DatabaseName;

        public MySqlSchemaProvider(string connectionString) : base(connectionString)
        {
            DataTable schemaTable = GetTable("select database()");
            DatabaseName = (string)schemaTable.Rows[0][0];
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        protected override DataSet GetSchemaSet()
        {
            DataSet dataSet = new DataSet();

            string sql = string.Format("select `TABLE_NAME`, TABLE_TYPE, `ENGINE` from information_schema.TABLES where TABLE_SCHEMA = '{0}'", DatabaseName);
            DataTable schemaTable = GetTable(sql);
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row[0];
                string tableType = (string)row[1];
                string engine = (row[2] == DBNull.Value) ? null : (string)row[2];
                DataTable table = new DataTable(tableName);
                if (tableType == "BASE TABLE")
                {
                    table.ExtendedProperties.Add("TableType", "Table");
                }
                else if (tableType == "VIEW")
                {
                    table.ExtendedProperties.Add("TableType", "View");
                }
                if (engine != null)
                {
                    table.ExtendedProperties.Add("Engine", engine);
                }
                dataSet.Tables.Add(table);
            }

            FillSchema(dataSet, "SELECT * FROM `{0}`");

            SetColumns(dataSet);

            SetForeignKeys(dataSet);

            return dataSet;
        }

        protected void SetColumns(DataSet dataSet)
        {
            string sql = string.Format("select `TABLE_NAME`, `COLUMN_NAME`, COLUMN_DEFAULT, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, COLUMN_TYPE from information_schema.COLUMNS where TABLE_SCHEMA = '{0}'", DatabaseName);
            DataTable schemaTable = GetTable(sql);
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row[0];
                string columnName = (string)row[1];
                string columnDefault = (row[2] == DBNull.Value) ? null : (string)row[2];
                string dataType = (string)row[3];
                ulong? charLength = (row[4] == DBNull.Value) ? null : (ulong?)row[4];
                string columnType = (string)row[5];

                DataTable table = dataSet.Tables[tableName];
                if (table == null) continue;
                if (table.Columns.Count == 0) continue;

                DataColumn column = table.Columns[columnName];
                column.ExtendedProperties.Add("MyColType", columnType);
                column.ExtendedProperties.Add("MyDbType", dataType);
                if (column.DataType == typeof(string) || column.DataType == typeof(byte[]))
                {
                    if (charLength != null)
                    {
                        column.ExtendedProperties.Add("MaxLength", charLength);
                    }
                }

                if (columnDefault != null)
                {
                    SetDefaultValue(column, columnDefault);
                }
            }
        }

        protected void SetDefaultValue(DataColumn column, string columnDefault)
        {
            if (column.DataType == typeof(DateTime))
            {
                string s = columnDefault;
                DateTime result;
                if (DateTime.TryParse(s, out result))
                {
                    column.DefaultValue = result;
                    return;
                }
            }
            else if (column.DataType == typeof(DateTimeOffset))
            {
                string s = columnDefault;
                DateTimeOffset result;
                if (DateTimeOffset.TryParse(s, out result))
                {
                    column.DefaultValue = result;
                    return;
                }
            }
            else if (column.DataType == typeof(TimeSpan))
            {
                string s = columnDefault;
                TimeSpan result;
                if (TimeSpan.TryParse(s, out result))
                {
                    column.DefaultValue = result;
                    return;
                }
            }
            else if (column.DataType == typeof(string))
            {
                string s = columnDefault;
                column.DefaultValue = s;
                return;
            }
            else if (column.DataType == typeof(bool))
            {
                string s = columnDefault;
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
                string s = columnDefault;
                Guid result;
                if (Guid.TryParse(s, out result))
                {
                    column.DefaultValue = result;
                    return;
                }
            }
            else if (column.DataType == typeof(byte[]))
            {
                string s = columnDefault;
            }
            else if (IsNumeric(column.DataType))
            {
                string s = columnDefault;
                double result;
                if (double.TryParse(s, out result))
                {
                    column.DefaultValue = Convert.ChangeType(s, column.DataType);
                    return;
                }
            }

            //
            column.ExtendedProperties.Add("DefaultValue", columnDefault);
        }

        protected void SetForeignKeys(DataSet dataSet)
        {
            //select `CONSTRAINT_NAME`, `TABLE_NAME`, `COLUMN_NAME`, REFERENCED_TABLE_NAME, REFERENCED_COLUMN_NAME from information_schema.KEY_COLUMN_USAGE order by `CONSTRAINT_NAME`, ORDINAL_POSITION
            //select `CONSTRAINT_NAME` from information_schema.TABLE_CONSTRAINTS where CONSTRAINT_TYPE = 'FOREIGN KEY'

            string sql = @"
select c.`CONSTRAINT_NAME`, c.`TABLE_NAME`, c.`COLUMN_NAME`, c.REFERENCED_TABLE_NAME, c.REFERENCED_COLUMN_NAME
from information_schema.KEY_COLUMN_USAGE c
inner join information_schema.table_constraints t
on c.constraint_schema = t.constraint_schema
and c.constraint_name = t.constraint_name
and t.constraint_type='FOREIGN KEY'
and c.constraint_schema='{0}'
order by c.`CONSTRAINT_NAME`, c.ORDINAL_POSITION";
            sql = string.Format(sql, DatabaseName);
            DataTable schemaTable = GetTable(sql);

            SetForeignKeys(dataSet, schemaTable);
        }

        protected override IEnumerable<string> GetSequences()
        {
            return new List<string>();
        }

        protected override string GetTimezoneOffset()
        {
            DataTable schemaTable = GetTable("select @@global.time_zone");
            string global_time_zone = (string)schemaTable.Rows[0][0];
            if (global_time_zone == "SYSTEM")
            {
                schemaTable = GetTable("select timestampdiff(minute, utc_timestamp(), current_timestamp())");
                long diff = (long)schemaTable.Rows[0][0];
                TimeSpan offset = new TimeSpan(0, (int)diff, 0);
                string format = (offset < TimeSpan.Zero ? "\\-" : "\\+") + "hh\\:mm";
                return offset.ToString(format);
            }
            return global_time_zone;
        }


    }
}
