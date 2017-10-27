using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Modification;

namespace XData.Data.Objects
{
    // MySQL 5.5.47-log
    // not supported: GEOMETRY, POINT, LINESTRING, POLYGON, MULTIPOINT, MULTILINESTRING, MULTIPOLYGON, GEOMETRYCOLLECTION
    public partial class MySqlDatabase : Database
    {
        public override string ParameterPrefix => "?";

        public override DateTime GetNow()
        {
            return (DateTime)ExecuteScalar("SELECT current_timestamp()");
        }

        public override DateTime GetUtcNow()
        {
            return (DateTime)ExecuteScalar("SELECT utc_timestamp()");
        }

        public MySqlDatabase(string connectionString) : base(connectionString)
        {
        }

        protected override DbConnection CreateConnection(string connectionString)
        {
            return new MySqlConnection(connectionString);
        }

        protected override DbDataAdapter CreateDataAdapter()
        {
            return new MySqlDataAdapter();
        }

        public override DbParameter CreateParameter(string parameter, object value)
        {
            return parameter.StartsWith(ParameterPrefix) ? new MySqlParameter(parameter, value) : new MySqlParameter(ParameterPrefix + parameter, value);
        }

        protected override ModificationGenerator CreateModificationGenerator()
        {
            return new MySqlModificationGenerator();
        }

        protected override int ExecuteInsertCommand(string sql, object[] parameters, out object autoIncrementValue)
        {
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandText = sql;
            cmd.Parameters.AddRange(parameters);
            ConnectionState state = cmd.Connection.State;
            if (state == ConnectionState.Closed)
            {
                cmd.Connection.Open();
            }
            try
            {
                int affected = cmd.ExecuteNonQuery();
                cmd.CommandText = "SELECT LAST_INSERT_ID()";
                autoIncrementValue = cmd.ExecuteScalar();
                return affected;
            }
            catch (Exception ex)
            {
                throw new SQLStatmentException(ex, sql, parameters);
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }

        protected override object FetchSequence(string sequenceName)
        {
            throw new NotSupportedException();
        }


    }
}
