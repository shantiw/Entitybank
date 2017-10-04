using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XData.Data.Modification;

namespace XData.Data.Objects
{
    public partial class SqlDatabase : Database
    {
        public override DateTime GetNow()
        {
            return (DateTime)ExecuteScalar("SELECT GETDATE()");
        }

        public override DateTime GetUtcNow()
        {
            return (DateTime)ExecuteScalar("SELECT GETUTCDATE()");
        }

        public SqlDatabase(string connectionString) : base(connectionString)
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

        public override DbParameter CreateParameter(string parameter, object value)
        {
            return new SqlParameter(parameter, value);
        }

        internal protected override ModificationGenerator CreateModificationGenerator()
        {
            return new SqlModificationGenerator();
        }

        internal protected override int ExecuteInsertCommand(string sql, object[] parameters, out object autoIncrementValue)
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
                cmd.CommandText = "SELECT SCOPE_IDENTITY()";
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

        internal protected override object FetchSequence(string sequenceName)
        {
            string sql = string.Format("SELECT NEXT VALUE FOR [{0}]", sequenceName);
            return ExecuteScalar(sql);
        }


    }
}
