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
        public override string ParameterPrefix => "@";

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
            return parameter.StartsWith(ParameterPrefix) ? new SqlParameter(parameter, value) : new SqlParameter(ParameterPrefix + parameter, value);
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

        internal protected override object[] FetchSequences(string sequenceName, int size)
        {
            DbCommand cmd = Connection.CreateCommand();
            if (Transaction != null)
            {
                cmd.Transaction = Transaction;
            }
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.CommandText = "sys.sp_sequence_get_range";

            cmd.Parameters.Add(CreateParameter("@sequence_name", sequenceName));
            cmd.Parameters.Add(CreateParameter("@range_size", size));

            // Specify an output parameter to retreive the first value of the generated range.
            SqlParameter firstValueInRange = new SqlParameter("@range_first_value", SqlDbType.Variant) { Direction = ParameterDirection.Output };
            cmd.Parameters.Add(firstValueInRange);

            ConnectionState state = cmd.Connection.State;
            if (state == ConnectionState.Closed)
            {
                cmd.Connection.Open();
            }
            try
            {
                // System.InvalidOperationException: SqlCommand/OleDbCommand.Prepare method requires all parameters to have an explicitly set type.
                //cmd.Prepare();
                int affected = cmd.ExecuteNonQuery();
                long value = long.Parse(firstValueInRange.Value.ToString());
                Type type = firstValueInRange.Value.GetType();

                object[] result = new object[size];
                for (int i = 0; i < size; i++)
                {
                    result[i] = Convert.ChangeType(value, type);
                    value++;
                }
                return result;
            }
            catch (Exception ex)
            {
                throw new SQLStatmentException(ex, cmd.CommandText, null);
            }
            finally
            {
                if (state == ConnectionState.Closed)
                {
                    cmd.Connection.Close();
                }
            }
        }


    }
}
