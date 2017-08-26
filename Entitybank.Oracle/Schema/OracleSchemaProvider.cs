using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XData.Data.Schema
{
    // Oracle 10.2.0.4.0
    // not supported: LONG, LONG VARCHAR, LONG RAW
    public class OracleSchemaProvider : DbSchemaProvider, IDbSchemaProvider
    {
        public OracleSchemaProvider(string connectionString) : base(connectionString)
        {
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new OracleConnection(connectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new OracleDataAdapter();
        }

        protected override DataSet GetSchemaSet()
        {
            DataSet dataSet = new DataSet();

            DataTable schemaTable = GetTable("SELECT TABLE_NAME FROM USER_TABLES");
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row[0];
                DataTable table = new DataTable(tableName);
                table.ExtendedProperties.Add("TableType", "Table");
                dataSet.Tables.Add(table);
            }

            schemaTable = GetTable("SELECT VIEW_NAME FROM USER_VIEWS");
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row[0];
                DataTable table = new DataTable(tableName);
                table.ExtendedProperties.Add("TableType", "View");
                dataSet.Tables.Add(table);
            }

            FillSchema(dataSet, "SELECT * FROM \"{0}\"");

            SetColumns(dataSet);

            SetForeignKeys(dataSet);

            return dataSet;
        }

        // DATA_DEFAULT is LONG
        protected void SetColumns(DataSet dataSet)
        {
            string tempTableName = null;
            DbCommand command = Connection.CreateCommand();
            DbDataAdapter adapter = CreateDataAdapter();
            DataTable schemaTable = new DataTable();
            try
            {
                Connection.Open();

                //
                while (true)
                {
                    string name = "T" + Guid.NewGuid().ToString("N").ToUpper().Substring(8);
                    command.CommandText = string.Format("SELECT TABLE_NAME FROM ALL_TAB_COMMENTS WHERE TABLE_NAME = '{0}'", name);
                    object obj = command.ExecuteScalar();
                    if (obj == null)
                    {
                        tempTableName = name;
                        break;
                    }
                }

                //
                command.CommandText = string.Format(
    "CREATE GLOBAL TEMPORARY TABLE {0} ON COMMIT PRESERVE ROWS AS SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, DATA_LENGTH, TO_LOB(DATA_DEFAULT) DATA_DEFAULT, CHAR_COL_DECL_LENGTH FROM USER_TAB_COLUMNS",
    tempTableName);
                int i = command.ExecuteNonQuery();

                //
                command.CommandText = string.Format("SELECT TABLE_NAME, COLUMN_NAME, DATA_TYPE, DATA_LENGTH, DATA_DEFAULT, CHAR_COL_DECL_LENGTH FROM {0} ORDER BY TABLE_NAME, COLUMN_NAME", tempTableName);
                adapter.SelectCommand = command;
                adapter.Fill(schemaTable);
            }
            finally
            {
                if (!string.IsNullOrWhiteSpace(tempTableName))
                {
                    command.CommandText = string.Format("TRUNCATE TABLE {0}", tempTableName);
                    int i = command.ExecuteNonQuery();
                    command.CommandText = string.Format("DROP TABLE {0} PURGE", tempTableName);
                    i = command.ExecuteNonQuery();
                }

                Connection.Close();
            }

            //
            foreach (DataRow row in schemaTable.Rows)
            {
                string tableName = (string)row[0];
                string columnName = (string)row[1];
                string dataType = (string)row[2];
                decimal dataLength = (decimal)row[3];
                string dataDefault = (row[4] == DBNull.Value) ? null : (string)row[4];
                decimal? charLength = (row[5] == DBNull.Value) ? null : (decimal?)row[5];

                DataTable table = dataSet.Tables[tableName];
                if (table == null) continue;
                if (table.Columns.Count == 0) continue;

                DataColumn column = table.Columns[columnName];
                column.ExtendedProperties.Add("OraDbType", dataType);
                if (column.DataType == typeof(string) || column.DataType == typeof(byte[]))
                {
                    column.ExtendedProperties.Add("MaxLength", (charLength == null) ? dataLength : charLength);
                }

                if (dataDefault != null)
                {
                    SetDefaultValue(column, dataDefault);
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
                if (s.StartsWith("'") || s.StartsWith("N'", StringComparison.InvariantCultureIgnoreCase) || s.StartsWith("U'", StringComparison.InvariantCultureIgnoreCase))
                {
                    s = s.Trim();
                    if (s.EndsWith("'"))
                    {
                        s = s.StartsWith("'") ? s.Substring(1, s.Length - 2) : s.Substring(2, s.Length - 3);
                    }
                }
                else
                {
                    column.DefaultValue = s.Trim();
                }
                return;
            }
            else if (column.DataType == typeof(bool))
            {
                string s = columnDefault;
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
                string s = columnDefault.Trim().TrimStart('\'').TrimEnd('\'');

            }
            else if (IsNumeric(column.DataType))
            {
                string s = columnDefault.Trim();
                if (s.StartsWith("(") && s.EndsWith(")"))
                {
                    s = s.Substring(1, s.Length - 2);
                }
                if (s.StartsWith("'") && s.EndsWith("'"))
                {
                    s = s.Substring(1, s.Length - 2);
                }
                double result;
                if (double.TryParse(s, out result))
                {
                    column.DefaultValue = Convert.ChangeType(s, column.DataType);
                    return;
                }
            }

            // whitespace, function, etc..
            if (string.IsNullOrWhiteSpace(columnDefault))
            {
                column.ExtendedProperties.Add("DefaultValue", columnDefault);
            }
            else
            {
                column.ExtendedProperties.Add("DefaultValue", columnDefault.Trim());
            }
        }

        protected void SetForeignKeys(DataSet dataSet)
        {
            // SELECT CONSTRAINT_NAME, R_CONSTRAINT_NAME FROM USER_CONSTRAINTS WHERE CONSTRAINT_TYPE ='R'
            // SELECT CONSTRAINT_NAME, TABLE_NAME, COLUMN_NAME, "POSITION" FROM USER_CONS_COLUMNS

            string sql = @"
SELECT T.CONSTRAINT_NAME, T.TABLE_NAME, T.COLUMN_NAME, C.TABLE_NAME R_TABLE_NAME, C.COLUMN_NAME R_COLUMN_NAME FROM
(SELECT A.CONSTRAINT_NAME, B.R_CONSTRAINT_NAME, A.TABLE_NAME, A.COLUMN_NAME, A.POSITION FROM USER_CONS_COLUMNS A
INNER JOIN USER_CONSTRAINTS B ON A.CONSTRAINT_NAME = B.CONSTRAINT_NAME AND B.CONSTRAINT_TYPE ='R') T
INNER JOIN USER_CONS_COLUMNS C ON T.R_CONSTRAINT_NAME = C.CONSTRAINT_NAME AND T.POSITION = C.POSITION
ORDER BY T.CONSTRAINT_NAME, T.POSITION";
            DataTable schemaTable = GetTable(sql);

            SetForeignKeys(dataSet, schemaTable);
        }

        protected override IEnumerable<string> GetSequences()
        {
            List<string> list = new List<string>();
            DataTable schemaTable = GetTable("SELECT SEQUENCE_NAME FROM USER_SEQUENCES");
            foreach (DataRow row in schemaTable.Rows)
            {
                string sequenceName = (string)row[0];
                list.Add(sequenceName);
            }
            return list;
        }

        protected override string GetTimezoneOffset()
        {
            DataTable schemaTable = GetTable("SELECT DBTIMEZONE FROM DUAL");
            return (string)schemaTable.Rows[0][0];
        }


    }
}
